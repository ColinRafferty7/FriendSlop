using UnityEngine;
using Unity.Netcode;

public class AbilityPickup : NetworkBehaviour
{
    [SerializeField] AbilityBase abilityPrefab;

    void OnTriggerEnter(Collider other)
    {
        if (!IsServer) return;
        Debug.Log("Got past check");

        PlayerItems ball = other.GetComponent<PlayerItems>();
        if (ball != null)
        {
            Debug.Log("Got past check 2");
            ball.CollectAbility(abilityPrefab);
            NetworkObject.Despawn();
        }
    }
}
