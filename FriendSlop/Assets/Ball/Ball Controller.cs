using Unity.Netcode;
using UnityEngine;

public class BallController : NetworkBehaviour
{
    [SerializeField] private Rigidbody rb;
    void Start()
    {
    
    }

    void Update()
    {
        if (!IsOwner) return;

        Vector3 dir = new Vector3(Input.GetAxisRaw("Horizontal")*3, rb.linearVelocity.y , Input.GetAxisRaw("Vertical")*10);  
        rb.linearVelocity = dir;
    }
}
