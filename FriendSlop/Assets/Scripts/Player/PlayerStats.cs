using UnityEngine;

public class PlayerStats : MonoBehaviour
{
    #region ========== Physics Related Stats ============
    [SerializeField] private float baseSpeed = 200f;
    private float speedMultiplier = 1f;

    [SerializeField] private float baseJumpForce = 100f;
    private float jumpMultiplier = 1f;

    [SerializeField] private float baseTorque = 5f;
    private float torqueMultiplier = 1f;

    [SerializeField] private float maxAngularVelocity = 1f;
    [SerializeField] private float linearFriction = 1f;
    [SerializeField] private float angularFriction = 1f;
    [SerializeField] private float gravityMultiplier = 1f;
    #endregion

    #region ========== Ball Related Stats ===============
    private float ballRadius;
    #endregion

    private Rigidbody rb;

    private void Start()
    {
        ballRadius = GetComponent<SphereCollider>().radius * transform.lossyScale.x;
        rb = GetComponent<Rigidbody>();
    }

    public void UpdateStats(SurfaceData surface)
    {
        speedMultiplier = surface.speedMultiplier;
        jumpMultiplier = surface.jumpMultiplier;
        torqueMultiplier = surface.torqueMultiplier;
        maxAngularVelocity = surface.maxAngularVelocity;
        linearFriction = surface.linearFriction;
        angularFriction = surface.angularFriction;

        rb.linearDamping = linearFriction;
        rb.angularDamping = angularFriction;

        maxAngularVelocity = 5f / ballRadius;
    }

    public float GetSpeed()
    {
        return speedMultiplier * baseSpeed;
    }

    public float GetJumpForce()
    {
        return jumpMultiplier * baseJumpForce;
    }

    public float GetTorque()
    {
        return torqueMultiplier * baseTorque;
    }

    public float GetGravityMultiplier()
    {
        return gravityMultiplier;
    }

    public float GetMaxAngularVelocity()
    {
        return maxAngularVelocity;
    }

    public float GetLinearFriction()
    {
        return linearFriction;
    }

    public float GetAngularFriction()
    {
        return angularFriction;
    }

    public float GetBallRadius()
    {
        return ballRadius;
    }
}
