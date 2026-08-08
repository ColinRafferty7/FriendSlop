using UnityEngine;


public enum SurfaceType
{
    Normal,
    Slippery,
    Sticky
}

[CreateAssetMenu(fileName = "NewSurfaceData", menuName = "Surfaces/Surface Data")]
public class SurfaceData : ScriptableObject
{
    [Header("Surface Type Enum")]
    public SurfaceType surfaceType;

    [Header("Friction")]
    public float linearFriction = 1f;
    public float angularFriction = 0.1f;

    [Header("Multipliers")]
    public float speedMultiplier = 1f;
    public float jumpMultiplier = 1f;
    public float torqueMultiplier = 0f;

    [Header("Slipping Override")]
    [Tooltip("Only used when isSlippingSurface is true - overrides the ball's normal radius-based max angular velocity.")]
    public float maxAngularVelocity = 1f;
}
