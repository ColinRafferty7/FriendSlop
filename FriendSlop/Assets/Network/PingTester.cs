using Unity.Netcode;
using UnityEngine;

public class PingTester : NetworkBehaviour
{
    private void Update()
    {
        SendPing();
    }

    private void SendPing()
    {
        double sentTime = NetworkManager.Singleton.ServerTime.Time;
        PingRpc(sentTime);
    }

    [Rpc(SendTo.Server)]
    private void PingRpc(double sentTime)
    {
        PingResponseRpc(sentTime);
    }

    [Rpc(SendTo.Owner)]
    private void PingResponseRpc(double sentTime)
    {
        double rtt = NetworkManager.Singleton.ServerTime.Time - sentTime;

        Debug.Log($"RTT: {rtt * 1000:F1} ms");
    }
}
