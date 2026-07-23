using UnityEngine;
using Unity.Netcode;

public class InputControl : NetworkBehaviour
{
    [SerializeField] private Rigidbody rb; 
    void Update()
    {
        // Network objects are owned by the specific instance of the game
        // This makes sure that the inputs are only beinng read from the owner
        if (!IsOwner) return;

        // Takes the main input types (Hoizontal, Jump, and Vertical) and stores it as a vector3
        // If the game starts slowing down, movement can be stored in smaller packets
        Vector3 deltaDir = new Vector3(Input.GetAxisRaw("Horizontal"), Input.GetKeyDown(KeyCode.Space) ? 1f : 0f, Input.GetAxisRaw("Vertical"));

        // Only send the packet to the server if there are inputs
        if (deltaDir !=  Vector3.zero) SendInputToHostRpc(deltaDir);
    }

    [Rpc(SendTo.Server)]
    private void SendInputToHostRpc(Vector3 packet)
    {
        rb.AddForce(packet * 200f);
    }
}
