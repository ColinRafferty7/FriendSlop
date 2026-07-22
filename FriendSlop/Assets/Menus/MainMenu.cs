using Unity.Netcode;
using Unity.Networking.Transport;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;
using Unity.Networking.Transport.Relay;
using UnityEngine;
using Unity.Netcode.Transports.UTP;
using System.Threading.Tasks;
using System;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MainMenu : MonoBehaviour
{
    [SerializeField] private string gamename;
    [SerializeField] private InputField enteredCode;
    [SerializeField] private Text codeText;

    public async void HostGame()
    {
        string code = await StartHostWithRelay();
        Debug.Log(code);
        codeText.text = "Code: " + code;
        NetworkManager.Singleton.SceneManager.LoadScene(gamename, LoadSceneMode.Single);
    }

    public async Task<string> StartHostWithRelay()
    {
        try
        {
            await UnityServices.InitializeAsync();

            if (!AuthenticationService.Instance.IsSignedIn)
            {
                await AuthenticationService.Instance.SignInAnonymouslyAsync();
            }

            Allocation allocation = await RelayService.Instance.CreateAllocationAsync(3);

            UnityTransport transport = NetworkManager.Singleton.GetComponent<UnityTransport>();

            transport.SetRelayServerData(
                AllocationUtils.ToRelayServerData(allocation, "udp")
            );

            string joinCode = await RelayService.Instance.GetJoinCodeAsync(allocation.AllocationId);

            bool started = NetworkManager.Singleton.StartHost();

            if (!started)
                return null;

            return joinCode;
        }
        catch (Exception e)
        {
            return e.ToString();
        }
    }

    public async void EnterCode()
    {
        Debug.Log(enteredCode.text);
        bool join = await StartClientWithRelay(enteredCode.text);
    }

    public async Task<bool> StartClientWithRelay(string joinCode)
    {
        await UnityServices.InitializeAsync();

        if (!AuthenticationService.Instance.IsSignedIn)
        {
            await AuthenticationService.Instance.SignInAnonymouslyAsync();
        }

        JoinAllocation allocation =
            await RelayService.Instance.JoinAllocationAsync(joinCode);

        UnityTransport transport =
            NetworkManager.Singleton.GetComponent<UnityTransport>();

        transport.SetRelayServerData(
            AllocationUtils.ToRelayServerData(allocation, "udp")
        );

        return NetworkManager.Singleton.StartClient();
    }
}
