using Unity.VisualScripting;
using UnityEngine;
using Unity.Netcode;

public class AuthorityManager : NetworkBehaviour
{
    public override void OnNetworkSpawn()
    {
        if (!IsServer)
        {
            Destroy(gameObject);
        }
    }
}
