using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.Audio;
using UnityEngine.UI;

public class MainMenu : MonoBehaviour
{
    private MenuState currentState = MenuState.Playing;
    public AudioMixer audioMixer;
    public Slider musicSlider;
    public GameObject pauseMenu;

    [Header("UI Tambahan")]
    public GameObject playSelectionMenu;
    public GameObject mainMenuPanel;

    [Header("Click Sound")]
    [SerializeField] private AudioSource uiAudioSource;
    [SerializeField] private AudioClip clickSound;

    private void Start()
    {
        if (mainMenuPanel != null) mainMenuPanel.SetActive(true);
        if (playSelectionMenu != null) playSelectionMenu.SetActive(false);

        LoadVolume();

        if (musicSlider != null)
        {
            musicSlider.onValueChanged.AddListener(UpdateMusicVolume);
        }
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (playSelectionMenu != null && playSelectionMenu.activeSelf)
            {
                ClosePlaySelection();
            }
            else if (currentState == MenuState.Playing)
            {
                PauseGame();
            }
            else if (currentState == MenuState.Paused)
            {
                ResumeGame();
            }
        }
    }

    // Klik TUTORIAL: Langsung pindah ke scene "Tutorial" dengan CircleWipe
    public void StartTutorial()
    {
        PlayClickSound();
        MusicManager.Instance.StopLobbyPlaylist();
        LevelManager.Instance.LoadScene("Tutorial", "CircleWipe");
        MusicManager.Instance.PlayMusic("Game");

        Time.timeScale = 1f;
        currentState = MenuState.Playing;
    }

    // Klik SKIP TUTORIAL: Langsung pindah ke "PlayScene" tanpa backstory
    public void StartDirectGame()
    {
        PlayClickSound();
        MusicManager.Instance.StopLobbyPlaylist();
        LevelManager.Instance.LoadScene("PlayScene", "CircleWipe");
        MusicManager.Instance.PlayMusic("Game");

        Time.timeScale = 1f;
        currentState = MenuState.Playing;
    }

    public void BackMenu()
    {
        PlayClickSound();
        LevelManager.Instance.LoadScene("MainMenu", "CircleWipe");
        Time.timeScale = 1f;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    private void PlayClickSound()
    {
        if (uiAudioSource != null && clickSound != null)
        {
            uiAudioSource.ignoreListenerPause = true;
            uiAudioSource.PlayOneShot(clickSound);
        }
    }

    public void UpdateMusicVolume(float volume)
    {
        if (audioMixer != null)
        {
            audioMixer.SetFloat("MusicVolume", volume);
            PlayerPrefs.SetFloat("MusicVolume", volume);
        }
    }

    public void LoadVolume()
    {
        if (musicSlider != null && audioMixer != null)
        {
            float savedVolume = PlayerPrefs.GetFloat("MusicVolume", 0f);
            musicSlider.value = savedVolume;
            audioMixer.SetFloat("MusicVolume", savedVolume);
        }
    }

    public void Play()
    {
        PlayClickSound();
        if (playSelectionMenu != null) playSelectionMenu.SetActive(true);
        if (mainMenuPanel != null) mainMenuPanel.SetActive(false);
    }

    public void ClosePlaySelection()
    {
        PlayClickSound();
        if (playSelectionMenu != null) playSelectionMenu.SetActive(false);
        if (mainMenuPanel != null) mainMenuPanel.SetActive(true);
    }

    public void PauseGame()
    {
        PlayClickSound();
        currentState = MenuState.Paused;
        if (pauseMenu != null) pauseMenu.SetActive(true);
        if (currentState == MenuState.Paused)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
    }

    public void ResumeGame()
    {
        PlayClickSound();
        currentState = MenuState.Playing;
        if (pauseMenu != null) pauseMenu.SetActive(false);
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    public void Quit()
    {
        PlayClickSound();
        Application.Quit();
    }

    public void SaveVolume()
    {
        audioMixer.GetFloat("MusicVolume", out float musicVolume);
        PlayerPrefs.SetFloat("MusicVolume", musicVolume);
    }

    public void ShowCursorSettings()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }
}