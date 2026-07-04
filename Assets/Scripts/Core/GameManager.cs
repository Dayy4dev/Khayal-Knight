using System;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    public static event Action<GameState, GameState> OnStateChanged;

    public GameState CurrentState { get; private set; } = GameState.Playing; // Force initial tracking assignment
    private GameState previousState = GameState.Playing;

    [Header("Input")]
    [SerializeField] private KeyCode pauseKey = KeyCode.Escape;
    [SerializeField] private KeyCode inventoryKey = KeyCode.E;

    public bool IsPlaying => CurrentState == GameState.Playing;
    public bool IsPaused => CurrentState == GameState.Paused;
    public bool IsInventoryOpen => CurrentState == GameState.Inventory;
    public bool IsMenuOpen => CurrentState == GameState.Paused || CurrentState == GameState.Inventory;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void Start()
    {
        // Enforce clean layout start settings explicitly
        CurrentState = GameState.Playing;
        ApplyStateEffects();
    }

    private void Update()
    {
        HandleInput();
    }

    private void HandleInput()
    {
        if (CurrentState == GameState.GameOver) return;

        if (Input.GetKeyDown(pauseKey))
        {
            TogglePause();
        }
        else if (Input.GetKeyDown(inventoryKey))
        {
            ToggleInventory();
        }

        if (CurrentState == GameState.Playing)
        {
            if (Input.GetKeyDown(KeyCode.LeftAlt) || Input.GetKeyDown(KeyCode.RightAlt))
            {
                Cursor.visible = true;
                Cursor.lockState = CursorLockMode.None;
            }
            else if (Input.GetKeyUp(KeyCode.LeftAlt) || Input.GetKeyUp(KeyCode.RightAlt))
            {
                Cursor.visible = false;
                Cursor.lockState = CursorLockMode.Confined;
            }
        }
    }

    public void TogglePause()
    {
        switch (CurrentState)
        {
            case GameState.Playing:
            case GameState.Inventory:
                previousState = CurrentState;
                SetState(GameState.Paused);
                break;

            case GameState.Paused:
                // Return cleanly to whatever screen we were looking at prior to pausing
                SetState(previousState);
                break;
        }
    }

    public void ToggleInventory()
    {
        switch (CurrentState)
        {
            case GameState.Playing:
                SetState(GameState.Inventory);
                break;

            case GameState.Inventory:
                SetState(GameState.Playing);
                break;

            case GameState.Paused:
                // Do nothing if the pause panel is open to prevent overlapping state conflicts
                break;
        }
    }

    public void SetState(GameState newState)
    {
        if (CurrentState == newState) return;

        GameState oldState = CurrentState;
        CurrentState = newState;

        ApplyStateEffects();
        Debug.Log($"State Changed: {oldState} -> {newState}");
        OnStateChanged?.Invoke(oldState, newState);
    }

 private void ApplyStateEffects()
    {
        Time.timeScale = (CurrentState == GameState.Playing || CurrentState == GameState.GameOver) ? 1f : 0f;

        // KUNCI PERBAIKAN: Pastikan AudioListener selalu menyala (false) agar suara UI Inventory tidak pernah mati!
        AudioListener.pause = false;

        if (CurrentState == GameState.Playing)
        {
            // Sembunyikan kursor putih saat kembali bermain
            Cursor.visible = false;
            Cursor.lockState = CursorLockMode.Confined;

            if (DungeonManager.Instance != null && DungeonManager.Instance.roomUIBar != null)
            {
                DungeonManager.Instance.roomUIBar.RefreshBarVisibilityOnResume();
            }
        }
        else
        {
            // Munculkan kursor saat membuka UI (Pause, Inventory, GameOver)
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None; 
        }
    }
    public void GameOver()
    {
        // Ubah state ke GameOver agar panel COOKED aktif
        SetState(GameState.GameOver);

        // Waktu tetap berjalan normal agar animasi tidak macet
        Time.timeScale = 1f;

        // Jalankan hitung mundur kelipatan 2 detik
        StartCoroutine(ProsesAutoRespawnKelipatan2());
    }

    private System.Collections.IEnumerator ProsesAutoRespawnKelipatan2()
    {
        // 1. FADE IN PANEL COOKED (2 Detik): Menunggu animasi masuk sampai layar hitam pekat
        yield return new WaitForSeconds(2f);

        // 2. TAHAN GELAP (3 Detik): Layar diam penuh menampilkan tulisan "COOKED"
        yield return new WaitForSeconds(2f);

        // 3. FADE OUT & RESPAWN (3 Detik):
        string sceneSekarang = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;

        if (LevelManager.Instance != null)
        {
            // Ganti "CircleWipe" dengan efek memudar ("Fade") bawaan dari LevelManager-mu
            LevelManager.Instance.LoadScene(sceneSekarang, "CrossFade");
        }
        else
        {
            UnityEngine.SceneManagement.SceneManager.LoadScene(sceneSekarang);
        }
    }

    public void ResumeGame()
    {
        if (CurrentState == GameState.Paused)
        {
            SetState(previousState);
        }
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }
    public void RespawnGame()
    {
        Time.timeScale = 1f; // Kembalikan waktu jadi normal

        string playedScene = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;

        if (LevelManager.Instance != null)
        {
            LevelManager.Instance.LoadScene(playedScene, "CircleWipe");
        }
        else
        {
            // Cadangan aman jika LevelManager tidak sengaja tidak ada di scene
            UnityEngine.SceneManagement.SceneManager.LoadScene(playedScene);
        }
    }

    public void LoadMainMenu()
    {
        Time.timeScale = 1f; // Kembalikan waktu jadi normal
        if (LevelManager.Instance != null)
        {
            LevelManager.Instance.LoadScene("MainMenu", "CircleWipe");
        }
        else
        {
            UnityEngine.SceneManagement.SceneManager.LoadScene("MainMenu");
        }
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }
}