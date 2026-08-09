using UnityEngine;

public class SetCenterOfMass : MonoBehaviour
{
    private Rigidbody rb;
    public Transform centerOfMassTransform;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        if (centerOfMassTransform != null)
        {
            rb.centerOfMass = centerOfMassTransform.localPosition;
        }
    }
}
