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

    #region ========== Input smoothing ===================
    [Tooltip("How quickly applied input ramps toward the actual input direction. Lower = smoother, less abrupt starts/stops. Higher = snappier, more immediate.")]
    [SerializeField] float inputSmoothTime = 0.15f;

    Vector3 smoothedDelta = Vector3.zero;
    Vector3 smoothedDeltaVelocity = Vector3.zero;
    #endregion

    #region ========== Local physics states =============
    float currentForceMultiplier = 1f;
    float currentJumpMultiplier = 1f;
    float currentTorqueMultiplier = 1f;
    float speed = 0.1f;

    Vector3 currentPlatformVelocity = Vector3.zero;
    Vector3 trackedPlatformVelocity = Vector3.zero;
    Vector3 ownVelocity = Vector3.zero;

    Vector3 externalVelocity = Vector3.zero;

    Vector3 movementDir = Vector3.zero;

    bool currentIsSlipping = false;
    bool currentIsSticky = false;
    bool justLanded = false;
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
        rb.WakeUp();

        bool wasGrounded = surfaceController.groundContacts;

        surfaceController.ResolveGroundState();

        justLanded = !wasGrounded && surfaceController.groundContacts;

        CaptureLandingMomentum();

        PhysicsCalculations(movementDir);
    }

    private void CaptureLandingMomentum()
    {
        if (!justLanded) return;

        Vector3 horizontalVelocity = new Vector3(rb.linearVelocity.x, 0, rb.linearVelocity.z);

        ownVelocity = horizontalVelocity;
    }

    public void ApplyJumpForce()
    {
        if (!surfaceController.groundContacts) return;

        rb.AddForce(Vector3.up * stats.GetJumpForce(), ForceMode.Impulse);
    }

    public void SetMovementDelta(Vector3 delta)
    {
        movementDir = delta.normalized;
    }

    public void AddForce(Vector3 force, ForceMode mode = ForceMode.Force)
    {
        Vector3 velocityDelta;

        switch (mode)
        {
            case ForceMode.Force:
                velocityDelta = (force / rb.mass) * Time.fixedDeltaTime;
                break;

            case ForceMode.Acceleration:
                velocityDelta = force * Time.fixedDeltaTime;
                break;

            case ForceMode.Impulse:
                velocityDelta = force / rb.mass;
                break;

            case ForceMode.VelocityChange:
                velocityDelta = force;
                break;

            default:
                velocityDelta = Vector3.zero;
                break;
        }

        externalVelocity += velocityDelta;
    }

    private void PhysicsCalculations(Vector3 delta)
    {
        smoothedDelta = Vector3.SmoothDamp(smoothedDelta, delta, ref smoothedDeltaVelocity, inputSmoothTime, Mathf.Infinity, Time.fixedDeltaTime);

        ApplyRoll();

        ApplySlopeGravity();

        ApplyAcceleration(smoothedDelta);

        ApplyWallCollision();

        ApplyTorque(smoothedDelta);

        DampVelocity();

        LeavingPlatform();

        ClampVelocity();

        PlatformCatchUp();

        Vector3 horizontalVelocity = ownVelocity + trackedPlatformVelocity + externalVelocity;

        ApplyVelocity(horizontalVelocity);

        if (rb.angularVelocity.magnitude > maxAngularVelocity)
        {
            rb.angularVelocity = rb.angularVelocity.normalized * maxAngularVelocity;
        }

        surfaceController.SetWasOnPlatform();
    }
    private void ApplyRoll()
    {
        if (!surfaceController.groundContacts) return;
        if (justLanded) return;

        Vector3 relativeVelocity = CalculateRelativeVelocity(rb);

        if (surfaceController.surfaceType != SurfaceType.Slippery && relativeVelocity.magnitude > 0.01f)
        {
            float angularSpeed = relativeVelocity.magnitude / stats.GetBallRadius();

            Vector3 rotationAxis = Vector3.Cross(Vector3.up, relativeVelocity.normalized);

            rb.angularVelocity = rotationAxis * angularSpeed;
        }
    }

    private void ApplySlopeGravity()
    {
        if (!surfaceController.groundContacts) return;
        if (surfaceController.surfaceType == SurfaceType.Sticky) return;

        Vector3 slideComponent = Vector3.ProjectOnPlane(Physics.gravity, surfaceController.surfaceNormal);

        ownVelocity += slideComponent * Time.fixedDeltaTime;
    }

    private void ApplyAcceleration(Vector3 delta)
    {
        float appliedForceMultiplier = surfaceController.groundContacts ? 1f : airControl;

        if (surfaceController.groundContacts && surfaceController.surfaceType != SurfaceType.Sticky)
        {
            appliedForceMultiplier *= GetClimbMultiplier(delta);
        }

        Vector3 inputAcceleration = delta * stats.GetSpeed() * appliedForceMultiplier / rb.mass;

        ownVelocity += inputAcceleration * Time.fixedDeltaTime;
    }

    private float GetClimbMultiplier(Vector3 delta)
    {
        if (delta.sqrMagnitude < 0.0001f) return 1f;

        Vector3 gravity = Physics.gravity;

        Vector3 downhill = gravity - Vector3.Project(gravity, surfaceController.surfaceNormal);

        if (downhill.sqrMagnitude < 0.0001f) return 1f;

        Vector3 uphillDir = -downhill.normalized;

        float uphillAlignment = Vector3.Dot(delta.normalized, uphillDir);

        if (uphillAlignment <= 0f) return 1f;

        float slopeAngle = Vector3.Angle(surfaceController.surfaceNormal, Vector3.up);

        float t = Mathf.Clamp01(slopeAngle / surfaceController.maxSurfaceAngle);

        float climbMultiplier = 1f - Mathf.SmoothStep(0f, 1f, t);

        return Mathf.Lerp(1f, climbMultiplier, uphillAlignment);
    }

    private void ApplyWallCollision()
    {
        if (!surfaceController.wallContact) return;

        if (surfaceController.wallSurfaceType != SurfaceType.Sticky && !surfaceController.groundContacts)
        {
            Vector3 downhillAlongWall = Physics.gravity - Vector3.Project(Physics.gravity, surfaceController.wallNormal);

            if (downhillAlongWall.sqrMagnitude > 0.0001f)
            {
                Vector3 downhillDir = downhillAlongWall.normalized;

                float downhillComponent = Vector3.Dot(ownVelocity, downhillDir);

                if (downhillComponent > 0f)
                {
                    ownVelocity -= downhillDir * downhillComponent;
                }
            }
        }

        float intoWall = Vector3.Dot(ownVelocity, -surfaceController.wallNormal);

        if (intoWall > 0f)
        {
            ownVelocity += surfaceController.wallNormal * intoWall;
        }
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

        externalVelocity *= dampingFactor;
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
    private void OnTriggerEnter(Collider other)
    {
        Debug.Log("Hit: " + other.gameObject.name);

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

        trackedPlatformVelocity = Vector3.MoveTowards(trackedPlatformVelocity, desiredPlatformVelocity, platformCatchUpRate * Time.fixedDeltaTime);
    }

    private void ApplyVelocity(Vector3 horVelo)
    {
        float verticalVelocity = Mathf.Clamp(rb.linearVelocity.y, -MAX_VERTICAL_SPEED, MAX_VERTICAL_SPEED);

        rb.linearVelocity = new Vector3(horVelo.x, verticalVelocity, horVelo.z);
    }

    private Vector3 CalculateRelativeVelocity(Rigidbody rb)
    {
        Vector3 relativeVelocity = ownVelocity - trackedPlatformVelocity;

        return relativeVelocity;
    }
}