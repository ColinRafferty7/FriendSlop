using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using Unity.Netcode;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public class RoundManager : NetworkBehaviour
{
    public static RoundManager Instance { get; private set; }
    private List<RoundParticipant> Players = new List<RoundParticipant>();
    [SerializeField] private SpawnData spawns;
    public NetworkVariable<RoundState> CurrentState;
    public NetworkVariable<int> Countdown = new();
    public NetworkVariable<bool> UIactive = new();

    public bool IsLobby = false;

    [SerializeField] private Text countdownText;

    public float AlivePlayers;

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

        if (spawns == null)
        {
            spawns = new SpawnData();
        }
    }

    void Start()
    {
        if (!IsServer) return;
        if (IsLobby)
        {
            SpawnAllPlayers();
            ActivateAllPlayers();
            CurrentState.Value = RoundState.Playing;
            return;
        }
        StartCoroutine(RoundStart());
    }

    public override void OnNetworkSpawn()
    {
        Countdown.OnValueChanged += OnCountdownChanged;
        UIactive.OnValueChanged += OnActiveChanged;
    }

    [Rpc(SendTo.ClientsAndHost)]
    public void LogRpc(string message)
    {
        Debug.Log(message);
    }

    private void OnActiveChanged(bool previous, bool current)
    {
        countdownText.gameObject.SetActive(current);
    }

    private void OnCountdownChanged(int previous, int current)
    {
        UIactive.Value = true;

        if (current > 0)
            countdownText.text = current.ToString();
        else
            countdownText.text = "GO!";
    }

    private IEnumerator StartTimer(int seconds)
    {
        CurrentState.Value = RoundState.Countdown;

        while (seconds > 0)
        {
            Countdown.Value = seconds;
            yield return new WaitForSeconds(1f);
            seconds--;
        }

        Countdown.Value = 0;
        yield return new WaitForSeconds(1f);

        UIactive.Value = false;
    }

    public void AddPlayer(RoundParticipant player)
    {
        if (Players.Contains(player)) return;
        Players.Add(player);
        if (!IsLobby) player.Eliminate();
        Debug.Log("Player Spawn: " + player.OwnerClientId);
    }

    private void DespawnAllPlayers()
    {
        foreach (RoundParticipant player in Players)
        {
            player.Eliminate();
        }
    }

    public IEnumerator RoundStart()
    {
        AlivePlayers = 0;

        DespawnAllPlayers();
        AlivePlayers += SpawnAllPlayers();
        yield return StartCoroutine(StartTimer(3));
        ActivateAllPlayers();
        
        CurrentState.Value = RoundState.Playing;
    }

    private int SpawnAllPlayers()
    {
        int count = 0;
        List<Vector3> spawnPoints = new List<Vector3>();
        foreach (RoundParticipant player in Players)
        {
            Vector3 spawn;
            do { spawn = spawns.GetRandomSpawnPoint(); }
            while (spawnPoints.Contains(spawn));
            
            spawnPoints.Add(spawn);
            player.ResetForRound(spawn);
            player.State.Value = PlayerState.Uncontrollable;
            count++;
        }
        return count;
    }

    private void ActivateAllPlayers()
    {
        foreach (RoundParticipant player in Players)
        {
            player.State.Value = PlayerState.Alive;
        }
    }

    public void PlayerEliminated(RoundParticipant player)
    {
        if (IsLobby)
        {
            player.ResetForRound(spawns.GetRandomSpawnPoint());
            return;
        }
        if (CurrentState.Value != RoundState.Playing) return;
        AlivePlayers--;
        if (AlivePlayers <= 1)
        {
            StartCoroutine(RoundOver());
        }
    }

    private IEnumerator RoundOver()
    {
        string message;

        CurrentState.Value = RoundState.RoundOver;

        if (AlivePlayers < 1)
        {
            message = "No Winner";
        }
        else
        {
            RoundParticipant winner = Players.FirstOrDefault(player => player.State.Value == PlayerState.Alive);

            message = $"Player #{winner.OwnerClientId} Wins!";
        }
        UIactive.Value = true;
        AnnounceWinnerRpc(message);
        yield return new WaitForSeconds(3f);
        UIactive.Value = false;
        StartCoroutine(RoundStart());
    }

    [Rpc(SendTo.ClientsAndHost)]
    private void AnnounceWinnerRpc(string message)
    {
        countdownText.text = message;
    }
}
