using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

public class LobbyFunctions : NetworkBehaviour
{
    public void LoadScene(string sceneName)
    {
        if (IsServer)
        { 
            NetworkManager.Singleton.SceneManager.LoadScene(sceneName, LoadSceneMode.Single); 
        }
    }
}
