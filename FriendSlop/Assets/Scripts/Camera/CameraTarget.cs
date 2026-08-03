using Unity.Netcode;
using UnityEngine;


public class CameraTarget : NetworkBehaviour
{
    public override void OnNetworkSpawn()
    {
        if (DynamicCameraController.Instance != null)
        {
            DynamicCameraController.Instance.RegisterTarget(transform);
        }
    }

    public override void OnNetworkDespawn()
    {
        if (DynamicCameraController.Instance != null)
        {
            DynamicCameraController.Instance.UnregisterTarget(transform);
        }
    }
}
