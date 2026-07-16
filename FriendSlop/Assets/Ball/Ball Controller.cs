using UnityEngine;
using System;



public class BallController : MonoBehaviour
{
    [SerializeField] private Collider col;
    [SerializeField] float speed = 0.1f;
    [SerializeField] private Rigidbody rb;
    [SerializeField] float jumpForce = 10f;
    [SerializeField] float maxSpeed = 5f;

    [System.Serializable]
    public class SurfaceDrag
    {
        public PhysicsMaterial material;
        public float angularDrag;
        public float linearDrag;
        public float forceMultiplier = 1f;
        public float jumpMultiplier = 1f;
    }

    public SurfaceDrag[] surfaceDrags;
    public float airAngularDrag = 0f;
    public float defaultJumpMultiplier = 1f;
    public float defaultForceMultiplier = 1f;
    int groundContacts = 0;
    float currentJumpMultiplier = 1f;
    float currentForceMultiplier = 1f;

    void OnCollisionEnter(Collision collision)
    {

        PhysicsMaterial mat = collision.collider.sharedMaterial;
        if (mat != null)
        {
            groundContacts++;
            ApplySurfaceValues(mat);
            Debug.Log(mat.name);
            
        }
    }
    void OnCollisionExit(Collision collision)
    {
        if (collision.collider.sharedMaterial != null)
        {
            groundContacts--;
            if(groundContacts <= 0)
            {
                rb.angularDamping = airAngularDrag;
                currentJumpMultiplier = defaultJumpMultiplier;
                currentForceMultiplier = defaultForceMultiplier;
               
            }
            Debug.Log(groundContacts);
        }
    }
    void ApplySurfaceValues(PhysicsMaterial mat)
    {
        foreach (var entry in surfaceDrags)
        {
            if (entry.material == mat)
            {
                rb.angularDamping = entry.angularDrag;
                rb.linearDamping = entry.linearDrag;
                Debug.Log("MATCHED: " + entry.material.name + " angularDrag=" + entry.angularDrag);
                currentJumpMultiplier = entry.jumpMultiplier;
                currentForceMultiplier = entry.forceMultiplier;
                return;
            }
        }
        Debug.Log("NO MATCH — falling back to default");
        rb.angularDamping = airAngularDrag;
        currentJumpMultiplier = defaultJumpMultiplier;
        currentForceMultiplier = defaultForceMultiplier;
    }
    void Start()
    {
        rb.angularDamping = airAngularDrag;
    }
    
    void Update()
    { 
        Vector3 deltaDir = new Vector3(Input.GetAxisRaw("Horizontal"), 0, Input.GetAxisRaw("Vertical"));
        deltaDir.Normalize();
        if (groundContacts > 0)
        {
            rb.AddForce(deltaDir * currentForceMultiplier * speed);
        }
        if (rb.linearVelocity.magnitude > maxSpeed)
        {
            rb.linearVelocity = rb.linearVelocity.normalized * maxSpeed;
        }
        if (groundContacts >= 0 && Input.GetKeyDown(KeyCode.Space))
        {
            rb.AddForce(Vector3.up * jumpForce * currentJumpMultiplier, ForceMode.Impulse);
        }
        Debug.Log("Current angularDamping: " + rb.angularDamping);

    }
}
