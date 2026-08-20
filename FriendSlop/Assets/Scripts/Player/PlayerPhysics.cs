using UnityEngine;
using Unity.Netcode;
using UnityEditor.U2D;

public class PlayerPhysics : NetworkBehaviour
{
    #region ========== Global ball data =================
    private Rigidbody rb;

    private PlayerStats stats;
    private SurfaceController surfaceController;
    #endregion

    #region ========== Physics calculation values =======
    [SerializeField] float airControl = 0.5f;
    [SerializeField] float maxVerticalSpeed = 50f;
    [SerializeField] float maxAngularVelocity = 1f;
    //Percentage of max climbable angle at which the helper force starts tapering off significantly(basically you stop being able to climb)
    [SerializeField, Range(0f, 1f)] float climbTaperStartFraction = 0.75f;
    #endregion

    #region ========== Input smoothing ===================
    [SerializeField] float inputSmoothTime = 0.15f;

    Vector3 smoothedDelta = Vector3.zero;
    Vector3 smoothedDeltaVelocity = Vector3.zero;
    #endregion

    #region ========== Local physics states =============

    Vector3 movementDir = Vector3.zero;

    Vector3 externalVelocity = Vector3.zero;

    bool justLanded = false;
    #endregion

    #region ========== Public external-velocity accessors =============
    //trackers for external velocity in case we need to use them for non-physics purposes
    public Vector3 ExternalVelocity => externalVelocity;

    public float ExternalHorizontalSpeed => new Vector3(externalVelocity.x, 0, externalVelocity.z).magnitude;

    public float ExternalVerticalSpeed => Mathf.Abs(externalVelocity.y);

    public bool IsExternallyBoosted => externalVelocity.sqrMagnitude > 0.0001f;
    #endregion

    private void Start()
    {
        rb = GetComponent<Rigidbody>();
        stats = GetComponent<PlayerStats>();
        surfaceController = GetComponent<SurfaceController>();

        //making sure gravity is off because we apply gravity manually
        rb.useGravity = false;
    }

    private void FixedUpdate()
    {
        if (!IsServer) return;

        bool wasGrounded = surfaceController.groundContacts;

        surfaceController.ResolveGroundState();

        justLanded = !wasGrounded && surfaceController.groundContacts;

        PhysicsCalculations(movementDir);
    }

    //applies an upward force when player inputs the jump button
    public void ApplyJumpForce()
    {
        if (!surfaceController.groundContacts) return;
        AddForce(Vector3.up * stats.GetJumpForce(), ForceMode.Impulse);
    }

    public void SetMovementDelta(Vector3 delta)
    {
        movementDir = delta.normalized;
    }

    //used for any external forces (amount of force, force type)
    public void AddForce(Vector3 force, ForceMode mode = ForceMode.Force)
    {
        rb.AddForce(force, mode);
        externalVelocity += ComputeVelocityDelta(force, mode);
    }

    //used in AddForce to keep the velocity variable up to date with the force added
    private Vector3 ComputeVelocityDelta(Vector3 force, ForceMode mode)
    {
        switch (mode)
        {
            case ForceMode.Force:
                return (force / rb.mass) * Time.fixedDeltaTime;

            case ForceMode.Acceleration:
                return force * Time.fixedDeltaTime;

            case ForceMode.Impulse:
                return force / rb.mass;

            case ForceMode.VelocityChange:
                return force;

            default:
                return Vector3.zero;
        }
    }

    private void PhysicsCalculations(Vector3 delta)
    {
        //makes player input direction smoother, instead of snapping to that direction
        smoothedDelta = Vector3.SmoothDamp(smoothedDelta, delta, ref smoothedDeltaVelocity, inputSmoothTime, Mathf.Infinity, Time.fixedDeltaTime);

        ApplyGravity();

        ApplyInputForce(smoothedDelta);

        ApplyTorque(smoothedDelta);

        ApplyRoll();

        DecayExternalVelocityTracker();

        ClampVelocity();

        if (rb.angularVelocity.magnitude > maxAngularVelocity)
        {
            rb.angularVelocity = rb.angularVelocity.normalized * maxAngularVelocity;
        }

        surfaceController.SetWasOnPlatform();
    }

    //manual gravity function to be able to edit the multiplier if so desired
    private void ApplyGravity()
    {
        rb.AddForce(Physics.gravity * stats.GetGravityMultiplier(), ForceMode.Acceleration);
    }

    //Applies force to the ball based on player input (essentially internal forces)
    private void ApplyInputForce(Vector3 delta)
    {
        float appliedForceMultiplier = surfaceController.groundContacts ? 1f : airControl;

        Vector3 forceDir = delta;
        float climbMultiplier = 1f;

        if (surfaceController.groundContacts)
        {
            //points player input to be parallel to whatever surface the ball is on
            Vector3 projected = Vector3.ProjectOnPlane(delta, surfaceController.surfaceNormal);
            if (projected.sqrMagnitude > 0.0001f)
            {
                forceDir = projected.normalized;
            }

            //tapers climbing strength down as the slope approaches maxSurfaceAngle, but only as it gets close, allowing full climb force for shallower slopes
            float slopeAngle = Vector3.Angle(surfaceController.surfaceNormal, Vector3.up);
            float taperStartAngle = surfaceController.maxSurfaceAngle * climbTaperStartFraction;
            float taperRatio = Mathf.InverseLerp(taperStartAngle, surfaceController.maxSurfaceAngle, slopeAngle);
            climbMultiplier = 1f - Mathf.SmoothStep(0f, 1f, taperRatio);
        }

        Vector3 finalForce = forceDir * stats.GetSpeed() * appliedForceMultiplier * climbMultiplier;

        rb.AddForce(finalForce, ForceMode.Force);
    }

    //Applies an extra rotational force when the surface is slippery to give the impression that it is slippery
    private void ApplyTorque(Vector3 delta)
    {
        if (surfaceController.surfaceType != SurfaceType.Slippery)
            return;

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

    //Matches the roll of the ball to the horizontal velocity of the ball if the ball is on the ground and its not slippery
    private void ApplyRoll()
    {
        if (!surfaceController.groundContacts) return;
        if (justLanded) return;
        if (surfaceController.surfaceType == SurfaceType.Slippery) return;

        Vector3 relativeVelocity = CalculateRelativeVelocity();

        if (relativeVelocity.magnitude > 0.01f)
        {
            float angularSpeed = relativeVelocity.magnitude / stats.GetBallRadius();

            Vector3 rotationAxis = Vector3.Cross(Vector3.up, relativeVelocity.normalized);

            rb.angularVelocity = rotationAxis * angularSpeed;
        }
    }

    //decays external velocity trackers along with actual velocity
    private void DecayExternalVelocityTracker()
    {
        float dampingFactor = 1f / (1f + rb.linearDamping * Time.fixedDeltaTime);
        externalVelocity *= dampingFactor;
    }

    //Caps internal forces while keeping external forces uncapped
    private void ClampVelocity()
    {
        Vector3 velocity = rb.linearVelocity;

        Vector3 horizontal = new Vector3(velocity.x, 0, velocity.z);
        float horizontalExternalMag = new Vector3(externalVelocity.x, 0, externalVelocity.z).magnitude;
        float effectiveHorizontalCap = stats.GetMaxSpeed() + horizontalExternalMag;

        bool horizontalClamped = horizontal.magnitude > effectiveHorizontalCap;
        if (horizontalClamped)
        {
            horizontal = horizontal.normalized * effectiveHorizontalCap;
        }

        float verticalExternalMag = Mathf.Abs(externalVelocity.y);
        float effectiveVerticalCap = maxVerticalSpeed + verticalExternalMag;
        float vertical = Mathf.Clamp(velocity.y, -effectiveVerticalCap, effectiveVerticalCap);

        rb.linearVelocity = new Vector3(horizontal.x, vertical, horizontal.z);
    }

    private Vector3 CalculateRelativeVelocity()
    {
        return new Vector3(rb.linearVelocity.x, 0, rb.linearVelocity.z);
    }
}