using UnityEngine;
using Unity.Netcode;

public class PlayerInput : NetworkBehaviour
{
    private PlayerPhysics physics;
    private PlayerItems items;

    private void Awake()
    {
        physics = GetComponent<PlayerPhysics>();
        items = GetComponent<PlayerItems>();
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        if (!IsOwner && !IsServer) Destroy(this);
    }

    private void Update()
    {
        if (!IsOwner) return;

        ReadMovementDir();

        ReadJumpInput();

        ReadItemInputs();
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

    private void ReadJumpInput()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            Debug.Log($"SEND: {Time.realtimeSinceStartup:F4}");
            ApplyJumpRpc();
        }
    }

    [Rpc(SendTo.Server)]
    public void ApplyJumpRpc()
    {
        Debug.Log($"RECEIVE: {Time.realtimeSinceStartup:F4}");
        physics.ApplyJumpForce();
    }

    private void ReadItemInputs()
    {

        if (Input.GetKeyDown(KeyCode.LeftShift))
        {
            ActivateItemRpc();
        }
        if (Input.GetKeyDown(KeyCode.Q))
        {
            SwapAbilityRpc(1);
        }
        else if (Input.GetKeyDown(KeyCode.E))
        {
            SwapAbilityRpc(-1);
        }

    }

    [Rpc(SendTo.Server)]
    public void ActivateItemRpc()
    {
        items.AttemptActivation();
    }

    [Rpc(SendTo.Server)]
    public void SwapAbilityRpc(int index)
    {
        items.SwapAbility(index);
    }
}
