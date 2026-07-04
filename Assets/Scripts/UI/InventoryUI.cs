using UnityEngine;
using TMPro;

public class InventoryUI : MonoBehaviour
{
    [Header("Panel References")]
    [SerializeField] private GameObject inventoryPanel;
    [SerializeField] private EquipmentUI equipmentUI;

    [Header("Status Display")]
    [SerializeField] private TextMeshProUGUI atkBuffText;
    [SerializeField] private TextMeshProUGUI maxHpBuffText; 

    [Header("Player Reference")]
    [SerializeField] private GameObject playerObject; 

    [Header("Buff Manager Reference")]
    [Tooltip("Akan dicari secara otomatis jika kosong")]
    [SerializeField] private PlayerBuffManager buffManager;

    [Header("Inventory Audio Settings")]
    [SerializeField] private AudioSource inventoryAudioSource;
    [SerializeField] private AudioClip openInventorySound;
    [SerializeField] private AudioClip closeInventorySound;

    private bool isAlreadyOpen = false; 
    private float soundDebounceTimer = 0f; 
    private const float SOUND_DEBOUNCE_TIME = 0.1f; 

    private void Start()
    {
        EnsureBuffManagerExists();

        if (GameManager.Instance != null)
        {
            EvaluateVisibility(GameManager.Instance.CurrentState);
        }
        else
        {
            if (inventoryPanel != null) inventoryPanel.SetActive(false);
        }
    }

    private void Update()
    {
        // KUNCI PERBAIKAN 1: Gunakan unscaledDeltaTime karena Time.deltaTime bernilai 0 saat inventory terbuka!
        if (soundDebounceTimer > 0f)
        {
            soundDebounceTimer -= Time.unscaledDeltaTime;
        }

        if (inventoryPanel != null && inventoryPanel.activeSelf)
        {
            UpdateBuffStatusText();
        }
    }

    private void OnEnable()
    {
        GameManager.OnStateChanged += HandleStateChanged;
        EnsureBuffManagerExists(); 
    }

    private void OnDisable()
    {
        GameManager.OnStateChanged -= HandleStateChanged;

        if (buffManager != null)
        {
            buffManager.OnBuffChanged -= HandleBuffChanged;
        }
    }

    private void EnsureBuffManagerExists()
    {
        if (buffManager == null)
        {
            GameObject player = GameObject.FindWithTag("Player");
            if (player != null)
            {
                buffManager = player.GetComponent<PlayerBuffManager>();
                
                if (buffManager != null)
                {
                    buffManager.OnBuffChanged -= HandleBuffChanged; 
                    buffManager.OnBuffChanged += HandleBuffChanged;
                }
            }
        }
    }

    private void HandleBuffChanged()
    {
        UpdateBuffStatusText();
    }

    private void HandleStateChanged(GameState oldState, GameState newState)
    {
        EvaluateVisibility(newState);
    }

    private void EvaluateVisibility(GameState state) 
    {
        if (inventoryPanel == null) return; 
        bool isWindowOpen = state == GameState.Inventory; 
        inventoryPanel.transform.localScale = isWindowOpen ? Vector3.one : Vector3.zero; 
        
        if (isWindowOpen && !isAlreadyOpen)
        {
            PlaySound(openInventorySound);
            isAlreadyOpen = true; 
        }
        else if (!isWindowOpen && isAlreadyOpen)
        {
            PlaySound(closeInventorySound);
            isAlreadyOpen = false; 
        }

        if (isWindowOpen) { 
            if (equipmentUI != null) equipmentUI.RefreshUI(); 
            UpdateBuffStatusText();
        }
    }

    private void PlaySound(AudioClip clip)
    {
        if (clip == null)
        {
            Debug.LogWarning("[InventoryUI] AudioClip belum di-assign di Inspector (openInventorySound / closeInventorySound kosong).");
            return;
        }

        // FIX: Debounce nyata — cegah suara open/close "makan" satu sama lain kalau
        // state berubah 2x dalam waktu sangat singkat (mis. Inventory->Paused->Inventory).
        // Sebelumnya field ini ada tapi tidak pernah dipakai, jadi Stop() bisa memotong
        // suara open yang baru saja mulai.
        if (soundDebounceTimer > 0f) return;
        soundDebounceTimer = SOUND_DEBOUNCE_TIME;

        if (inventoryAudioSource != null)
        {
            inventoryAudioSource.ignoreListenerPause = true;
            inventoryAudioSource.PlayOneShot(clip);
        }
        else
        {
            Debug.LogWarning("[InventoryUI] inventoryAudioSource belum di-assign di Inspector, fallback ke PlayClipAtPoint.");
            if (Camera.main != null)
            {
                AudioSource.PlayClipAtPoint(clip, Camera.main.transform.position, 1f);
            }
        }
    }
    
    private void UpdateBuffStatusText()
    {
        EnsureBuffManagerExists();

        if (buffManager != null)
        {
            int currentAtkBonus = buffManager.GetDamageBuffStack() * 50;
            int currentHpBonus = buffManager.GetHpBuffStack() * 100;

            if (atkBuffText != null)
            {
                atkBuffText.text = currentAtkBonus > 0 
                    ? $"BUFF ATK: <color=green>+{currentAtkBonus}</color> (x{buffManager.GetDamageBuffStack()})" 
                    : "BUFF ATK: ";
            }

            if (maxHpBuffText != null)
            {
                maxHpBuffText.text = currentHpBonus > 0 
                    ? $"BUFF MAX HP: <color=green>+{currentHpBonus}</color> (x{buffManager.GetHpBuffStack()})" 
                    : "BUFF MAX HP: ";
            }
        }
    }
}