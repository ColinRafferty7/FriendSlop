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
        Vector3 dir = new Vector3(rb.linearVelocity.x, 0, rb.linearVelocity.z);  
        rb.linearVelocity = dir * 10;
    }
}
