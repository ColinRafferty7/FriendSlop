using UnityEngine;
using Unity.Netcode;

public class LocalHostSpawning : MonoBehaviour
{   
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.H))
        {
            NetworkManager.Singleton.StartHost();
        }
        if (Input.GetKeyDown(KeyCode.C))
        {
            NetworkManager.Singleton.StartClient();
        }
    }
}
