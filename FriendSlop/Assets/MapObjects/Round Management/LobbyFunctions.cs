using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

public class LobbyFunctions : NetworkBehaviour
{
    public void LoadScene(string sceneName)
    {
        Debug.Log("Switching scene to: " + sceneName);
        NetworkManager.Singleton.SceneManager.LoadScene(sceneName, LoadSceneMode.Single); 
    }
}
