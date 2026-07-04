using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Audio;

public class MusicManager : MonoBehaviour
{
    public static MusicManager Instance;
    [SerializeField] private MusicLibrary musicLibrary;
    
    // HAPUS ATAU KOMENTAR: Kita tidak lagi memakai satu musicSource global ini
    // [SerializeField] private AudioSource musicSource; 

    [SerializeField] private List<string> lobbyPlaylist = new List<string> { "Lobby Sound 1", "Lobby Sound 2" };
    private Coroutine playlistCoroutine;
    private string currentTrackName = "";
    
    // Menyimpan referensi track yang sedang aktif berputar saat ini
    private MusicTrack currentActiveTrack; 
    [SerializeField] private AudioMixer audioMixer; // Tarik AudioMixer kamu ke slot ini via Inspector prefab/scene
    
    private void Start()
    {
        playlistCoroutine = StartCoroutine(PlayLobbyPlaylistAutomatically());
        ApplySavedVolume();
    }
    private void ApplySavedVolume()
    {
        if (audioMixer != null)
        {
            float savedVolume = PlayerPrefs.GetFloat("MusicVolume", 0f);
            audioMixer.SetFloat("MusicVolume", savedVolume);
        }
    }

    private void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
        }
        else
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            SceneManager.sceneLoaded += OnSceneLoaded;
        }
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name == "MainMenu")
        {
            StopLobbyPlaylist();
            currentTrackName = "";
            playlistCoroutine = StartCoroutine(PlayLobbyPlaylistAutomatically());
        }
        else if (scene.name == "PlayScene")
        {
            StopLobbyPlaylist();
            PlayMusic("Game");
        }
    }

    public void PlayMusic(string trackName, float fadeDuration = 0.5f)
    {
        if (trackName == currentTrackName) return;
        currentTrackName = trackName;

        // Ambil data track baru dari library
        MusicTrack nextTrack = musicLibrary.GetTrackFromName(trackName);

        if (nextTrack.clip != null && nextTrack.audioSource != null)
        {
            nextTrack.audioSource.loop = true;
            StartCoroutine(AnimateMusicCrossfade(nextTrack, fadeDuration));
        }
    }

    IEnumerator AnimateMusicCrossfade(MusicTrack nextTrack, float fadeDuration = 0.5f)
    {
        float percent = 0;
        
        // 1. FADE OUT: Mengecilkan volume AudioSource yang sedang aktif saat ini (jika ada)
        AudioSource oldSource = currentActiveTrack.audioSource;
        if (oldSource != null && oldSource.isPlaying)
        {
            float startVolume = oldSource.volume;
            while (percent < 1)
            {
                percent += Time.deltaTime * 1 / fadeDuration;
                oldSource.volume = Mathf.Lerp(startVolume, 0, percent);
                yield return null;
            }
            oldSource.Stop();
        }

        // Ganti referensi track aktif ke track yang baru
       currentActiveTrack = nextTrack;
        AudioSource newSource = currentActiveTrack.audioSource;

        newSource.clip = nextTrack.clip;
        newSource.volume = 0;
        newSource.Play();

        percent = 0;
        while (percent < 1)
        {
            percent += Time.deltaTime * 1 / fadeDuration;
       
            newSource.volume = Mathf.Lerp(0, 1f, percent); 
            yield return null;
        }
    }

    IEnumerator PlayLobbyPlaylistAutomatically()
    {
        int currentIndex = 0;

        while (true)
        {
            string trackToPlay = lobbyPlaylist[currentIndex];
            MusicTrack nextTrack = musicLibrary.GetTrackFromName(trackToPlay);

            if (nextTrack.clip != null && nextTrack.audioSource != null)
            {
                currentTrackName = trackToPlay;
                nextTrack.audioSource.loop = false; // Playlist otomatis biasanya tidak looping per lagu
                yield return StartCoroutine(AnimateMusicCrossfade(nextTrack, 0.5f));
            }

            yield return new WaitForSeconds(0.2f);

            // Tunggu sampai AudioSource dari track yang aktif selesai berputar
            while (currentActiveTrack.audioSource != null && currentActiveTrack.audioSource.isPlaying)
            {
                yield return null;
            }

            currentIndex++;
            if (currentIndex >= lobbyPlaylist.Count)
            {
                currentIndex = 0;
            }
        }
    }

    public void StopLobbyPlaylist()
    {
        if (playlistCoroutine != null)
        {
            StopCoroutine(playlistCoroutine);
        }
        if (currentActiveTrack.audioSource != null)
        {
            currentActiveTrack.audioSource.Stop();
        }
    }
}