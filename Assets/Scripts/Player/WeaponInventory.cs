using System.Collections.Generic;
using UnityEngine;

public class WeaponInventory : MonoBehaviour
{
    public static WeaponInventory Instance { get; private set; }

    [Header("Equipped Weapon Slots")]
    public WeaponData primaryWeapon;
    public WeaponData secondaryWeapon;

    [Header("Inventory Pool")]
    public List<WeaponData> ownedWeapons = new List<WeaponData>();

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else if (Instance != this)
        {
            Destroy(this);
        }
    }

    /// <summary>
    /// Menambahkan senjata ke pool dengan validasi anti-duplikat tipe/kategori
    /// </summary>
    public void AddWeaponToPool(WeaponData weapon)
    {
        if (weapon == null) return;

        // 1. Tentukan kategori dari senjata baru yang ingin dimasukkan
        string newCategory = GetWeaponCategory(weapon);

        // Jika senjata baru tidak masuk kategori yang dibatasi (misal: "Unarmed"), langsung masukkan
        if (newCategory == "Unknown")
        {
            if (!ownedWeapons.Contains(weapon))
            {
                ownedWeapons.Add(weapon);
                Debug.Log($"[Inventory] Berhasil menambahkan senjata umum: {weapon.weaponName}");
            }
            return;
        }

        // 2. VALIDASI: Cek apakah di inventory sudah ada senjata dengan kategori yang sama
        foreach (WeaponData owned in ownedWeapons)
        {
            string ownedCategory = GetWeaponCategory(owned);

            if (ownedCategory == newCategory)
            {
                Debug.LogWarning($"[Inventory] Gagal! Player tidak bisa menyimpan dua senjata tipe {newCategory} sekaligus. Kamu sudah memiliki '{owned.weaponName}'.");
                return; // Membatalkan penambahan (No Bug / Auto block)
            }
        }

        // 3. Jika lolos pengecekan dan belum ada di pool
        if (!ownedWeapons.Contains(weapon))
        {
            ownedWeapons.Add(weapon);
            Debug.Log($"[Inventory] Berhasil menambahkan ke pool: {weapon.weaponName} ({newCategory})");
        }
    }

    /// <summary>
    /// Helper untuk mendeteksi kategori berdasarkan string nama senjata (Sesuai penamaan di PlayerAttack.cs)
    /// </summary>
    private string GetWeaponCategory(WeaponData data)
    {
        if (data == null || string.IsNullOrEmpty(data.weaponName)) return "Unknown";

        string nameLower = data.weaponName.ToLower();

        // Deteksi Sword
        if (nameLower.Contains("sword") || nameLower.Contains("blade") || nameLower.Contains("calibur"))
            return "Sword";

        // Deteksi Cannon
        if (nameLower.Contains("cannon") || nameLower.Contains("blaster") || nameLower.Contains("artillery"))
            return "Cannon";

        // Deteksi Hammer
        if (nameLower.Contains("hammer") || nameLower.Contains("mallet"))
            return "Hammer";

        return "Unknown";
    }
}