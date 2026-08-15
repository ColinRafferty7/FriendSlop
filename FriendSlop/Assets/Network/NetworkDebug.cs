using Unity.VisualScripting.Antlr3.Runtime.Misc;
using UnityEngine;
using Unity.Netcode;

public class NetworkDebug : MonoBehaviour
{
    public static void LogAll(string message)
    {
        var nd = new NetworkDebug();
        nd.LogRpc(message);
    }

    [Rpc(SendTo.ClientsAndHost)]
    private void LogRpc(string message)
    {
        Debug.Log(message);
    }
}
