using UnityEngine;
using System;




public class BallController : MonoBehaviour
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
    Vector3 deltaDir;
    

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

    void OnCollisionStay(Collision collision)
    {
        Debug.Log("Collision!");
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
        if (!isPlayer) return;
        deltaDir = new Vector3(Input.GetAxisRaw("Horizontal"), 0, Input.GetAxisRaw("Vertical"));
        if (groundContacts == true && Input.GetKeyDown(KeyCode.Space))
        {
            rb.AddForce(Vector3.up * jumpForce * currentJumpMultiplier, ForceMode.Impulse);
        }
    }
    void FixedUpdate()
    {
        Debug.Log(groundContacts);
        Debug.Log(rb.angularDamping);
        Debug.Log(rb.linearDamping);
        Debug.Log(currentJumpMultiplier);
        Debug.Log(currentForceMultiplier);
        Debug.Log(currentTorqueMultiplier);
        Vector3 deltaDir = new Vector3(Input.GetAxisRaw("Horizontal"), 0, Input.GetAxisRaw("Vertical"));
        Vector3 torqueAxis = Vector3.Cross(Vector3.up, deltaDir);
        Vector3 horizontalVelocity = new Vector3(rb.linearVelocity.x, 0, rb.linearVelocity.z);
        deltaDir.Normalize();
        float verticalVelocity = rb.linearVelocity.y;
        if (!currentIsSlipping && horizontalVelocity.magnitude > 0.01f)
        {
            float angularSpeed = horizontalVelocity.magnitude / ballRadius;
            Vector3 rotationAxis = Vector3.Cross(Vector3.up, horizontalVelocity.normalized);
            rb.angularVelocity = rotationAxis * angularSpeed;
        }
        if (groundContacts == true)
        {
            rb.AddForce(deltaDir * currentForceMultiplier * speed);
            rb.AddTorque(torqueAxis * torqueAmount * currentTorqueMultiplier, ForceMode.Force);
        }
        if (groundContacts == false)
        {
            rb.AddForce(deltaDir * currentForceMultiplier * speed * airControl);
            rb.AddTorque(torqueAxis * torqueAmount * currentTorqueMultiplier * airControl * 0.5f, ForceMode.Force);
        }
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
}
