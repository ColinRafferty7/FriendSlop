using UnityEngine;
using Unity.Netcode;

public class PlayerInput : NetworkBehaviour
{
    private PlayerPhysics physics;

    private void Awake()
    {
        physics = GetComponent<PlayerPhysics>();
    }

    private void Update()
    {
        ReadMovementDir();
        
    }

    private void ReadMovementDir()
    {
        float h = Input.GetAxisRaw("Horizontal");
        float v = Input.GetAxisRaw("Vertical");

        Vector3 deltaDir = CameraTarget.GetCameraRelativeInputDirection(h, v);

        ApplyMovementDirRpc(deltaDir);
    }

    [Rpc(SendTo.Server)]
    public void ApplyMovementDirRpc(Vector3 delta)
    {
        physics.SetMovementDelta(delta);
    }
}
