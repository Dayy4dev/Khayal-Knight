using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class RoomUIBar : MonoBehaviour
{
    [Header("UI Components")]
    [SerializeField] private GameObject uiContainer; 
    [SerializeField] private Slider barSlider;         
    [SerializeField] private TextMeshProUGUI roomText; 

    private int localCurrentEnemies = 0;
    private int localMaxEnemies = 0;

    private void OnEnable()
    {
        GameManager.OnStateChanged += HandleGameStateChanged;
    }

    private void OnDisable()
    {
        GameManager.OnStateChanged -= HandleGameStateChanged;
    }

    private void HandleGameStateChanged(GameState oldState, GameState newState)
    {
        // Fungsi ini hanya menangani penyembunyian saat PAUSE/INVENTORY/GAMEOVER
        if (newState != GameState.Playing)
        {
            ShowBar(false);
        }
    }

    // FUNGSI BARU: Dipanggil langsung oleh GameManager saat unpause / tutup inventory
    public void RefreshBarVisibilityOnResume()
    {
        if (localMaxEnemies > 0)
        {
            ShowBar(true);
        }
    }

    public void ShowBar(bool state)
    {
        if (uiContainer != null)
            uiContainer.SetActive(state);
    }

    public void UpdateRoomText(string roomType)
    {
        if (roomText != null) 
            roomText.text = $"Room right now: {roomType}";
    }

    public void UpdateBarValue(int currentEnemies, int maxEnemies)
    {
        localCurrentEnemies = currentEnemies;
        localMaxEnemies = maxEnemies;

        if (barSlider != null && maxEnemies > 0)
        {
            float progress = (float)currentEnemies / maxEnemies;
            barSlider.value = progress;
        }
    }
}