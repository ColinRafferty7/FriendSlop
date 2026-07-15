using System.Collections.Specialized;
using UnityEngine;
using System;

public class BallController : MonoBehaviour
{
    Vector3 dir = new Vector3(0f, 0f, 0f);
    [SerializeField] float friction = 0.01f;
    [SerializeField] float speed = 0.1f;
    [SerializeField] private Rigidbody rb;
    void Start()
    {
    
    }
    
    void Update()
    {
        //Vector3.x dir = new Vector3(Input.GetAxisRaw("Horizontal")*10, rb.linearVelocity.y , Input.GetAxisRaw("Vertical")*10);  
        if (dir.x != 0){
            dir.x -= (dir.x / Math.Abs(dir.x)) * friction;
        }
        if (dir.z != 0){
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
