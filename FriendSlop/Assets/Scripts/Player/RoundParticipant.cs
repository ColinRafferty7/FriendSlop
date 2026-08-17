using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

public enum PlayerState
{
    Alive,
    Uncontrollable,
    Dead
}

public class RoundParticipant : NetworkBehaviour
{
    private Rigidbody rb;
    private MeshRenderer mesh;
    private SphereCollider col;
    private PlayerInput input;

    [SerializeField] private bool debug;

    public NetworkVariable<int> Score = new(0);
    public NetworkVariable<PlayerState> State = new(PlayerState.Alive);



    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        mesh = GetComponent<MeshRenderer>(); 
        col = GetComponent<SphereCollider>();
        input = GetComponent<PlayerInput>();

        SceneManager.sceneLoaded += OnSceneLoaded;

        State.OnValueChanged += OnStateChanged;
    }

    private void Update()
    {
        if (!IsServer) return;
        if (rb.position.y < -10f)
        {
            Eliminate();
        }
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (!IsServer) return;

        if (RoundManager.Instance != null)
        {
            RoundManager.Instance.AddPlayer(this);
        }
    }

    private void OnStateChanged(PlayerState prev, PlayerState current)
    {
        Debug.Log("State Change: " + current);
        switch (current)
        {
            case PlayerState.Alive:
                mesh.enabled = true;
                col.enabled = true;
                input.enabled = true;
                break;
            case PlayerState.Uncontrollable:
                mesh.enabled = true;
                col.enabled = true;
                input.enabled = false;
                break;
            case PlayerState.Dead:
                mesh.enabled = false;
                col.enabled = false;
                input.enabled = false;
                break;
        }
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        if (!IsOwner && !IsServer) Destroy(this);

        State.Value = PlayerState.Alive;
        OnStateChanged(0, State.Value);
    }

    public void Eliminate()
    {
        if (!IsServer)
            return;

        if (RoundManager.Instance.Debug) 
        {
            ResetForRound(Vector3.zero);
            return;
        }
        State.Value = PlayerState.Dead;

        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;

        RoundManager.Instance.PlayerEliminated(this);
    }

    [Rpc(SendTo.Server)]
    public void EliminateRpc()
    {
        Eliminate();
    }

    public void ResetForRound(Vector3 spawnPoint)
    {
        if (!IsServer)
            return;

        State.Value = PlayerState.Alive;

        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;

        rb.position = spawnPoint;
        rb.rotation = Quaternion.identity;
    }

    [Rpc(SendTo.Server)]
    public void ResetForRoundRpc(Vector3 spawnPoint)
    {
        ResetForRound(spawnPoint);
    }
}
