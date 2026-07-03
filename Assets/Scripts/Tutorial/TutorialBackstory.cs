using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using TMPro;

public class TutorialBackstory : MonoBehaviour
{
    [Header("UI Komponen (Gambar & Narasi)")]
    [SerializeField] private GameObject backstoryCanvas;      // Canvas utama backstory
    [SerializeField] private Image backstoryBgImage;           // Gambar Latar/Ilustrasi yang akan berubah-ubah
    [SerializeField] private TextMeshProUGUI backstoryText;    // Teks Narasi Cerita

    [Header("UI Gameplay Tutorial")]
    [SerializeField] private GameObject gameplayTutorialUI;    // UI asli game tutorial (HUD, dll)

    [Header("Pengaturan Cerita")]
    [SerializeField] private float textSpeed = 0.05f;         // Kecepatan mesin tik teks
    [SerializeField] private float gameplayDelay = 1.5f;       // Jeda waktu diam setelah layar terbuka lagi
    [SerializeField] private List<StoryBlockTutorial> backstorySequence; // List data Gambar + Teks

    

    private int currentLineIndex = 0;
    private bool isTyping = false;

    private void Start()
    {
        // Cek apakah backstory sudah diselesaikan
        if (PlayerPrefs.GetInt("BackstoryFinished", 0) == 1)
        {
            if (backstoryCanvas != null) backstoryCanvas.SetActive(false);
            
            PlayerPrefs.SetInt("BackstoryFinished", 0);
            PlayerPrefs.Save();

            StartCoroutine(StartGameplayWithDelay());
        }
        else
        {
            // Tampilkan cerita jika ini pertama kalinya
            if (gameplayTutorialUI != null) gameplayTutorialUI.SetActive(false);

            if (backstoryCanvas != null && backstorySequence != null && backstorySequence.Count > 0)
            {
                backstoryCanvas.SetActive(true);
                currentLineIndex = 0;
                StartCoroutine(TypeStoryLine());
            }
            else
            {
                EndBackstoryAndStartTutorial();
            }
        }
    }

    private void Update()
    {
        if (backstoryCanvas != null && backstoryCanvas.activeSelf)
        {
            // Klik untuk memajukan narasi
            if (Input.GetMouseButtonDown(0) || Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Return))
            {
                OnScreenClicked();
            }
        }
    }

    private IEnumerator TypeStoryLine()
    {
        isTyping = true;
        backstoryText.text = "";

        // Ambil data gambar dan teks pada urutan saat ini
        StoryBlockTutorial currentBlock = backstorySequence[currentLineIndex];

        // Ganti Gambar Latar Belakang / Ilustrasi
        if (backstoryBgImage != null)
        {
            if (currentBlock.storyIllustration != null)
            {
                backstoryBgImage.gameObject.SetActive(true);
                backstoryBgImage.sprite = currentBlock.storyIllustration; // Gambar berganti di sini
            }
            else
            {
                backstoryBgImage.gameObject.SetActive(false); // Kalau tidak ada gambar, jadikan layar hitam
            }
        }

        // Tampilkan teks dengan efek mengetik
        foreach (char letter in currentBlock.dialogueText.ToCharArray())
        {
            backstoryText.text += letter;
            yield return new WaitForSeconds(textSpeed);
        }

        isTyping = false;
    }

    public void OnScreenClicked()
    {
        if (isTyping)
        {
            // Munculkan semua teks langsung jika diklik saat mengetik
            StopAllCoroutines();
            backstoryText.text = backstorySequence[currentLineIndex].dialogueText;
            isTyping = false;
        }
        else
        {
            // Maju ke narasi & gambar berikutnya
            currentLineIndex++;

            if (currentLineIndex < backstorySequence.Count)
            {
                StartCoroutine(TypeStoryLine());
            }
            else
            {
                EndBackstoryAndStartTutorial();
            }
        }
    }

    private void EndBackstoryAndStartTutorial()
    {
        StopAllCoroutines();
        
        PlayerPrefs.SetInt("BackstoryFinished", 1);
        PlayerPrefs.Save();

        // Menggunakan LevelManager bawaan untuk reload dengan CircleWipe
        if (LevelManager.Instance != null)
        {
            LevelManager.Instance.LoadScene("Tutorial", "CircleWipe");
        }
        else
        {
            if (backstoryCanvas != null) backstoryCanvas.SetActive(false);
            StartCoroutine(StartGameplayWithDelay());
        }
    }

    private IEnumerator StartGameplayWithDelay()
    {
        if (gameplayTutorialUI != null) gameplayTutorialUI.SetActive(false);

        // Jeda waktu setelah transisi CircleWipe selesai
        yield return new WaitForSeconds(gameplayDelay);

        if (gameplayTutorialUI != null) gameplayTutorialUI.SetActive(true);
    }
}

// Struct super simpel di luar class
[System.Serializable]
public struct StoryBlockTutorial
{
    public Sprite storyIllustration;     // Kolom khusus gambar latar/suasana
    [TextArea(2, 5)]
    public string dialogueText;          // Kolom khusus teks narasi
}