using UnityEngine;
using System;
using System.Collections.Generic;
using Unity.Netcode;




public class BallController : NetworkBehaviour
{
    [SerializeField] bool isPlayer = false;
    [SerializeField] private SphereCollider col;
    [SerializeField] float speed = 0.1f;
    [SerializeField] private Rigidbody rb;
    [SerializeField] float jumpForce = 10f;
    [SerializeField] float maxHorizontalSpeed = 5f;
    [SerializeField] float torqueAmount = 5f;
    [SerializeField] float maxAngularVelocity = 5f;
    [SerializeField] float airControl = 0.25f;
    [SerializeField] float maxVerticalSpeed = 15f;
    float ballRadius;
    public float BallRadius => ballRadius;
    Vector3 deltaDir;
    [SerializeField] Transform frontIndicator;
    [SerializeField] float indicatorOffsetMultiplier = 1.5f;
    [SerializeField] float indicatorSmoothSpeed = 8f;
    public Vector3 Front { get; private set; } = Vector3.forward;
    Vector3 lastInputDir = Vector3.forward;



    [System.Serializable]
    public class SurfaceDrag
    {
        public bool isSlippingSurface = false;
        public PhysicsMaterial material;
        public float angularDrag;
        public float linearDrag;
        public float forceMultiplier = 1f;
        public float jumpMultiplier = 1f;
        public float torqueMultiplier = 0f;
    }

    public SurfaceDrag[] surfaceDrags;
    public float airAngularDrag = 0f;
    public float defaultJumpMultiplier = 1f;
    public float defaultForceMultiplier = 1f;
    public float defaultTorqueMultiplier = 1f;
    bool groundContacts = false;
    float currentJumpMultiplier = 1f;
    float currentForceMultiplier = 1f;
    float currentTorqueMultiplier = 1f;
    bool currentIsSlipping = false;

    List<AbilityBase> ownedAbilities = new List<AbilityBase>();
    int currentAbilityIndex = -1;
    AbilityBase currentAbility;
    float cooldownTimer = 0f;
    bool activatePressed = false;
    int swapPressed = 0;

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

    public void CollectAbility(AbilityBase prefab)
    {
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
        groundContacts = true;
        PhysicsMaterial mat = collision.collider.sharedMaterial;
        if (mat != null)
        {
            ApplySurfaceValues(mat);
        }
    }
    void OnCollisionExit(Collision collision)
    {
        if (collision.collider.sharedMaterial != null)
        {
            groundContacts = false;
            if(groundContacts == false)
            {
                rb.angularDamping = airAngularDrag;
                currentJumpMultiplier = defaultJumpMultiplier;
                currentForceMultiplier = defaultForceMultiplier;
                currentTorqueMultiplier = defaultTorqueMultiplier;
                currentIsSlipping = false;
            }
        }
    }
    void ApplySurfaceValues(PhysicsMaterial mat)
    {
        foreach (var entry in surfaceDrags)
        {
            if (entry.material == mat)
            {
                if (rb.angularDamping != entry.angularDrag)
                    rb.angularDamping = entry.angularDrag;
                if (rb.linearDamping != entry.linearDrag)
                    rb.linearDamping = entry.linearDrag;
                if (currentJumpMultiplier != entry.jumpMultiplier)
                    currentJumpMultiplier = entry.jumpMultiplier;
                if (currentForceMultiplier != entry.forceMultiplier)
                    currentForceMultiplier = entry.forceMultiplier;
                if (currentTorqueMultiplier != entry.torqueMultiplier)
                    currentTorqueMultiplier = entry.torqueMultiplier;
                if (currentIsSlipping != entry.isSlippingSurface)
                    currentIsSlipping = entry.isSlippingSurface;
                return;
            }
        }
        rb.angularDamping = airAngularDrag;
        currentJumpMultiplier = defaultJumpMultiplier;
        currentForceMultiplier = defaultForceMultiplier;
        currentTorqueMultiplier = defaultTorqueMultiplier;
        currentIsSlipping = false;
    }
    void Start()
    {
        rb.angularDamping = airAngularDrag;
        ballRadius = col.radius * transform.lossyScale.x;
    }
    void Update()
    {
        if (!IsOwner) return;
        if (!isPlayer) return;
        deltaDir = new Vector3(Input.GetAxisRaw("Horizontal"), 0, Input.GetAxisRaw("Vertical"));
        if (groundContacts == true && Input.GetKeyDown(KeyCode.Space))
        {
            rb.AddForce(Vector3.up * jumpForce * currentJumpMultiplier, ForceMode.Impulse);
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

    }
    void FixedUpdate()
    {
        if (!IsOwner) return;
        //Debug.Log(groundContacts);
        //Debug.Log(rb.angularDamping);
        //Debug.Log(rb.linearDamping);
        //Debug.Log(currentJumpMultiplier);
        //Debug.Log(currentForceMultiplier);
        //Debug.Log(currentTorqueMultiplier);
        Vector3 deltaDir = new Vector3(Input.GetAxisRaw("Horizontal"), 0, Input.GetAxisRaw("Vertical"));
        Vector3 torqueAxis = Vector3.Cross(Vector3.up, deltaDir);
        deltaDir.Normalize();
        float verticalVelocity = rb.linearVelocity.y;

        PhysicsCalculationsRpc(torqueAxis, verticalVelocity, deltaDir);

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
    private void PhysicsCalculationsRpc(Vector3 torqueAxis, float verticalVelocity, Vector3 delta)
    {
        Vector3 horizontalVelocity = new Vector3(rb.linearVelocity.x, 0, rb.linearVelocity.z);
        float angularSpeed = 0f;
        if (!currentIsSlipping && horizontalVelocity.magnitude > 0.01f)
        {
            angularSpeed = horizontalVelocity.magnitude / ballRadius;
            Vector3 rotationAxis = Vector3.Cross(Vector3.up, horizontalVelocity.normalized);
            rb.angularVelocity = rotationAxis * angularSpeed;
        }
        if (groundContacts == true)
        {
            rb.AddForce(delta * currentForceMultiplier * speed);
            rb.AddTorque(torqueAxis * torqueAmount * currentTorqueMultiplier, ForceMode.Force);
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
        verticalVelocity = Mathf.Clamp(verticalVelocity, -maxVerticalSpeed, maxVerticalSpeed);
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
