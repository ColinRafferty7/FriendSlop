using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;


public class ColorManager : NetworkBehaviour
{
    public static ColorManager Instance { get; private set; }

    [Tooltip("Colors players can choose from. Index in this list = colorIndex used everywhere else.")]
    public List<Color> palette = new List<Color>();


    private const ulong Unclaimed = ulong.MaxValue;

    private NetworkList<ulong> colorOwners;

    public event System.Action OnAvailabilityChanged;

    private void Awake()
    {
        Instance = this;
        colorOwners = new NetworkList<ulong>();
    }

    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            colorOwners.Clear();
            for (int i = 0; i < palette.Count; i++)
            {
                colorOwners.Add(Unclaimed);
            }
        }

        colorOwners.OnListChanged += OnColorOwnersChanged;

        OnColorOwnersChanged(default);
    }

    public override void OnNetworkDespawn()
    {
        colorOwners.OnListChanged -= OnColorOwnersChanged;
    }

    private void OnColorOwnersChanged(NetworkListEvent<ulong> changeEvent)
    {
        OnAvailabilityChanged?.Invoke();
    }

    public bool IsColorTakenByOther(int index, ulong clientId)
    {
        if (index < 0 || index >= colorOwners.Count) return true;
        ulong owner = colorOwners[index];
        return owner != Unclaimed && owner != clientId;
    }


    public bool TryClaimColor(int colorIndex, ulong clientId)
    {
        if (!IsServer) return false;
        if (colorIndex < 0 || colorIndex >= colorOwners.Count) return false;
        if (IsColorTakenByOther(colorIndex, clientId)) return false;

        for (int i = 0; i < colorOwners.Count; i++)
        {
            if (colorOwners[i] == clientId) colorOwners[i] = Unclaimed;
        }

        colorOwners[colorIndex] = clientId;
        return true;
    }

    public void ReleaseColorsForClient(ulong clientId)
    {
        if (!IsServer) return;
        for (int i = 0; i < colorOwners.Count; i++)
        {
            if (colorOwners[i] == clientId) colorOwners[i] = Unclaimed;
        }
    }
}
