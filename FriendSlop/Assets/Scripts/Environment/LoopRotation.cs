using Unity.Netcode;
using UnityEngine;

public class LoopRotation : NetworkBehaviour
{
    [SerializeField] Transform pivotPoint;
    [SerializeField] float speed = 30f;
    [SerializeField] float angleOfRotation = 60f;

    float currentAngle = 0f;
    bool returning = false;

    Quaternion startingRotation;

    void Start()
    {
        startingRotation = transform.rotation;
    }

    void Update()
    {
        if (!IsServer)
            return;
        float direction = returning ? -1f : 1f;

        float delta = speed * Time.deltaTime * direction;

        currentAngle += delta;

        transform.RotateAround(
            pivotPoint.position,
            Vector3.up,
            delta
        );

        transform.rotation = Quaternion.Euler(
            startingRotation.eulerAngles.x,
            transform.eulerAngles.y,
            startingRotation.eulerAngles.z
        );


        if (!returning && currentAngle >= angleOfRotation)
        {
            returning = true;
        }
        else if (returning && currentAngle <= 0)
        {
            returning = false;
        }
    }
}
