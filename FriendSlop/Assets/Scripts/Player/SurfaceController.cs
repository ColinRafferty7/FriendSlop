using UnityEngine;
public class SurfaceController : MonoBehaviour
{
    [Tooltip("Contacts steeper than this angle (degrees from straight up) are treated as walls, not floor/slopes, and don't count as ground contact or apply surface data. Also used by PlayerPhysics as the single climbability threshold: climb strength scales to zero at exactly this angle.")]
    public float maxSurfaceAngle = 60f;
    [Tooltip("Maps PhysicsMaterial assets to SurfaceData for floors that don't have a SurfaceIdentifier component.")]
    [SerializeField] SurfaceMaterialRegistry surfaceRegistry;
    private PlayerStats stats;
    public Vector3 currentPlatformVelocity = Vector3.zero;
    public Vector3 trackedPlatformVelocity = Vector3.zero;
    Collider lastGroundCollider;
    Collider lastWallCollider;
    public SurfaceData currentSurfaceData;
    public SurfaceType surfaceType;
    public SurfaceData currentWallSurfaceData;
    public SurfaceType wallSurfaceType;
    public bool IsOnPlatform { get; private set; }
    public bool WasOnPlatformLastFrame { get; private set; }
    public Vector3 surfaceNormal { get; private set; } = Vector3.up;
    public bool groundContacts = false;

    public bool wallContact { get; private set; }
    public Vector3 wallNormal { get; private set; } = Vector3.zero;

    // Accumulated across all OnCollisionStay calls since the last ResolveGroundState()
    Vector3 pendingFloorNormalSum = Vector3.zero;
    int pendingFloorContactCount = 0;
    bool pendingFloorContact = false;
    Collider pendingGroundCollider;

    Vector3 pendingWallNormalSum = Vector3.zero;
    int pendingWallContactCount = 0;
    bool pendingWallContact = false;
    Collider pendingWallCollider;

    private void Awake()
    {
        stats = GetComponent<PlayerStats>();
    }

    public void OnCollisionStay(Collision collision)
    {
        Vector3 floorNormalSum = Vector3.zero;
        int floorContactCount = 0;
        Vector3 wallNormalSum = Vector3.zero;
        int wallContactCount = 0;

        foreach (var contact in collision.contacts)
        {
            if (Vector3.Angle(contact.normal, Vector3.up) <= maxSurfaceAngle)
            {
                floorNormalSum += contact.normal;
                floorContactCount++;
            }
            else
            {
                wallNormalSum += contact.normal;
                wallContactCount++;
            }
        }

        if (floorContactCount > 0)
        {
            pendingFloorContact = true;
            pendingFloorNormalSum += floorNormalSum;
            pendingFloorContactCount += floorContactCount;

            if (collision.collider != lastGroundCollider)
            {
                pendingGroundCollider = collision.collider;
            }
        }

        if (wallContactCount > 0)
        {
            pendingWallContact = true;
            pendingWallNormalSum += wallNormalSum;
            pendingWallContactCount += wallContactCount;

            if (collision.collider != lastWallCollider)
            {
                pendingWallCollider = collision.collider;
            }
        }
    }

    // Called once per fixed-timestep physics step, from PlayerPhysics.FixedUpdate,
    // BEFORE PhysicsCalculations reads groundContacts/wallContact. Tied to the physics
    // cadence instead of render cadence to avoid frame-rate mismatch flicker.
    public void ResolveGroundState()
    {
        groundContacts = pendingFloorContact;

        if (pendingFloorContact)
        {
            surfaceNormal = (pendingFloorNormalSum / pendingFloorContactCount).normalized;

            if (pendingGroundCollider != null && pendingGroundCollider != lastGroundCollider)
            {
                lastGroundCollider = pendingGroundCollider;
                SurfaceIdentifier identifier = lastGroundCollider.GetComponent<SurfaceIdentifier>();
                if (identifier != null)
                {
                    currentSurfaceData = identifier.surfaceData;
                }
                else if (surfaceRegistry != null)
                {
                    currentSurfaceData = surfaceRegistry.GetSurfaceData(lastGroundCollider.sharedMaterial);
                }
                else
                {
                    currentSurfaceData = null;
                }
            }
            ApplySurfaceValues(currentSurfaceData);
        }
        else
        {
            lastGroundCollider = null;
            currentSurfaceData = null;
        }

        wallContact = pendingWallContact;
        wallNormal = pendingWallContact ? (pendingWallNormalSum / pendingWallContactCount).normalized : Vector3.zero;

        if (pendingWallContact)
        {
            if (pendingWallCollider != null && pendingWallCollider != lastWallCollider)
            {
                lastWallCollider = pendingWallCollider;
                SurfaceIdentifier wallIdentifier = lastWallCollider.GetComponent<SurfaceIdentifier>();
                if (wallIdentifier != null)
                {
                    currentWallSurfaceData = wallIdentifier.surfaceData;
                }
                else if (surfaceRegistry != null)
                {
                    currentWallSurfaceData = surfaceRegistry.GetSurfaceData(lastWallCollider.sharedMaterial);
                }
                else
                {
                    currentWallSurfaceData = null;
                }
            }
            // default(SurfaceType) rather than a named member, since we only ever branch
            // on "is this Sticky or not" — any non-sticky value (including no data found)
            // falls through to the same non-sticky wall behavior.
            wallSurfaceType = currentWallSurfaceData != null ? currentWallSurfaceData.surfaceType : default;
        }
        else
        {
            lastWallCollider = null;
            currentWallSurfaceData = null;
            wallSurfaceType = default;
        }

        pendingFloorContact = false;
        pendingFloorNormalSum = Vector3.zero;
        pendingFloorContactCount = 0;
        pendingGroundCollider = null;

        pendingWallContact = false;
        pendingWallNormalSum = Vector3.zero;
        pendingWallContactCount = 0;
        pendingWallCollider = null;
    }

    void ApplySurfaceValues(SurfaceData data)
    {
        if (data == null) return;
        surfaceType = data.surfaceType;
        stats.UpdateStats(data);
    }
    public void SetPlatformVelocity(Vector3 velocity)
    {
        currentPlatformVelocity = velocity;
    }
    public void SetOnPlatform(bool onPlatform)
    {
        IsOnPlatform = onPlatform;
    }

    public void SetWasOnPlatform()
    {
        WasOnPlatformLastFrame = IsOnPlatform;
    }
}