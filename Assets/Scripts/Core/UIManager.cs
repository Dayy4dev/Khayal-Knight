using UnityEngine;

public class UIManager : MonoBehaviour
{
    [Header("UI Panels")]
    [SerializeField] private GameObject gameplayUI;   // Drag your 'Gameplay Health' here
    // inventoryUI Dihapus dari sini karena InventoryUI.cs sudah mandiri
    [SerializeField] private GameObject pauseUI;      // Drag your 'Pause Panel' here
    [SerializeField] private GameObject gameOverUI;

    private void OnEnable()
    {
        GameManager.OnStateChanged += HandleStateChanged;
    }

    private void OnDisable()
    {
        GameManager.OnStateChanged -= HandleStateChanged;
    }

    private void Start()
    {
        if (GameManager.Instance != null)
        {
            UpdateUIElements(GameManager.Instance.CurrentState);
        }
    }

    private void HandleStateChanged(GameState oldState, GameState newState)
    {
        UpdateUIElements(newState);
    }

    private void UpdateUIElements(GameState state)
    {
        switch (state)
        {
            case GameState.Playing:
                if (gameplayUI != null) gameplayUI.SetActive(true);
                if (pauseUI != null) pauseUI.SetActive(false);
                if (gameOverUI != null) gameOverUI.SetActive(false);
                break;

            case GameState.Inventory:
                if (gameplayUI != null) gameplayUI.SetActive(false);
                if (pauseUI != null) pauseUI.SetActive(false);
                if (gameOverUI != null) gameOverUI.SetActive(false);
                break;

            case GameState.Paused:
                if (gameplayUI != null) gameplayUI.SetActive(false);
                if (pauseUI != null) pauseUI.SetActive(true);
                if (gameOverUI != null) gameOverUI.SetActive(false);
                break;

            case GameState.GameOver:
                if (gameplayUI != null) gameplayUI.SetActive(false);
                if (pauseUI != null) pauseUI.SetActive(false);
                if (gameOverUI != null) gameOverUI.SetActive(true);
                break;
        }
    }
}