using UnityEngine;

[CreateAssetMenu(fileName = "NewSurfaceData", menuName = "Surfaces/Surface Data")]
public class SurfaceData : ScriptableObject
{
    [Header("Behavior Flags")]
    public bool isSlippingSurface = false;
    public bool isStickySurface = false;

    [Header("Friction")]
    public float linearFriction = 1f;
    public float angularFriction = 0.1f;

    [Header("Multipliers")]
    public float forceMultiplier = 1f;
    public float jumpMultiplier = 1f;
    public float torqueMultiplier = 0f;

    [Header("Slipping Override")]
    [Tooltip("Only used when isSlippingSurface is true - overrides the ball's normal radius-based max angular velocity.")]
    public float maxAngularVelocityOverride = 1f;
}
