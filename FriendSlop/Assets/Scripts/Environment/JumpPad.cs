using Unity.Netcode;
using UnityEngine;

public class JumpPad : NetworkBehaviour
{
    [SerializeField] float jumpForce = 15f;

    private void OnTriggerEnter(Collider other)
    {
        if (!IsServer)
            return;

        Rigidbody rb = other.attachedRigidbody;

        if (rb == null)
            return;

        Vector3 velocity = rb.linearVelocity;
        velocity.y = jumpForce;
        rb.linearVelocity = velocity;
    }
}