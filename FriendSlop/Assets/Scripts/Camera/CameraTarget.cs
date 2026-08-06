using System.Collections;
using Unity.Netcode;
using UnityEngine;

public class CameraTarget : NetworkBehaviour
{
    private Coroutine registerCoroutine;

    public override void OnNetworkSpawn()
    {
        registerCoroutine = StartCoroutine(RegisterWhenReady());
    }

    public override void OnNetworkDespawn()
    {
        if (registerCoroutine != null)
        {
            StopCoroutine(registerCoroutine);
            registerCoroutine = null;
        }

        if (DynamicCameraController.Instance != null)
        {
            DynamicCameraController.Instance.UnregisterTarget(transform);
        }
    }
    private IEnumerator RegisterWhenReady()
    {
        while (DynamicCameraController.Instance == null)
        {
            yield return null;
        }

        DynamicCameraController.Instance.RegisterTarget(transform);
        registerCoroutine = null;
    }
}