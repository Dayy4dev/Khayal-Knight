using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class WeaponSlotUI : MonoBehaviour
{
    [Header("UI Component Child Bindings")]
    [SerializeField] private Image weaponImage;
    [SerializeField] private TMP_Text weaponName;

    // Referensi ke Teks Stats
    [SerializeField] private TMP_Text weaponStats;

    private void Awake()
    {
        if (weaponImage == null)
            weaponImage = GetComponent<Image>();
    }

    public void SetWeapon(WeaponData weapon)
    {
        // 1. JIKA SLOT KOSONG (EMPTY)
        if (weapon == null)
        {
            if (weaponImage != null)
            {
                weaponImage.sprite = null;
                weaponImage.enabled = false;
            }
            if (weaponName != null) weaponName.text = "Empty";

            // MEMBUAT TEKS STATS KOSONG TOTAL SAAT TIDAK ADA SENJATA
            if (weaponStats != null) weaponStats.text = "";
            return;
        }

        // 2. TAMPILKAN GAMBAR DAN NAMA JIKA SENJATA ADA
        if (weaponImage != null)
        {
            weaponImage.enabled = true;
            weaponImage.sprite = weapon.icon;
        }

        if (weaponName != null)
            weaponName.text = weapon.weaponName;

        // 3. TAMPILKAN STATS HANYA SAAT ADA SENJATA
        if (weaponStats != null)
        {
            // =========================================================================
            // 🛑 BAGIAN YANG DIGANTI: Membaca Rarity Dinamis dari overrideRarity
            // =========================================================================
            string rarity = "Normal"; // Nilai default awal

            if (!string.IsNullOrEmpty(weapon.overrideRarity))
            {
                // Jika senjata memiliki data kelangkaan dari peti gacha, pakai data itu!
                rarity = weapon.overrideRarity;
            }
            else
            {
                // Cadangan (Fallback) jika senjata ditaruh langsung di map tanpa lewat peti
                string nameLower = weapon.weaponName.ToLower();
                if (nameLower.Contains("rusty")) rarity = "Rusty";
                else if (nameLower.Contains("legendary")) rarity = "Legendary";
            }
            // =========================================================================

            // Deteksi Attack Speed (Ranged vs Melee)
            float atkSpeed = weapon.magazineSize > 0 ? weapon.fireRate : weapon.attackCooldown;

            // Cetak ke UI TextMeshPro
            weaponStats.text = $"Rarity: {rarity}\n" +
                               $"DMG: {weapon.damage}\n" +
                               $"ATK Speed: {atkSpeed}s";
        }
    }
}