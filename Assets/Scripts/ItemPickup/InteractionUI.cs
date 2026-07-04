using TMPro;
using UnityEngine;

public class InteractionUI : MonoBehaviour
{
    [SerializeField] private PlayerInventory playerInventory;
    [SerializeField] private GameObject uiPanel; // Drag Game Object Teks/Panel UI ke sini
    [SerializeField] private TMP_Text promptText; // Drag komponen Text ke sini

    void Awake()
    {
        if (playerInventory != null)
            playerInventory.onNearestPickupChanged.AddListener(UpdateUI);
    }

    void OnDestroy()
    {
        if (playerInventory != null)
            playerInventory.onNearestPickupChanged.RemoveListener(UpdateUI);
    }

    private void UpdateUI(WeaponPickup nearestPickup)
    {
        if (nearestPickup != null)
        {
            // Nyalakan UI Panel (Background + Teks)
            if (uiPanel != null) uiPanel.SetActive(true);
            
            // Set teks statis tanpa nama senjata
            if (promptText != null)
            {
                promptText.text = "[F] Pick Up";
            }
        }
        else
        {
            // Jika tidak ada senjata terdekat, matikan UI Panel
            if (uiPanel != null) uiPanel.SetActive(false);
        }
    }
}