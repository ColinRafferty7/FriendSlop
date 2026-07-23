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

        BallController ball = other.GetComponent<BallController>();
        if (ball != null)
        {
            ball.ApplyTimedBoost(statType, multiplier, duration); 
            NetworkObject.Despawn(); 
        }
    }
}
