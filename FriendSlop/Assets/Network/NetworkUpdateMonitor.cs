using System.Data.Common;
using Unity.Netcode;
using UnityEngine;

public class NetworkUpdateMonitor : NetworkBehaviour
{
    private Vector3 lastPosition;
    private Quaternion lastRotation;

    private float timeSinceLastChange;
    private float longestGap;

    private float updates;
    private float longupdates;

    private double lastTime;
    private Vector3 lastPos;

    private NetworkVariable<int> tick = new(0);

    void Start()
    {
        if (!IsOwner) Destroy(this);
        lastPosition = transform.position;
        lastRotation = transform.rotation;
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();


        NetworkManager.Singleton.NetworkTickSystem.Tick += Tick;
    }
    
    public void Tick()
    {
        if (OwnerClientId == 0) return;
        //Debug.Log($"Tick Time: {(NetworkManager.Singleton.ServerTime.Time - lastTime) * 1000f:F4}");
        lastTime = NetworkManager.Singleton.ServerTime.Time;
        if (IsServer) tick.Value++;
        Debug.Log($"Tick {tick.Value}: {(lastPos - transform.position).magnitude:F3}");
        lastPos = transform.position;
    }

    void Update()
    {
        timeSinceLastChange += Time.unscaledDeltaTime;

        bool positionChanged = Vector3.Distance(transform.position, lastPosition) > 0.001f;

        bool rotationChanged = Quaternion.Angle(transform.rotation, lastRotation) > 0.1f;

        if (positionChanged || rotationChanged)
        { 
            updates++;

            if (timeSinceLastChange > 0.025f)
                longupdates++;

            updates++;

            timeSinceLastChange = 0f;

            lastPosition = transform.position;
            lastRotation = transform.rotation;
        }


        if (Input.GetKeyDown(KeyCode.U))
        {
            Debug.Log("Long Update %: " + longupdates / updates);
        }
    }
}