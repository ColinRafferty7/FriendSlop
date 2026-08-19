using Unity.Netcode;
using UnityEngine;

public class JumpPad : NetworkBehaviour
{
    [SerializeField] float jumpForce = 15f;
    Animator anim;

    private void Awake()
    {
        anim = GetComponent<Animator>();
    }

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

        StartAnimationRpc();
    }

    [Rpc(SendTo.ClientsAndHost)]
    public void StartAnimationRpc()
    {
        anim.SetTrigger("Jump");
    }
}