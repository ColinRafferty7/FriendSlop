using UnityEngine;

public class OrbitAroundLocalPoint : MonoBehaviour
{
    [SerializeField] Vector3 localPivotOffset = new Vector3(-0.01044f, 0.02824f, 0f); // offset from the object's own origin, in its own local space
    [SerializeField] Vector3 rotationAxis = Vector3.up;
    [SerializeField] float degreesPerSecond = 30f;

    void Update()
    {
        Vector3 worldPivot = transform.TransformPoint(localPivotOffset);
        transform.RotateAround(worldPivot, rotationAxis, degreesPerSecond * Time.deltaTime);
    }
}
