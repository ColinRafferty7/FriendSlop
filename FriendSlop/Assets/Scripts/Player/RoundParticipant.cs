using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

public class RoundParticipant : NetworkBehaviour
{
    private Rigidbody rb;
    private MeshRenderer mesh;
    private SphereCollider col;
    [SerializeField] private GameObject frontIndicator;

    [SerializeField] private bool debug;

    public NetworkVariable<bool> IsAlive = new(true);
    public NetworkVariable<int> Score = new(0);

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        mesh = GetComponent<MeshRenderer>();
        col = GetComponent<SphereCollider>();

        SceneManager.sceneLoaded += OnSceneLoaded;

        IsAlive.OnValueChanged += OnAliveChanged;
    }

    private void Update()
    {
        if (rb.position.y < -10f)
        {
            ResetForRound(Vector3.zero);
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

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        if (IsServer && !debug)
        {
            IsAlive.Value = false;
        }

        OnAliveChanged(false, IsAlive.Value);
    }

    public void Eliminate()
    {
        if (!IsServer)
            return;

        IsAlive.Value = false;

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

        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;

        rb.position = spawnPoint;
        rb.rotation = Quaternion.identity;

        IsAlive.Value = true;
    }

    [Rpc(SendTo.Server)]
    public void ResetForRoundRpc(Vector3 spawnPoint)
    {
        ResetForRound(spawnPoint);
    }

    private void OnAliveChanged(bool prev, bool current)
    {
        mesh.enabled = current;
        col.enabled = current;
        frontIndicator.gameObject.SetActive(current);
    }
}
