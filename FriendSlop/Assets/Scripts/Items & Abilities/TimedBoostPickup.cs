using UnityEngine;
using Unity.Netcode;
using Unity.VisualScripting.Dependencies.NCalc;

public class TimedBoostPickup : NetworkBehaviour
{
    [SerializeField] StatType statType;
    [SerializeField] float multiplier = 1.5f;
    [SerializeField] float duration = 10f;

    void OnTriggerEnter(Collider other)
    {
        if (!IsServer) return; 

        if (other.TryGetComponent<PlayerStats>(out PlayerStats stats))
        {
            stats.ActivateBoost(statType, multiplier, duration); 
            NetworkObject.Despawn(); 
        }
    }
}
