using UnityEngine;

public class SurfaceController : MonoBehaviour
{
    [Tooltip("Contacts steeper than this angle (degrees from straight up) are treated as walls, not floor/slopes, and don't count as ground contact or apply surface data.")]
    [SerializeField] float maxSurfaceAngle = 60f;
    [Tooltip("Maps PhysicsMaterial assets to SurfaceData for floors that don't have a SurfaceIdentifier component.")]
    [SerializeField] SurfaceMaterialRegistry surfaceRegistry;

    private PlayerStats stats;

    public Vector3 currentPlatformVelocity = Vector3.zero;
    public Vector3 trackedPlatformVelocity = Vector3.zero;

    bool frameHasFloorContact = false;

    Collider lastGroundCollider;
    public SurfaceData currentSurfaceData;
    public SurfaceType surfaceType;

    public bool IsOnPlatform {get; private set;}
    public bool WasOnPlatformLastFrame {get; private set;}

    public Vector3 surfaceNormal {get; private set;} = Vector3.up;

    public bool groundContacts = false;

    private void Awake()
    {
        stats = GetComponent<PlayerStats>();
    }

    public void OnCollisionStay(Collision collision)
    {
        Vector3 floorNormalSum = Vector3.zero;
        int floorContactCount = 0;

        foreach (var contact in collision.contacts)
        {
            if (Vector3.Angle(contact.normal, Vector3.up) <= maxSurfaceAngle)
            {
                floorNormalSum += contact.normal;
                floorContactCount++;
            }
        }

        surfaceNormal = (floorNormalSum / floorContactCount).normalized;

        if (floorContactCount == 0) return;

        frameHasFloorContact = true;
        groundContacts = true;


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
    }

    void OnCollisionExit(Collision collision)
    {
        if (collision.collider == lastGroundCollider)
        {
            lastGroundCollider = null;
            currentSurfaceData = null;
        }
        groundContacts = false;
    }

    void ApplySurfaceValues(SurfaceData data)
    {
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
