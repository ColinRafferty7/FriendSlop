using System.Collections;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

public class CameraTarget : NetworkBehaviour
{
    private Coroutine registerCoroutine;

    public override void OnNetworkSpawn()
    {
        registerCoroutine = StartCoroutine(RegisterWhenReady());

        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        StartCoroutine(RegisterWhenReady());
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

    // Moved this function into camera target because its camera/player
    // related, but it could hypothetically go somewhere else as a static function
    public static Vector3 GetCameraRelativeInputDirection(float horizontal, float vertical)
    {
        Transform camTransform = DynamicCameraController.Instance != null
            ? DynamicCameraController.Instance.transform
            : null;


        Vector3 camForward = camTransform != null ? camTransform.forward : Vector3.forward;
        Vector3 camRight = camTransform != null ? camTransform.right : Vector3.right;

        camForward.y = 0f;
        camRight.y = 0f;


        if (camForward.sqrMagnitude > 0.0001f) camForward.Normalize();
        if (camRight.sqrMagnitude > 0.0001f) camRight.Normalize();


        return camRight * horizontal + camForward * vertical;
    }
}