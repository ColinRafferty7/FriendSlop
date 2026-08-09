using UnityEngine;
using Unity.Netcode;

public class AbilityPickup : NetworkBehaviour
{
    [SerializeField] AbilityBase abilityPrefab;

    void OnTriggerEnter(Collider other)
    {
        if (!IsServer) return;

        PlayerItems ball = other.GetComponent<PlayerItems>();
        if (ball != null)
        {
            ball.CollectAbility(abilityPrefab);
            NetworkObject.Despawn();
        }
    }
}
