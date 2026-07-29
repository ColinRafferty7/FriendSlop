using UnityEngine;
using Unity.Netcode;
using Unity.VisualScripting;
using System.Collections.Generic;

public class RoundManager : NetworkBehaviour
{
    public static RoundManager Instance { get; private set; }
    private List<BallController> Players = new List<BallController>();
    [SerializeField] SpawnData spawns;
    public NetworkVariable<RoundState> CurrentState;

    public enum RoundState
    {
        Waiting,
        Countdown,
        Playing,
        RoundOver,
    }

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
        player.Eliminate();
        Debug.Log("Player Spawn: " + player.OwnerClientId);
    }

    public void RoundStart()
    {
        CurrentState.Value = RoundState.Playing;
        foreach (BallController player in Players)
        {
            player.ResetForRound(spawns.GetRandomSpawnPoint());
        }
    }
}
