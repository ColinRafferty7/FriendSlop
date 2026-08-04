using Unity.Netcode;
using UnityEngine;


[RequireComponent(typeof(Renderer))]
public class PlayerColorPicker : NetworkBehaviour
{
    [SerializeField] private Renderer ballRenderer;


    public NetworkVariable<int> ColorIndex = new NetworkVariable<int>(
        -1,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server);

    private void Awake()
    {
        if (ballRenderer == null) ballRenderer = GetComponent<Renderer>();
    }

    public override void OnNetworkSpawn()
    {
        ColorIndex.OnValueChanged += HandleColorChanged;
        ApplyColor(ColorIndex.Value);
    }

    public override void OnNetworkDespawn()
    {
        ColorIndex.OnValueChanged -= HandleColorChanged;

        if (IsServer && ColorManager.Instance != null)
        {
            ColorManager.Instance.ReleaseColorsForClient(OwnerClientId);
        }
    }

    private void HandleColorChanged(int previous, int current)
    {
        ApplyColor(current);
    }

    private void ApplyColor(int index)
    {
        if (ballRenderer == null || ColorManager.Instance == null) return;
        if (index < 0 || index >= ColorManager.Instance.palette.Count) return;

        Color targetColor = ColorManager.Instance.palette[index];
        Material mat = ballRenderer.material;




        if (mat.HasProperty("_BaseColor"))
        {
            mat.SetColor("_BaseColor", targetColor);
        }
        else
        {
            mat.color = targetColor;
        }
    }


    public void RequestColor(int colorIndex)
    {
        if (!IsOwner) return;
        RequestColorServerRpc(colorIndex);
    }

    [ServerRpc]
    private void RequestColorServerRpc(int colorIndex, ServerRpcParams rpcParams = default)
    {
        ulong requester = rpcParams.Receive.SenderClientId;
        if (ColorManager.Instance == null) return;

        bool claimed = ColorManager.Instance.TryClaimColor(colorIndex, requester);
        if (claimed)
        {
            ColorIndex.Value = colorIndex;
        }

    }
}