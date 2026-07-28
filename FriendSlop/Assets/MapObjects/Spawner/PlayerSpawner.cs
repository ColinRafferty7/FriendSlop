using UnityEngine;
using Unity.Netcode;
using System.Xml.Serialization;

public class PlayerSpawner : NetworkBehaviour
{
    [SerializeField] private SpawnData SpawnPoints;
    [SerializeField] private BallController controller;

    public override void OnNetworkSpawn()
    {
        if (!IsServer)
        {   
            enabled = false;
            return;
        }
        SpawnPlayer();
        EnablePlayerControllerRpc();
        base.OnNetworkSpawn();
    }    

    private void SpawnPlayer()
    {
        if (SpawnPoints == null) return;
        Vector3 spawn = SpawnPoints.GetRandomSpawnPoint();
        transform.position = spawn;
    }

    // Only the server is able to run this code, so it needs to send Rpc to all 
    // clients to tell them to enable their ball controllers
    [Rpc(SendTo.ClientsAndHost)]
    private void EnablePlayerControllerRpc()
    {
        controller.enabled = true;
    }
}
