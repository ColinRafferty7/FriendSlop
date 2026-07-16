using UnityEngine;
using System;

public class BallController : MonoBehaviour
{
    bool isGrounded = false;
    [SerializeField] private Collider col;
    [SerializeField] float friction = 0.01f;
    [SerializeField] float speed = 0.1f;
    [SerializeField] private Rigidbody rb;
    void Start()
    {

    }
    
    void Update()
    {
        Vector3 ballBottom = new Vector3(col.bounds.center.x, (col.bounds.min.y)+0.1f, col.bounds.center.z);
        isGrounded = Physics.Raycast(ballBottom, Vector3.down, 0.3f);
        print(isGrounded);
        //if (isGrounded)
        //{
        //    if (rb.linearVelocity.x != 0)
        //    {
        //        rb.linearVelocity.x -= (rb.linearVelocity.x / Math.Abs(rb.linearVelocity.x)) * friction;
        //    }
        //    if (rb.linearVelocity.z != 0)
        //    {
        //        rb.linearVelocity.z -= (rb.linearVelocity.z / Math.Abs(rb.linearVelocity.z)) * friction;
        //    }
            if (rb.linearVelocity.magnitude <= 5)
            {
                Vector3 deltaDir = new Vector3(Input.GetAxisRaw("Horizontal"), 0, Input.GetAxisRaw("Vertical"));
                deltaDir.Normalize();
                 //rb.linearVelocity += deltaDir * speed;
                rb.AddForce(deltaDir * speed);
            }
        Debug.Log(rb.linearVelocity);
        //}
    }
}
