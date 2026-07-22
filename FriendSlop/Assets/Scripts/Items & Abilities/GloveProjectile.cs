using UnityEngine;
using System.Collections.Generic;

public class GloveProjectile : MonoBehaviour
{
    GameObject user;
    Transform userTransform;
    Vector3 direction;
    float pushForce;
    float range;
    float speed;
    float ballRadius;
    float spawnOffsetMultiplier;

    float currentDistance = 0f;
    bool retracting = false;
    HashSet<Rigidbody> alreadyHit = new HashSet<Rigidbody>();

    public void Init(GameObject user, Vector3 direction, float pushForce, float range, float speed, float ballRadius, float spawnOffsetMultiplier)
    {
        this.user = user;
        this.userTransform = user.transform;
        this.direction = direction;
        this.pushForce = pushForce;
        this.range = range;
        this.speed = speed;
        this.ballRadius = ballRadius;
        this.spawnOffsetMultiplier = spawnOffsetMultiplier;
    }

    void FixedUpdate()
    {
        if (!retracting)
        {
            currentDistance += speed * Time.fixedDeltaTime;
            if (currentDistance >= range)
            {
                currentDistance = range;
                retracting = true;
            }
        }
        else
        {
            currentDistance -= speed * Time.fixedDeltaTime;
            if (currentDistance <= 0f)
            {
                Destroy(gameObject);
                return;
            }
        }

        Vector3 basePos = userTransform.position + direction * (ballRadius * spawnOffsetMultiplier);
        transform.position = basePos + direction * currentDistance;
        transform.rotation = Quaternion.LookRotation(direction, Vector3.up);
    }

    void OnTriggerEnter(Collider other)
    {
        if (retracting) return;

        Rigidbody otherRb = other.attachedRigidbody;
        if (otherRb == null || other.gameObject == user || alreadyHit.Contains(otherRb))
            return;

        alreadyHit.Add(otherRb);

        Vector3 hitDir = (other.transform.position - transform.position).normalized;
        hitDir.y = 0;
        hitDir.Normalize();

        otherRb.AddForce(hitDir * pushForce, ForceMode.Impulse);
    }
}
//using UnityEngine;
//using System.Collections.Generic;

//public class GloveProjectile : MonoBehaviour
//{
//    GameObject user;
//    Transform userTransform;
//    Vector3 direction;
//    float range;
//    float speed;
//    float ballRadius;
//    float spawnOffsetMultiplier;
//    float pushMultiplier;
//    float currentDistance = 0f;
//    Rigidbody rb;
//    float distanceTraveled = 0f;
//    bool retracting = false;
//    HashSet<Rigidbody> alreadyHit = new HashSet<Rigidbody>();

//    public void Init(GameObject user, Vector3 direction, float range, float speed, float ballRadius, float spawnOffsetMultiplier, float pushMultiplier)
//    {
//        this.user = user;
//        this.userTransform = user.transform;
//        this.direction = direction;
//        this.range = range;
//        this.speed = speed;
//        this.ballRadius = ballRadius;
//        this.spawnOffsetMultiplier = spawnOffsetMultiplier;
//        this.pushMultiplier = pushMultiplier;

//        rb = GetComponent<Rigidbody>();

//        Vector3 spawnPos = userTransform.position + direction * (ballRadius * spawnOffsetMultiplier);
//        transform.position = spawnPos;
//        rb.linearVelocity = direction * speed; 
//    }

//    void FixedUpdate()
//    {
//        if (!retracting)
//        {
//            currentDistance += speed * Time.fixedDeltaTime;
//            if (currentDistance >= range)
//            {
//                currentDistance = range;
//                retracting = true;
//            }
//        }
//        else
//        {
//            currentDistance -= speed * Time.fixedDeltaTime;
//            if (currentDistance <= 0f)
//            {
//                Destroy(gameObject);
//                return;
//            }
//        }

//        Vector3 basePos = userTransform.position + direction * (ballRadius * spawnOffsetMultiplier);
//        Vector3 targetPos = basePos + direction * currentDistance;

//        Vector3 requiredVelocity = (targetPos - rb.position) / Time.fixedDeltaTime;
//        rb.linearVelocity = requiredVelocity;
//    }

//    void OnCollisionEnter(Collision collision)
//    {
//        if (retracting) return;

//        Rigidbody otherRb = collision.rigidbody;
//        if (otherRb == null || collision.gameObject == user || alreadyHit.Contains(otherRb))
//            return;

//        alreadyHit.Add(otherRb);


//        Vector3 impactVelocity = rb.linearVelocity;
//        otherRb.AddForce(impactVelocity * rb.mass * pushMultiplier, ForceMode.Impulse);
//    }
//}
