using System.Collections.Specialized;
using UnityEngine;

public class BallController : MonoBehaviour
{
    [SerializeField] private Rigidbody rb;
    void Start()
    {
    
    }

    void Update()
    {
        Vector3 dir = new Vector3(Input.GetAxisRaw("Horizontal")*10, rb.linearVelocity.y , Input.GetAxisRaw("Vertical")*10);  
        rb.linearVelocity = dir;
    }
}
