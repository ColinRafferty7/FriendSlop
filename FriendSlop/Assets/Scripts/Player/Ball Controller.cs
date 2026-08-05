using UnityEngine;
using System;
using System.Collections.Generic;
using Unity.Netcode;
using System.Xml.Serialization;
using Unity.VisualScripting;
using UnityEngine.SceneManagement;


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
    [SerializeField] bool debugMode = false;
    public Vector3 Front { get; private set; } = Vector3.forward;
    Vector3 lastInputDir = Vector3.forward;

    Vector3 currentPlatformVelocity = Vector3.zero;

    Vector3 trackedPlatformVelocity = Vector3.zero;

    Vector3 ownVelocity = Vector3.zero;

    [SerializeField]
    [Tooltip("How quickly the ball's tracked velocity ramps to match a newly-landed-on platform, avoiding an instant pop. Higher = faster pickup.")]
    float platformCatchUpRate = 40f;

    bool wasOnPlatformLastFrame = false;


    Collider lastGroundCollider;
    SurfaceData currentSurfaceData;
    public bool IsOnPlatform { get; private set; } = false;


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
    public NetworkVariable<bool> groundContacts = new(false);
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

    public NetworkVariable<bool> IsAlive = new(true);
    public NetworkVariable<int> Score = new(0);
    [SerializeField] private MeshRenderer mesh;

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

        if (anyExpired)
            RecalculateStats();
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

    public void SetPlatformVelocity(Vector3 velocity)
    {
        currentPlatformVelocity = velocity;
    }
    public void SetOnPlatform(bool onPlatform)
    {
        IsOnPlatform = onPlatform;
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

    public void Eliminate()
    {
        if (!IsServer)
            return;

        IsAlive.Value = false;

        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;

        RoundManager.Instance.PlayerEliminated(this);
    }

    [Rpc(SendTo.Server)]
    public void EliminateRpc()
    {
        Eliminate();
    }

    public void ResetForRound(Vector3 spawnPoint)
    {
        if (!IsServer)
            return;

        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;

        rb.position = spawnPoint;
        rb.rotation = Quaternion.identity;

        IsAlive.Value = true;
    }

    [Rpc(SendTo.Server)]
    public void ResetForRoundRpc(Vector3 spawnPoint)
    {
        ResetForRound(spawnPoint);
    }


    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        if (IsServer)
        {
            IsAlive.Value = false;
        }

        OnAliveChanged(false, IsAlive.Value);
    }

    private void Awake()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
        if (debugMode)
        {
            IsAlive.Value = true;
        }
        else
        {
            IsAlive.OnValueChanged += OnAliveChanged;
        }
    }

    private void OnAliveChanged(bool prev, bool current)
    {
        Debug.Log($"${OwnerClientId}: IsAlive Changed to - {current}");
        if (current) Debug.Log($"${OwnerClientId}\nPosition: {rb.position}\nVelocity: {rb.linearVelocity}");
        mesh.enabled = current;
        col.enabled = current;
        frontIndicator.gameObject.SetActive(current);
    }

    void Start()
    {
        if (RoundManager.Instance != null) RoundManager.Instance.AddPlayer(this);
        rb.angularDamping = airAngularDrag;
        baseScale = transform.localScale;
        RecalculateStats();
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (!IsServer) return;

        if (scene.name.Contains("Map"))
        {
            RoundManager.Instance.AddPlayer(this);
        }
    }

    void Update()
    {
        if (IsServer && rb.position.y < -10f)
        {
            Debug.Log("Should be eliminated");
            Eliminate();
            return;
        }

        if (!IsOwner) return;
        if (!isPlayer) return;
        if (!IsAlive.Value) return;
        if (RoundManager.Instance.CurrentState.Value != RoundManager.RoundState.Playing) return;

        float h = Input.GetAxisRaw("Horizontal");
        float v = Input.GetAxisRaw("Vertical");

        deltaDir = GetCameraRelativeInputDirection(h, v);

        if (groundContacts.Value == true && Input.GetKeyDown(KeyCode.Space))
        {
            Debug.Log("Jump");
            ApplyJumpForceRpc();
        }


        if (Input.GetKeyDown(KeyCode.LeftShift))
            activatePressed = true;

        if (Input.GetKeyDown(KeyCode.Q))
            swapPressed = -1;
        else if (Input.GetKeyDown(KeyCode.E))
            swapPressed = 1;


        if (deltaDir.magnitude > 0.01f)
        {
            lastInputDir = deltaDir.normalized;
        }


    }


    [Rpc(SendTo.Server)]
    private void ApplyJumpForceRpc()
    {
        rb.AddForce(Vector3.up * jumpForce * currentJumpMultiplier, ForceMode.Impulse);
    }

    private Vector3 GetCameraRelativeInputDirection(float horizontal, float vertical)
    {
        Transform camTransform = DynamicCameraController.Instance != null
            ? DynamicCameraController.Instance.transform
            : null;


        Vector3 camForward = camTransform != null ? camTransform.forward : Vector3.forward;
        Vector3 camRight = camTransform != null ? camTransform.right : Vector3.right;

        camForward.y = 0f;
        camRight.y = 0f;


        if (camForward.sqrMagnitude > 0.0001f) camForward.Normalize();
        if (camRight.sqrMagnitude > 0.0001f) camRight.Normalize();


        return camRight * horizontal + camForward * vertical;
    }


    void FixedUpdate()
    {
        if (!IsAlive.Value) return;
        if (RoundManager.Instance.CurrentState.Value != RoundManager.RoundState.Playing) return;

        if (frameHasFloorContact)
        {
            groundContacts.Value = true;
        }
        else if (groundContacts.Value)
        {
            groundContacts.Value = false;
            lastGroundCollider = null;
            currentSurfaceData = null;
            ApplySurfaceValues(null);
        }

        frameHasFloorContact = false;


        if (!IsOwner) return;


        Vector3 torqueAxis = Vector3.Cross(Vector3.up, deltaDir);

        deltaDir.Normalize();


        PhysicsCalculationsRpc(torqueAxis, deltaDir, currentSurfaceNormal);


        if (swapPressed != 0)
        {
            SwapAbility(swapPressed);
            swapPressed = 0;
        }


        if (cooldownTimer > 0)
            cooldownTimer -= Time.fixedDeltaTime;


        if (currentAbility != null && (currentAbility.Type == AbilityType.Passive || currentAbility.Type == AbilityType.ActiveAndPassive))
        {
            currentAbility.PassiveTick(gameObject);
        }


        if (activatePressed && currentAbility != null && cooldownTimer <= 0 && (currentAbility.Type == AbilityType.Active || currentAbility.Type == AbilityType.ActiveAndPassive))
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

        Vector3 currentVelocity = rb.linearVelocity;

        Vector3 worldVelocity = new Vector3(currentVelocity.x, 0, currentVelocity.z);

        Vector3 relativeVelocity = worldVelocity - trackedPlatformVelocity;


        if (!currentIsSlipping && relativeVelocity.magnitude > 0.01f)
        {
            float angularSpeed = relativeVelocity.magnitude / ballRadius;

            Vector3 rotationAxis = Vector3.Cross(Vector3.up, relativeVelocity.normalized);

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

        float appliedForceMultiplier = groundContacts.Value ? 1f : airControl;
        Vector3 inputAcceleration = delta * currentForceMultiplier * speed * appliedForceMultiplier / rb.mass;
        ownVelocity += inputAcceleration * Time.fixedDeltaTime;

        if (groundContacts.Value)
        {
            rb.AddTorque(torqueAxis * torqueAmount * currentTorqueMultiplier, ForceMode.Force);
        }
        else
        {
            rb.AddTorque(torqueAxis * torqueAmount * currentTorqueMultiplier * airControl * 0.5f, ForceMode.Force);
        }

        float dampingFactor = 1f / (1f + rb.linearDamping * Time.fixedDeltaTime);
        ownVelocity *= dampingFactor;

        bool justLeftPlatform = wasOnPlatformLastFrame && !IsOnPlatform;

        if (justLeftPlatform)
        {
            ownVelocity += trackedPlatformVelocity;
            trackedPlatformVelocity = Vector3.zero;
        }

        if (ownVelocity.magnitude > maxHorizontalSpeed)
        {
            ownVelocity = ownVelocity.normalized * maxHorizontalSpeed;
        }

        Vector3 desiredPlatformVelocity = IsOnPlatform ? currentPlatformVelocity : Vector3.zero;

        trackedPlatformVelocity = Vector3.MoveTowards(
            trackedPlatformVelocity,
            desiredPlatformVelocity,
            platformCatchUpRate * Time.fixedDeltaTime);

        Vector3 horizontalVelocity = ownVelocity + trackedPlatformVelocity;

        float verticalVelocity = Mathf.Clamp(rb.linearVelocity.y, -maxVerticalSpeed, maxVerticalSpeed);
        rb.linearVelocity = new Vector3(horizontalVelocity.x, verticalVelocity, horizontalVelocity.z);

        if (rb.angularVelocity.magnitude > maxAngularVelocity)
        {
            rb.angularVelocity = rb.angularVelocity.normalized * maxAngularVelocity;
        }

        wasOnPlatformLastFrame = IsOnPlatform;
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