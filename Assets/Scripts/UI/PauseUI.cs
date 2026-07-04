using UnityEngine;

public class PauseUI : MonoBehaviour
{
    [SerializeField] private GameObject pausePanel;

    private void Awake()
    {
        if (pausePanel == null)
        {
            pausePanel = this.gameObject;
        }
    }

    private void Start()
    {
        // Force the panel to look at the manager's state on frame one
        if (GameManager.Instance != null)
        {
            pausePanel.SetActive(GameManager.Instance.CurrentState == GameState.Paused);
        }
        else
        {
            pausePanel.SetActive(false); // Fallback safe closure
        }
    }

    private void OnEnable()
    {
        GameManager.OnStateChanged += HandleStateChanged;
    }

    private void OnDisable()
    {
        GameManager.OnStateChanged -= HandleStateChanged;
    }

    private void HandleStateChanged(GameState oldState, GameState newState)
    {
        if (pausePanel != null)
        {
            pausePanel.SetActive(newState == GameState.Paused);
        }
    }

    public void ResumeButton()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.ResumeGame();
        }
    }
}