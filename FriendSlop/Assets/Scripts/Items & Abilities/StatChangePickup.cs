using UnityEngine;
using Unity.Netcode;
public class TimedBoostPickup : NetworkBehaviour
{
    [SerializeField] StatType statType;
    [SerializeField] float multiplier = 1.5f;
    [SerializeField] float duration = 10f;

    void OnTriggerEnter(Collider other)
    {
        if (!IsServer) return;

        PlayerStats stats = other.GetComponentInParent<PlayerStats>();
        if (stats != null)
        {
            stats.ApplyTimedBoost(statType, multiplier, duration);
            NetworkObject.Despawn();
        }
    }
}