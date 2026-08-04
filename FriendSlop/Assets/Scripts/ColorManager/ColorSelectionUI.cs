using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;


public class ColorSelectionUI : MonoBehaviour
{
    [System.Serializable]
    public struct ColorButtonEntry
    {
        public Button button;
        public Image swatch;        
        public GameObject lockIcon; 
    }

    public List<ColorButtonEntry> colorButtons = new List<ColorButtonEntry>();

    private PlayerColorPicker localPicker;

    private void OnEnable()
    {
        for (int i = 0; i < colorButtons.Count; i++)
        {
            int index = i; 
            colorButtons[i].button.onClick.RemoveAllListeners();
            colorButtons[i].button.onClick.AddListener(() => SelectColor(index));

            if (ColorManager.Instance != null && index < ColorManager.Instance.palette.Count)
            {
                Color paletteColor = ColorManager.Instance.palette[index];
                
                colorButtons[i].swatch.color = new Color(paletteColor.r, paletteColor.g, paletteColor.b, 1f);
            }
        }

        if (ColorManager.Instance != null)
        {
            ColorManager.Instance.OnAvailabilityChanged += RefreshButtonStates;
        }

        RefreshButtonStates();
    }

    private void OnDisable()
    {
        if (ColorManager.Instance != null)
        {
            ColorManager.Instance.OnAvailabilityChanged -= RefreshButtonStates;
        }
    }

    private PlayerColorPicker GetLocalPicker()
    {
        if (localPicker != null) return localPicker;
        if (NetworkManager.Singleton == null || NetworkManager.Singleton.LocalClient.PlayerObject == null)
            return null;

        localPicker = NetworkManager.Singleton.LocalClient.PlayerObject.GetComponent<PlayerColorPicker>();
        return localPicker;
    }

    public void SelectColor(int index)
    {
        GetLocalPicker()?.RequestColor(index);
    }

    private void RefreshButtonStates()
    {
        ulong myId = NetworkManager.Singleton != null ? NetworkManager.Singleton.LocalClientId : 0;

        for (int i = 0; i < colorButtons.Count; i++)
        {
            bool takenByOther = ColorManager.Instance != null
                && ColorManager.Instance.IsColorTakenByOther(i, myId);

            colorButtons[i].button.interactable = !takenByOther;
            if (colorButtons[i].lockIcon != null)
            {
                colorButtons[i].lockIcon.SetActive(takenByOther);
            }
        }
    }
}