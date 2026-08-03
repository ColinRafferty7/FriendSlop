using Unity.Netcode;
using UnityEngine;

public class DisappearingPlatform : NetworkBehaviour
{
    [SerializeField] bool respawn = true;
    [SerializeField] bool falls = true;
    [SerializeField] float timeBeforeDisappearing = 2f;
    [SerializeField] float timeBeforeRespawning = 10f;
    [SerializeField] float fallSpeed = 5f;

    float timer = 0;
    bool timerActive = false;
    float respawnTimer = 0;
    bool respawnTimerActive = false;

    Vector3 startPosition;
    Collider platformCollider;
    Renderer platformRenderer;

    NetworkVariable<bool> isGone = new NetworkVariable<bool>(
        false,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server);

    NetworkVariable<bool> netFalling = new NetworkVariable<bool>(
        false,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server);

    private void Awake()
    {
        platformCollider = GetComponent<Collider>();
        platformRenderer = GetComponent<Renderer>();
    }

    private void Start()
    {
        startPosition = transform.position;
    }

    public override void OnNetworkSpawn()
    {
        isGone.OnValueChanged += HandleIsGoneChanged;
        ApplyGoneState(isGone.Value, snapPosition: true);
    }

    public override void OnNetworkDespawn()
    {
        isGone.OnValueChanged -= HandleIsGoneChanged;
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (!IsServer) return;

        if (timerActive) return;
        timerActive = true;

        if (falls)
        {
            netFalling.Value = true;
        }
    }

    private void Update()
    {
        if (netFalling.Value)
        {
            transform.Translate(fallSpeed * Vector3.down * Time.deltaTime);
        }

        if (!IsServer) return;

        if (timerActive)
        {
            timer += Time.deltaTime;
        }

        if (respawnTimerActive)
        {
            respawnTimer += Time.deltaTime;
        }

        if (timer > timeBeforeDisappearing && !isGone.Value)
        {
            timerActive = false;
            timer = 0;
            netFalling.Value = false;
            isGone.Value = true;
        }

        if (respawn && isGone.Value)
        {
            respawnTimerActive = true;
            if (respawnTimer > timeBeforeRespawning)
            {
                isGone.Value = false;
                respawnTimerActive = false;
                respawnTimer = 0;
            }
        }
    }

    private void HandleIsGoneChanged(bool previous, bool current)
    {
        ApplyGoneState(current, snapPosition: true);
    }

    private void ApplyGoneState(bool gone, bool snapPosition)
    {
        platformCollider.enabled = !gone;
        platformRenderer.enabled = !gone;

        if (snapPosition && !gone)
        {
            transform.position = startPosition;
        }
    }
}

