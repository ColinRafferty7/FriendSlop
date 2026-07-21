using UnityEngine;

public class AbilityPickup : MonoBehaviour
{
    [SerializeField] AbilityBase abilityPrefab;

    void OnTriggerEnter(Collider other)
    {
        BallController ball = other.GetComponent<BallController>();
        if (ball != null)
        {
            ball.CollectAbility(abilityPrefab);
            Destroy(gameObject);
        }
    }
}
