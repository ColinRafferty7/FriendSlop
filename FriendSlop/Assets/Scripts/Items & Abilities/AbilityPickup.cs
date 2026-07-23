using UnityEngine;
using Unity.Netcode;

public class AbilityPickup : NetworkBehaviour
{
    [SerializeField] AbilityBase abilityPrefab;

    void OnTriggerEnter(Collider other)
    {
        if (!IsServer) return;

        BallController ball = other.GetComponent<BallController>();
        if (ball != null)
        {
            ball.CollectAbility(abilityPrefab);
            NetworkObject.Despawn();
        }
    }
}
