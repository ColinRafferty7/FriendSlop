using UnityEngine;
using Unity.Netcode;
using Unity.VisualScripting;
using System.Collections.Generic;

public class RoundManager : NetworkBehaviour
{
    public static RoundManager Instance { get; private set; }
    private List<BallController> Players = new List<BallController>();
    [SerializeField] SpawnData spawns;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    public void AddPlayer(BallController player)
    {
        if (Players.Contains(player)) return;
        Players.Add(player);
        player.Disable();
        Debug.Log("Player Spawn: " + player.OwnerClientId);
    }

    [Rpc(SendTo.ClientsAndHost)]
    public void RoundStartRpc()
    {
        foreach (BallController player in Players)
        {
            player.enabled = true;
            player.SpawnRpc(spawns.GetRandomSpawnPoint());
        }
    }
}
