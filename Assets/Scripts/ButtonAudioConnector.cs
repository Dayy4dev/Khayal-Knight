using UnityEngine;
using UnityEngine.UI;

public class ButtonAudioController : MonoBehaviour
{
   
    [Header("Komponen Audio")]
    public AudioSource audioSource;
    public AudioClip clickSound;

    // Fungsi ini akan dipanggil saat tombol ditekan
    public void PlaySound()
    {
        // Mengecek apakah AudioSource dan AudioClip sudah terisi
        if (audioSource != null && clickSound != null)
        {
            // Memainkan suara satu kali
            audioSource.PlayOneShot(clickSound);
        }
        else
        {
            Debug.LogWarning("AudioSource atau AudioClip belum dimasukkan di Inspector!");
        }
    }
}

