using UnityEngine;

/// <summary>
/// Komponen yang ditempel di prefab senjata yang ada di dunia.
/// Saat player mendekat → tampilkan prompt "[F] Ambil <nama>"
/// Saat F ditekan → PlayerInventory.TryPickup(this) dipanggil
/// Saat di-drop → ditandai sebagai dropped, dihapus saat scene unload
/// </summary>
public class WeaponPickup : MonoBehaviour
{
    // -------------------------------------------------------------------------
    // Inspector
    // -------------------------------------------------------------------------

    [Header("Data")]
    [SerializeField] private WeaponData weaponData;

    [Header("Animation")]
    [SerializeField] private bool enableFloat = true;
    [SerializeField] private float floatAmplitude = 0.2f;
    [SerializeField] private float floatSpeed = 2f;
    [SerializeField] private Vector3 rotationSpeed = new Vector3(0f, 90f, 0f);

    [Header("UI Prompt Object (Opsional)")]
    [Tooltip("Jika kamu pakai World-Space UI menempel di senjata, masukkan Canvas-nya di sini")]
    [SerializeField] private GameObject localPromptUI;

    // -------------------------------------------------------------------------
    // State
    // -------------------------------------------------------------------------

    private float startY;
    private bool isDropped; // true = spawn dari drop player, dihapus saat scene unload

    // -------------------------------------------------------------------------
    // Properties
    // -------------------------------------------------------------------------

    [HideInInspector] public string customRarity = "";
    public WeaponData Data => weaponData;
    public bool IsDropped => isDropped;

    // -------------------------------------------------------------------------
    // Unity Lifecycle
    // -------------------------------------------------------------------------

    void Awake()
    {
        startY = transform.position.y;
        TogglePrompt(false);
    }

    void Update()
    {
        if (!enableFloat) return;

        transform.Rotate(rotationSpeed * Time.deltaTime);
        float newY = startY + Mathf.Sin(Time.time * floatSpeed) * floatAmplitude;
        transform.position = new Vector3(transform.position.x, newY, transform.position.z);
    }

    void OnEnable()
    {
        startY = transform.position.y;
    }

    // -------------------------------------------------------------------------
    // Public API
    // -------------------------------------------------------------------------

    /// <summary>Untuk menyalakan/mematikan UI lokal di senjata ini</summary>
    public void TogglePrompt(bool show)
    {
        if (localPromptUI != null)
        {
            localPromptUI.SetActive(show);
        }
    }

    /// <summary>Dipanggil PlayerInventory setelah senjata berhasil diambil.</summary>
    public void OnPickedUp()
    {
        TogglePrompt(false);

        // --- SUNTIKAN INFO RARITY SEBELUM MASUK INVENTORY ---
        if (weaponData != null && !string.IsNullOrEmpty(customRarity))
        {
            weaponData.overrideRarity = customRarity;
        }

        // ---- TAMBAHKAN KODE INI ----
        if (TutorialManager.Instance != null && weaponData != null)
        {

            // Mengecek ID atau nama dari WeaponData Anda
            if (weaponData.name.Contains("Sword"))
            {
                TutorialManager.Instance.sudahAmbilPedang = true;
            }
            else if (weaponData.name.Contains("Cannon"))
            {
                TutorialManager.Instance.sudahAmbilMeriam = true;
            }
        }
        // ----------------------------
        Destroy(gameObject);
    }

    /// <summary>Tandai sebagai senjata yang dijatuhkan player (dihapus saat pindah scene).</summary>
    public void MarkAsDropped()
    {
        isDropped = true;
    }
}