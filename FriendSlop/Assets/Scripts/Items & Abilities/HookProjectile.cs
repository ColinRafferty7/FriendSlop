using UnityEngine;

public class HookProjectile : MonoBehaviour
{
    GameObject user;
    Transform userTransform;
    Vector3 direction;
    float range;
    float speed;
    float ballRadius;
    float spawnOffsetMultiplier;
    float stopDistance; 

    float currentDistance = 0f;
    bool retracting = false;
    bool hasCaught = false;
    Rigidbody caughtTarget;

    public void Init(GameObject user, Vector3 direction, float range, float speed, float ballRadius, float spawnOffsetMultiplier)
    {
        this.user = user;
        this.userTransform = user.transform;
        this.direction = direction;
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

            float minDistance = hasCaught ? stopDistance : 0f; 

            if (currentDistance <= minDistance)
            {
                currentDistance = minDistance;

                if (caughtTarget != null)
                {
                    caughtTarget.linearVelocity = Vector3.zero; 
                }
                Destroy(gameObject);
                return;
            }
        }

        Vector3 basePos = userTransform.position + direction * (ballRadius * spawnOffsetMultiplier);
        Vector3 targetPos = basePos + direction * currentDistance;
        transform.position = targetPos;
        transform.rotation = Quaternion.LookRotation(direction, Vector3.up);

        if (hasCaught && caughtTarget != null)
        {
            Vector3 requiredVelocity = (targetPos - caughtTarget.position) / Time.fixedDeltaTime;
            caughtTarget.linearVelocity = requiredVelocity;
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (retracting || hasCaught) return;

        Rigidbody otherRb = other.attachedRigidbody;
        if (otherRb == null || other.gameObject == user) return;

        hasCaught = true;
        caughtTarget = otherRb;
        retracting = true;


        SphereCollider targetCol = otherRb.GetComponent<SphereCollider>();
        float targetRadius = targetCol != null ? targetCol.radius * otherRb.transform.lossyScale.x : 0.5f;
        stopDistance = ballRadius + targetRadius - 0.3f; 
    }
}