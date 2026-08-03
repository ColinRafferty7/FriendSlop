using System.Collections.Generic;
using UnityEngine;


[RequireComponent(typeof(Camera))]
public class DynamicCameraController : MonoBehaviour
{
    public static DynamicCameraController Instance { get; private set; }

    [Header("Targets")]
    [Tooltip("Populated at runtime via RegisterTarget/UnregisterTarget.")]
    [SerializeField] private List<Transform> targets = new List<Transform>();

    [Header("Framing")]
    [Tooltip("Extra world-space margin added around the players' bounding box before fitting it in view.")]
    [SerializeField] private float padding = 3f;

    [Tooltip("Minimum half-size to frame even with one player or all players stacked together to prevent extreme close-up zoom.")]
    [SerializeField] private float minExtent = 2f;

    [Tooltip("World-space offset added to the computed focus point.")]
    [SerializeField] private Vector3 focusOffset = Vector3.zero;

    [Header("Zoom (Distance Along Camera's Fixed Forward)")]
    [SerializeField] private float minDistance = 8f;
    [SerializeField] private float maxDistance = 50f;

    [Header("Smoothing")]
    [Tooltip("Roughly how many seconds panning takes to catch up.")]
    [SerializeField] private float positionSmoothTime = 0.35f;

    [Tooltip("Roughly how many seconds zoom takes to catch up.")]
    [SerializeField] private float zoomSmoothTime = 0.5f;

    [Header("References")]
    [Tooltip("Auto-filled from GetComponent if left empty.")]
    [SerializeField] private Camera targetCamera;

    private Vector3 positionVelocity;
    private float distanceVelocity;
    private float currentDistance;

    private void Awake()
    {
        Instance = this;

        if (targetCamera == null) targetCamera = GetComponent<Camera>();

        
        currentDistance = Mathf.Clamp(minDistance, minDistance, maxDistance);
    }

    public void RegisterTarget(Transform t)
    {
        if (t != null && !targets.Contains(t))
        {
            targets.Add(t);
        }
    }

    public void UnregisterTarget(Transform t)
    {
        targets.Remove(t);
    }

    private void LateUpdate()
    {
        
        targets.RemoveAll(t => t == null);

        if (targets.Count == 0) return;

        Bounds bounds = new Bounds(targets[0].position, Vector3.zero);
        for (int i = 1; i < targets.Count; i++)
        {
            bounds.Encapsulate(targets[i].position);
        }

        Vector3 center = bounds.center + focusOffset;

        
        Vector3 right = transform.right;
        Vector3 up = transform.up;

        float extentRight = minExtent;
        float extentUp = minExtent;

        for (int i = 0; i < targets.Count; i++)
        {
            Vector3 offset = targets[i].position - center;
            extentRight = Mathf.Max(extentRight, Mathf.Abs(Vector3.Dot(offset, right)));
            extentUp = Mathf.Max(extentUp, Mathf.Abs(Vector3.Dot(offset, up)));
        }

        extentRight += padding;
        extentUp += padding;

        
        float verticalFovRad = targetCamera.fieldOfView * Mathf.Deg2Rad;
        float distanceForHeight = extentUp / Mathf.Tan(verticalFovRad * 0.5f);

        float horizontalFovRad = 2f * Mathf.Atan(Mathf.Tan(verticalFovRad * 0.5f) * targetCamera.aspect);
        float distanceForWidth = extentRight / Mathf.Tan(horizontalFovRad * 0.5f);

        float requiredDistance = Mathf.Max(distanceForHeight, distanceForWidth);
        requiredDistance = Mathf.Clamp(requiredDistance, minDistance, maxDistance);

        currentDistance = Mathf.SmoothDamp(currentDistance, requiredDistance, ref distanceVelocity, zoomSmoothTime);

      
        Vector3 targetPosition = center - transform.forward * currentDistance;
        transform.position = Vector3.SmoothDamp(transform.position, targetPosition, ref positionVelocity, positionSmoothTime);
    }
}
