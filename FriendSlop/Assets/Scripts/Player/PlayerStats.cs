using NUnit.Framework.Internal.Commands;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static BallController;

public class PlayerStats : MonoBehaviour
{
    #region ========== Physics Related Stats ============
    [SerializeField] private float baseSpeed = 200f;
    private float speedMultiplier = 1f;

    [SerializeField] float maxSpeed = 5f;
    private float maxSpeedMultiplier = 1f;

    [SerializeField] private float baseJumpForce = 100f;
    private float jumpMultiplier = 1f;

    [SerializeField] private float baseTorque = 5f;
    private float torqueMultiplier = 1f;

    [SerializeField] private float maxAngularVelocity = 1f;
    [SerializeField] private float linearFriction = 1f;
    [SerializeField] private float angularFriction = 1f;
    [SerializeField] private float gravityMultiplier = 1f;

    List<ActiveBoost> activeBoosts = new List<ActiveBoost>();
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

    public void ActivateBoost(StatType statType, float multiplier, float duration)
    {
        StartCoroutine(ApplyTimedBoost(statType, multiplier, duration));
    }

    // Change later to add time to already active boosts
    // Deleted that previous logic to make code cleaner
    public IEnumerator ApplyTimedBoost(StatType statType, float multiplier, float duration)
    {
        ApplyBoost(statType, multiplier);
        yield return new WaitForSeconds(duration);
        ApplyBoost(statType, 1f / multiplier);
    }

    private void ApplyBoost(StatType statType, float multiplier)
    {
        switch (statType)
        {
            case StatType.Speed:
                baseSpeed *= multiplier;
                maxSpeedMultiplier *= multiplier;
                break;
            case StatType.JumpForce:
                baseJumpForce *= multiplier;
                break;
            case StatType.Size:
                transform.localScale *= multiplier;
                rb.mass *= multiplier;
                break;
        }
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
    public float GetMaxSpeed()
    {
        return maxSpeed * maxSpeedMultiplier;
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
