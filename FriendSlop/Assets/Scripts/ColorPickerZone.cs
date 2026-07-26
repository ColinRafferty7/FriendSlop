using Unity.Netcode;
using UnityEngine;


[RequireComponent(typeof(Collider))]
public class ColorPickerZone : MonoBehaviour
{
    [Tooltip("The UI GameObject (e.g. a Canvas panel holding ColorSelectionUI) to show while standing in this zone.")]
    public GameObject colorSelectionUI;

    private void Awake()
    {

        GetComponent<Collider>().isTrigger = true;

        if (colorSelectionUI != null)
        {
            colorSelectionUI.SetActive(false);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!IsLocalPlayer(other)) return;
        if (colorSelectionUI != null) colorSelectionUI.SetActive(true);
    }

    private void OnTriggerExit(Collider other)
    {
        if (!IsLocalPlayer(other)) return;
        if (colorSelectionUI != null) colorSelectionUI.SetActive(false);
    }

    private bool IsLocalPlayer(Collider other)
    {

        NetworkObject netObj = other.GetComponentInParent<NetworkObject>();
        return netObj != null && netObj.IsOwner;
    }
}