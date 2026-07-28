using UnityEngine;
using System;
using System.Collections.Generic;
using Unity.Netcode;
using System.Xml.Serialization;
using Unity.VisualScripting;



public enum StatType { Speed, JumpForce, Size }

[System.Serializable]

public class BallController : NetworkBehaviour
{
    [SerializeField] bool isPlayer = false;
    [SerializeField] private SphereCollider col;
    float speed = 0.1f;
    [SerializeField] private Rigidbody rb;
    float jumpForce = 10f;
    float maxHorizontalSpeed = 5f;
    [SerializeField] float torqueAmount = 5f;
    [SerializeField] float maxAngularVelocity = 5f;
    [SerializeField] float airControl = 0.25f;
    [SerializeField] float maxVerticalSpeed = 15f;
    [SerializeField] float baseSpeed = 0.1f;
    [SerializeField] float baseJumpForce = 10f;
    [SerializeField] float baseMaxHorizontalSpeed = 5f;
    [SerializeField] float baseMass = 20f;
    [SerializeField] int maxOwnedAbilities = 3;
    [SerializeField] bool replaceOldestWhenFull = true;
    float ballRadius;
    List<ActiveBoost> activeBoosts = new List<ActiveBoost>();
    public float BallRadius => ballRadius;
    Vector3 deltaDir;
    [SerializeField] Transform frontIndicator;
    [SerializeField] float indicatorOffsetMultiplier = 1.5f;
    [SerializeField] float indicatorSmoothSpeed = 8f;
    public Vector3 Front { get; private set; } = Vector3.forward;
    Vector3 lastInputDir = Vector3.forward;


    Collider lastGroundCollider;
    SurfaceData currentSurfaceData;


    bool frameHasFloorContact = false;

    [SerializeField]
    [Tooltip("Maps PhysicsMaterial assets to SurfaceData for floors that don't have a SurfaceIdentifier component.")]
    SurfaceMaterialRegistry surfaceRegistry;

    [SerializeField]
    [Tooltip("Contacts steeper than this angle (degrees from straight up) are treated as walls, not floor/slopes, and don't count as ground contact or apply surface data.")]
    float maxSurfaceAngle = 60f;

    public float airAngularDrag = 0f;
    public float defaultJumpMultiplier = 1f;
    public float defaultForceMultiplier = 1f;
    public float defaultTorqueMultiplier = 1f;
    public float defaultAngularFriction = 1f;
    public float defaultLinearFriction = 1f;
    bool groundContacts = false;
    float currentJumpMultiplier = 1f;
    float currentForceMultiplier = 1f;
    float currentTorqueMultiplier = 1f;
    public float currentAngularFriction = 1f;
    public float currentLinearFriction = 1f;
    bool currentIsSlipping = false;
    bool currentIsSticky = false;
    float currentMaxAngularVelocityOverride = 1f;
    Vector3 currentSurfaceNormal = Vector3.up;

    List<AbilityBase> ownedAbilities = new List<AbilityBase>();
    int currentAbilityIndex = -1;
    AbilityBase currentAbility;
    float cooldownTimer = 0f;
    bool activatePressed = false;
    int swapPressed = 0;
    Vector3 baseScale;

    [System.Serializable]
    public class ActiveBoost
    {
        public StatType statType;
        public float multiplier;
        public float remainingTime;
    }

    [Rpc(SendTo.Server)]
    public void RequestApplyTimedBoostRpc(StatType statType, float multiplier, float duration)
    {
        ApplyTimedBoost(statType, multiplier, duration);
    }

    public void ApplyTimedBoost(StatType statType, float multiplier, float duration)
    {
        ActiveBoost existing = activeBoosts.Find(b => b.statType == statType);

        if (existing != null)
        {
            existing.remainingTime = duration;
        }
        else
        {
            activeBoosts.Add(new ActiveBoost { statType = statType, multiplier = multiplier, remainingTime = duration });
        }

        RecalculateStats();
    }


    void RecalculateStats()
    {
        float speedMult = 1f, jumpMult = 1f, maxSpeedMult = 1f, sizeMult = 1f;

        foreach (var boost in activeBoosts)
        {
            switch (boost.statType)
            {
                case StatType.Speed:
                    speedMult *= boost.multiplier;
                    maxSpeedMult *= boost.multiplier;
                    break;
                case StatType.JumpForce: jumpMult *= boost.multiplier; break;
                case StatType.Size:
                    sizeMult *= boost.multiplier;
                    break;
            }
        }

        speed = baseSpeed * speedMult;
        jumpForce = baseJumpForce * jumpMult;
        maxHorizontalSpeed = baseMaxHorizontalSpeed * maxSpeedMult;
        Debug.Log(speed);
        transform.localScale = baseScale * sizeMult;
        rb.mass = baseMass * sizeMult;
        RecalculateRadius();
    }
    void TickBoosts(float deltaTime)
    {
        bool anyExpired = false;

        for (int i = activeBoosts.Count - 1; i >= 0; i--)
        {
            activeBoosts[i].remainingTime -= deltaTime;
            if (activeBoosts[i].remainingTime <= 0f)
            {
                activeBoosts.RemoveAt(i);
                anyExpired = true;
            }
        }

        if (anyExpired) RecalculateStats();
    }


    public GameObject FindClosestTargetInFront(float searchRadius)
    {
        Vector3 origin = transform.position;
        Collider[] candidates = Physics.OverlapSphere(origin, searchRadius);

        GameObject closest = null;
        float closestDist = float.MaxValue;

        foreach (var col in candidates)
        {
            if (col.gameObject == gameObject) continue;
            if (col.attachedRigidbody == null) continue;

            Vector3 toTarget = col.transform.position - origin;
            toTarget.y = 0;

            if (toTarget.magnitude < 0.01f) continue;

            Vector3 dirToTarget = toTarget.normalized;
            float dot = Vector3.Dot(Front, dirToTarget);

            if (dot > 0f)
            {
                float dist = toTarget.magnitude;
                if (dist < closestDist)
                {
                    closestDist = dist;
                    closest = col.gameObject;
                }
            }
        }
        return closest;
    }
    public void RecalculateRadius()
    {
        ballRadius = col.radius * transform.lossyScale.x;
        if (!currentIsSlipping)
        {
            maxAngularVelocity = maxHorizontalSpeed / ballRadius;
        }
    }

    public void CollectAbility(AbilityBase prefab)
    {
        if (ownedAbilities.Count >= maxOwnedAbilities)
        {
            int indexToRemove = replaceOldestWhenFull ? 0 : currentAbilityIndex;

            if (indexToRemove < 0 || indexToRemove >= ownedAbilities.Count)
            {
                indexToRemove = 0; 
            }

            AbilityBase removed = ownedAbilities[indexToRemove];

            if (removed == currentAbility)
            {
                currentAbility.OnUnequip(gameObject);
                currentAbility = null;
                currentAbilityIndex = -1;
            }

            Destroy(removed.gameObject);
            ownedAbilities.RemoveAt(indexToRemove);
        }

        AbilityBase instance = Instantiate(prefab, transform);
        instance.enabled = true;
        ownedAbilities.Add(instance);

        EquipByIndex(ownedAbilities.Count - 1);
    }
    public void SwapAbility(int direction)
    {
        if (ownedAbilities.Count == 0) return;

        int newIndex = (currentAbilityIndex + direction + ownedAbilities.Count) % ownedAbilities.Count;
        EquipByIndex(newIndex);
    }
    void EquipByIndex(int index)
    {
        if (currentAbility != null)
            currentAbility.OnUnequip(gameObject);

        currentAbilityIndex = index;
        currentAbility = ownedAbilities[index];
        cooldownTimer = 0f;

        currentAbility.OnEquip(gameObject);
        Debug.Log("Equipped: " + currentAbility.GetType().Name);
    }

    void OnCollisionStay(Collision collision)
    {

        Vector3 floorNormalSum = Vector3.zero;
        int floorContactCount = 0;

        foreach (var contact in collision.contacts)
        {
            float angleFromUp = Vector3.Angle(contact.normal, Vector3.up);
            if (angleFromUp <= maxSurfaceAngle)
            {
                floorNormalSum += contact.normal;
                floorContactCount++;
            }
        }


        if (floorContactCount == 0) return;

        frameHasFloorContact = true;


        if (collision.collider != lastGroundCollider)
        {
            lastGroundCollider = collision.collider;


            SurfaceIdentifier identifier = collision.collider.GetComponent<SurfaceIdentifier>();
            if (identifier != null)
            {
                currentSurfaceData = identifier.surfaceData;
            }

            else if (surfaceRegistry != null)
            {
                currentSurfaceData = surfaceRegistry.GetSurfaceData(collision.collider.sharedMaterial);
            }
            else
            {
                currentSurfaceData = null;
            }
        }

        ApplySurfaceValues(currentSurfaceData);

        currentSurfaceNormal = (floorNormalSum / floorContactCount).normalized;
    }
    void OnCollisionExit(Collision collision)
    {

        if (collision.collider == lastGroundCollider)
        {
            lastGroundCollider = null;
            currentSurfaceData = null;
        }
    }
    void ApplySurfaceValues(SurfaceData data)
    {
        if (data != null)
        {
            currentJumpMultiplier = data.jumpMultiplier;
            currentForceMultiplier = data.forceMultiplier;
            currentAngularFriction = data.angularFriction;
            currentTorqueMultiplier = data.torqueMultiplier;
            currentIsSlipping = data.isSlippingSurface;
            rb.linearDamping = data.linearFriction;
            currentIsSticky = data.isStickySurface;
            currentMaxAngularVelocityOverride = data.maxAngularVelocityOverride;

            if (currentIsSlipping)
                maxAngularVelocity = currentMaxAngularVelocityOverride;
            else
                RecalculateRadius();

            return;
        }

        rb.angularDamping = airAngularDrag;
        currentJumpMultiplier = defaultJumpMultiplier;
        currentForceMultiplier = defaultForceMultiplier;
        rb.linearDamping = defaultLinearFriction;
        currentAngularFriction = defaultAngularFriction;
        currentTorqueMultiplier = defaultTorqueMultiplier;
        currentMaxAngularVelocityOverride = 1f;
        currentIsSlipping = false;
        currentIsSticky = false;
    }

    
    private void Awake()
    {
        // Disables player control until the ball has been movedto spawn position
        // Otherwise player position desyncs
        enabled = false;
    }
    
    // Switched Start to OnEnable since it won't trigger while the script is disabled
    void OnEnable()
    {
        rb.angularDamping = airAngularDrag;
        baseScale = transform.localScale;
        RecalculateStats();
    }
    void Update()
    {
        if (!IsOwner) return;
        if (!isPlayer) return;
        deltaDir = new Vector3(Input.GetAxisRaw("Horizontal"), 0, Input.GetAxisRaw("Vertical"));
        if (groundContacts == true && Input.GetKeyDown(KeyCode.Space))
        {
            ApplyJumpForceRpc();
        }

        if (Input.GetKeyDown(KeyCode.LeftShift)) activatePressed = true;
        if (Input.GetKeyDown(KeyCode.Q))
            swapPressed = -1;
        else if (Input.GetKeyDown(KeyCode.E))
            swapPressed = 1;

        if (deltaDir.magnitude > 0.01f)
        {
            lastInputDir = deltaDir.normalized;
        }

        // Basic Respawn Function
        if (transform.position.y < -10f)
        {
            RespawnRpc();
        }

        if (Input.GetKeyDown("l"))
        {
            Debug.Log(rb.linearVelocity.y);
        }
    }

    [Rpc(SendTo.Server)]
    private void RespawnRpc()
    {
        transform.position = Vector3.zero;
    }


    [Rpc(SendTo.Server)]
    private void ApplyJumpForceRpc()
    {
        rb.AddForce(Vector3.up * jumpForce * currentJumpMultiplier, ForceMode.Impulse);
    }

    void FixedUpdate()
    {
        if (frameHasFloorContact)
        {
            groundContacts = true;
        }
        else if (groundContacts)
        {
            groundContacts = false;
            lastGroundCollider = null;
            currentSurfaceData = null;
            ApplySurfaceValues(null);
        }
        frameHasFloorContact = false;

        if (!IsOwner) return;
        if (isPlayer)
        {
            Vector3 deltaDir = new Vector3(Input.GetAxisRaw("Horizontal"), 0, Input.GetAxisRaw("Vertical"));
        }
        Vector3 torqueAxis = Vector3.Cross(Vector3.up, deltaDir);
        deltaDir.Normalize();

        PhysicsCalculationsRpc(torqueAxis, deltaDir, currentSurfaceNormal);

        if (swapPressed != 0) { SwapAbility(swapPressed); swapPressed = 0; }

        if (cooldownTimer > 0) cooldownTimer -= Time.fixedDeltaTime;

        if (currentAbility != null &&
            (currentAbility.Type == AbilityType.Passive || currentAbility.Type == AbilityType.ActiveAndPassive))
        {
            currentAbility.PassiveTick(gameObject);
        }

        if (activatePressed && currentAbility != null && cooldownTimer <= 0 &&
            (currentAbility.Type == AbilityType.Active || currentAbility.Type == AbilityType.ActiveAndPassive))
        {
            currentAbility.Activate(gameObject);
            cooldownTimer = currentAbility.Cooldown;
        }

        activatePressed = false;
    }

    [Rpc(SendTo.Server)]
    private void PhysicsCalculationsRpc(Vector3 torqueAxis, Vector3 delta, Vector3 surfaceNormal)
    {
        TickBoosts(Time.fixedDeltaTime);
        Vector3 horizontalVelocity = new Vector3(rb.linearVelocity.x, 0, rb.linearVelocity.z);
        float angularSpeed = 0f;
        if (!currentIsSlipping && horizontalVelocity.magnitude > 0.01f)
        {
            angularSpeed = horizontalVelocity.magnitude / ballRadius;
            Vector3 rotationAxis = Vector3.Cross(Vector3.up, horizontalVelocity.normalized);
            rb.angularVelocity = rotationAxis * angularSpeed;
        }
        if (currentIsSticky)
        {
            Vector3 gravityForce = Physics.gravity * rb.mass;
            Vector3 slideComponent = gravityForce - Vector3.Project(gravityForce, surfaceNormal);

            float inputAlignment = delta.magnitude > 0.01f ? Vector3.Dot(delta.normalized, slideComponent.normalized) : -1f;

            if (inputAlignment < 0.3f)
            {
                rb.AddForce(-slideComponent);
            }
        }
        if (groundContacts == true)
        {
            rb.AddForce(delta * currentForceMultiplier * speed);
            rb.AddTorque(torqueAxis * torqueAmount * currentTorqueMultiplier, ForceMode.Force);

            if (rb.angularVelocity != null && currentIsSlipping)
            {
                rb.AddTorque((-rb.angularVelocity).normalized * currentAngularFriction);
            }
        }
        else
        {
            rb.AddForce(delta * currentForceMultiplier * speed * airControl);
            rb.AddTorque(torqueAxis * torqueAmount * currentTorqueMultiplier * airControl * 0.5f, ForceMode.Force);
        }
        horizontalVelocity = new Vector3(rb.linearVelocity.x, 0, rb.linearVelocity.z);
        if (horizontalVelocity.magnitude > maxHorizontalSpeed)
        {
            horizontalVelocity = horizontalVelocity.normalized * maxHorizontalSpeed;
        }
        float verticalVelocity = Mathf.Clamp(rb.linearVelocity.y, -maxVerticalSpeed, maxVerticalSpeed);
        rb.linearVelocity = new Vector3(horizontalVelocity.x, verticalVelocity, horizontalVelocity.z);
        if (rb.angularVelocity.magnitude > maxAngularVelocity)
        {
            rb.angularVelocity = rb.angularVelocity.normalized * maxAngularVelocity;
        }
    }

    private void LateUpdate()
    {
        if (frontIndicator != null)
        {
            Front = Vector3.Slerp(Front, lastInputDir, indicatorSmoothSpeed * Time.deltaTime);

            Vector3 targetPos = transform.position + Front * (ballRadius * indicatorOffsetMultiplier);
            Quaternion targetRot = Quaternion.LookRotation(Front, Vector3.up);

            frontIndicator.position = targetPos;
            frontIndicator.rotation = targetRot;
        }
    }
}