using UnityEngine;
using Unity.Netcode;

public class TestNetworking : MonoBehaviour
{
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.H))
        {
            Debug.Log("Host Start: " + NetworkManager.Singleton.StartHost());
            
        }

        if (Input.GetKeyDown(KeyCode.C))
        {
            Debug.Log("Client Start: " + NetworkManager.Singleton.StartClient());
        }
    }
    
}
