using UnityEngine;

public class FanPushing : MonoBehaviour
{
    [SerializeField] private float force = 10f;

    void OnTriggerStay(Collider other)
    {
        if (other.TryGetComponent<PlayerPhysics>(out PlayerPhysics physics))
        {
            physics.ApplyForce(force * transform.forward);
        }
    }
}
