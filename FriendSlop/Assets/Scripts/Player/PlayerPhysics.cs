using UnityEngine;
using Unity.Netcode;

public class PlayerPhysics : NetworkBehaviour
{
    #region ========== Global ball data =================
    private Rigidbody rb;
    private SphereCollider col;

    private PlayerStats stats;
    private SurfaceController surfaceController;

    private float ballRadius;
    #endregion

    #region ========== Network dependent data ===========
    public NetworkVariable<bool> groundContacts = new(false);
    #endregion

    #region ========== Physics calculation values =======
    [SerializeField] float airControl = 0.5f;
    [SerializeField] float torqueAmount = 5f;
    [SerializeField] float maxHorizontalSpeed = 5f;
    [SerializeField] float MAX_VERTICAL_SPEED = 50f;
    [SerializeField] float maxAngularVelocity = 1f;

    [Tooltip("How quickly the ball's tracked velocity ramps to match a newly-landed-on platform, avoiding an instant pop. Higher = faster pickup.")]
    [SerializeField] float platformCatchUpRate = 1000f;
    #endregion

    #region ========== Local physics states =============
    float currentForceMultiplier = 1f;
    float currentJumpMultiplier = 1f;
    float currentTorqueMultiplier = 1f;
    float speed = 0.1f;

    Vector3 currentPlatformVelocity = Vector3.zero;
    Vector3 trackedPlatformVelocity = Vector3.zero;
    Vector3 ownVelocity = Vector3.zero;

    Vector3 movementDir = Vector3.zero;

    bool currentIsSlipping = false;
    bool currentIsSticky = false;
    #endregion

    private void Start()
    {
        rb = GetComponent<Rigidbody>();
        col = GetComponent<SphereCollider>();
        stats = GetComponent<PlayerStats>();
        surfaceController = GetComponent<SurfaceController>();
        ballRadius = col.radius * transform.lossyScale.x;
    }

    private void FixedUpdate()
    {
        PhysicsCalculations(movementDir);
    }

    public void SetMovementDelta(Vector3 delta)
    {
        movementDir = delta.normalized;
    }

    private void PhysicsCalculations(Vector3 delta)
    {
        ApplyRoll();

        HandleStickySurface(delta);

        ApplyAcceleration(delta);

        ApplyTorque(delta);

        DampVelocity();

        LeavingPlatform();

        ClampVelocity();

        PlatformCatchUp();

        Vector3 horizontalVelocity = ownVelocity + trackedPlatformVelocity;

        ApplyVelocity(horizontalVelocity);

        if (rb.angularVelocity.magnitude > maxAngularVelocity)
        {
            rb.angularVelocity = rb.angularVelocity.normalized * maxAngularVelocity;
        }

        surfaceController.SetWasOnPlatform();
    }

    private void ApplyRoll()
    {
        Vector3 relativeVelocity = CalculateRelativeVelocity(rb);

        if (surfaceController.surfaceType != SurfaceType.Slippery && relativeVelocity.magnitude > 0.01f)
        {
            float angularSpeed = relativeVelocity.magnitude / stats.GetBallRadius();

            Vector3 rotationAxis = Vector3.Cross(Vector3.up, relativeVelocity.normalized);

            rb.angularVelocity = rotationAxis * angularSpeed;
        }
    }

    private void HandleStickySurface(Vector3 delta)
    {
        if (surfaceController.surfaceType == SurfaceType.Sticky)
        {
            Vector3 gravityForce = Physics.gravity * rb.mass;
            Vector3 slideComponent = gravityForce - Vector3.Project(gravityForce, surfaceController.surfaceNormal);

            float inputAlignment = delta.magnitude > 0.01f ? Vector3.Dot(delta.normalized, slideComponent.normalized) : -1f;

            if (inputAlignment < 0.3f)
            {
                rb.AddForce(-slideComponent);
            }
        }
    }

    private void ApplyAcceleration(Vector3 delta)
    {
        float appliedForceMultiplier = surfaceController.groundContacts ? 1f : airControl;
        Vector3 inputAcceleration = delta * stats.GetSpeed() * appliedForceMultiplier / rb.mass;
        ownVelocity += inputAcceleration * Time.fixedDeltaTime;
    }

    private void ApplyTorque(Vector3 delta)
    {
        Vector3 torqueAxis = Vector3.Cross(Vector3.up, delta);

        if (surfaceController.groundContacts)
        {
            rb.AddTorque(torqueAxis * stats.GetTorque(), ForceMode.Force);
        }
        else
        {
            rb.AddTorque(torqueAxis * stats.GetTorque() * airControl * 0.5f, ForceMode.Force);
        }
    }

    private void DampVelocity()
    {
        float dampingFactor = 1f / (1f + rb.linearDamping * Time.fixedDeltaTime);
        ownVelocity *= dampingFactor;
    }

    private void LeavingPlatform()
    {
        bool justLeftPlatform = surfaceController.WasOnPlatformLastFrame && !surfaceController.IsOnPlatform;

        if (justLeftPlatform)
        {
            ownVelocity += trackedPlatformVelocity;
            trackedPlatformVelocity = Vector3.zero;
        }
    }

    private void ClampVelocity()
    {
        if (ownVelocity.magnitude > maxHorizontalSpeed)
        {
            ownVelocity = ownVelocity.normalized * maxHorizontalSpeed;
        }
    }

    private void PlatformCatchUp()
    {
        Vector3 desiredPlatformVelocity = surfaceController.IsOnPlatform ? surfaceController.currentPlatformVelocity : Vector3.zero;

        trackedPlatformVelocity = Vector3.MoveTowards(
            trackedPlatformVelocity,
            desiredPlatformVelocity,
            platformCatchUpRate * Time.fixedDeltaTime);
    }

    private void ApplyVelocity(Vector3 horVelo)
    {
        float verticalVelocity = Mathf.Clamp(rb.linearVelocity.y, -MAX_VERTICAL_SPEED, MAX_VERTICAL_SPEED);
        rb.linearVelocity = new Vector3(horVelo.x, verticalVelocity, horVelo.z);
    }

    private Vector3 CalculateRelativeVelocity(Rigidbody rb)
    {
        Vector3 currentVelocity = rb.linearVelocity;

        Vector3 worldVelocity = new Vector3(currentVelocity.x, 0, currentVelocity.z);

        Vector3 relativeVelocity = worldVelocity - trackedPlatformVelocity;

        return relativeVelocity;
    }
}
