using UnityEngine;

[AddComponentMenu("Player/Player Buff Manager")]
public class PlayerBuffManager : MonoBehaviour
{
    /// <summary>
    /// Dipanggil setiap kali stack buff (HP atau Damage) berubah,
    /// supaya UI lain (mis. InventoryUI) bisa langsung refresh tanpa menunggu panel dibuka.
    /// </summary>
    public event System.Action OnBuffChanged;

    [Header("References")]
    [SerializeField] private PlayerHealth playerHealth;
    [SerializeField] private PlayerAttack playerAttack;

    // Menghitung berapa kali buff telah di-stack
    private int hpBuffStackCount = 0;
    private int damageBuffStackCount = 0;

    void Awake()
    {
        if (playerHealth == null) playerHealth = GetComponent<PlayerHealth>();
        if (playerAttack == null) playerAttack = GetComponent<PlayerAttack>();
    }

    /// <summary>
    /// Menambahkan Buff Max HP +100 dan Heal 30% dari Max HP Saat Ini (Bisa di-stack)
    /// </summary>
    public void ApplyHpAndHealBuff()
    {
        if (playerHealth == null || playerHealth.IsDead) return;

        hpBuffStackCount++;
        
        // 1. Tambah Max HP sebesar 100
        playerHealth.IncreaseMaxHealth(100);

        // 2. Berikan Heal sebesar 30% dari total Max HP yang baru setelah ditambahkan
        int maxHp = playerHealth.GetMaxHealth();
        int healAmount = Mathf.RoundToInt(maxHp * 0.30f);
        playerHealth.Heal(healAmount);

        Debug.Log($"[BUFF] Max HP Buff Stack #{hpBuffStackCount} diaplikasikan! +100 Max HP & Heal 30% (+{healAmount} HP)");
        Debug.Log($"[InventoryUI-DEBUG] ApplyHpAndHealBuff jalan di instance {GetInstanceID()} pada GameObject '{gameObject.name}'");

        OnBuffChanged?.Invoke();
    }

    /// <summary>
    /// Menambahkan Buff Damage +50 (Bisa di-stack)
    /// </summary>
    public void ApplyDamageBuff()
    {
        if (playerAttack == null) return;

        damageBuffStackCount++;

        // Memasukkan tambahan damage buff ke dalam variabel permanen di PlayerAttack
        playerAttack.ApplyPermanentDamageBuff(50);

        Debug.Log($"[BUFF] Damage Buff Stack #{damageBuffStackCount} diaplikasikan! +50 Permanent Damage Buff.");
        Debug.Log($"[InventoryUI-DEBUG] ApplyDamageBuff jalan di instance {GetInstanceID()} pada GameObject '{gameObject.name}'");

        OnBuffChanged?.Invoke();
    }

    // =========================================================================
    // --- TAMBAHAN FUNGSI UNTUK REFRESH DI INVENTORY UI ---
    // =========================================================================

    /// <summary>
    /// Mengembalikan jumlah total stack buff damage saat ini untuk dibaca UI
    /// </summary>
    public int GetDamageBuffStack()
    {
        return damageBuffStackCount;
    }

    /// <summary>
    /// Mengembalikan jumlah total stack buff HP saat ini untuk dibaca UI
    /// </summary>
    public int GetHpBuffStack()
    {
        return hpBuffStackCount;
    }
}