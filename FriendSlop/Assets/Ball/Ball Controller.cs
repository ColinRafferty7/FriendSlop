using System.Collections.Specialized;
using UnityEngine;
using System;
using System.Security.Cryptography;
using System.Runtime.InteropServices;

public class BallController : MonoBehaviour
{
    bool isGrounded = false;
    Vector3 dir = new Vector3(0f, 0f, 0f);
    Collider col;
    [SerializeField] float friction = 0.01f;
    [SerializeField] float speed = 0.1f;
    [SerializeField] private Rigidbody rb;
    void Start()
    {
        col = GetComponent<Collider>();
    }
    
    void Update()
    {
        //Vector3.x dir = new Vector3(Input.GetAxisRaw("Horizontal")*10, rb.linearVelocity.y , Input.GetAxisRaw("Vertical")*10);
        Vector3 ballBottom = new Vector3(col.bounds.center.x, (col.bounds.min.y)+0.1f, col.bounds.center.z);
        isGrounded = Physics.Raycast(ballBottom, Vector3.down, 0.3f);
        print(isGrounded);
        if (isGrounded)
        {
            if (dir.x != 0)
            {
                dir.x -= (dir.x / Math.Abs(dir.x)) * friction;
            }
            if (dir.z != 0)
            {
                dir.z -= (dir.z / Math.Abs(dir.z)) * friction;
            }
            if (dir.magnitude <= 5)
            {
                dir.x += Input.GetAxisRaw("Horizontal") * speed;
                dir.z += Input.GetAxisRaw("Vertical") * speed;
                rb.linearVelocity = dir;
            }
        }
    }
}
