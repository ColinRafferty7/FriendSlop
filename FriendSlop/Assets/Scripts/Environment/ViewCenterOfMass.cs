using UnityEngine;
public class ViewCenterOfMass : MonoBehaviour
{
    private Rigidbody rb;
    public Transform centerOfMassTransform;

    void OnDrawGizmos()
    {
        Rigidbody rbRef = rb != null ? rb : GetComponent<Rigidbody>();
        if (rbRef == null) return;
        Gizmos.color = Color.red;
        Gizmos.DrawSphere(rbRef.worldCenterOfMass, 0.05f);
    }
}

