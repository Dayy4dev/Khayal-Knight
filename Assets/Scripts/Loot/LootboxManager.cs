using UnityEngine;

public class LootboxManager : MonoBehaviour
{
    public static LootboxManager Instance { get; private set; }

    // 4 VARIAN RARITY SESUAI PERMINTAAN
    public enum Rarity { Legendary, Rare, Common, Rusty }

    // --- TEMPAT MENYIMPAN BUFFER NILAI BUFF BARU YANG DIDAPAT ---
    [HideInInspector] public int currentAtkBuff = 0;
    [HideInInspector] public int currentMaxHpBuff = 0;

    public struct GachaOutput
    {
        public GameObject spawnedObject;
        public Rarity finalRarity;
    }

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    public void SetCandyEffectStatus(string effectName)
    {
        switch (effectName)
        {
            case "Heal":
                break;
            case "MaxHP":
                currentMaxHpBuff = 30; 
                break;
            case "BuffDamage":
                currentAtkBuff = 10; 
                break;
            case "InstantDamage":
                break;
        }
    }

    // FUNGSI GACHA MEMBAWA 4 POOL VARIAN BARU
    public GachaOutput OpenWeaponBox(GameObject[] legendaryPool, GameObject[] rarePool, GameObject[] commonPool, GameObject[] rustyPool, string weaponName, Vector3 spawnPosition, Transform roomParent)
    {
        int roll = Random.Range(1, 11); // Roll angka 1 sampai 10
        Rarity obtainedRarity;
        GameObject[] selectedPool = null;

        // Reset buffer sebelum diisi gacha baru
        currentAtkBuff = 0;
        currentMaxHpBuff = 0;

        // SISTEM PROBABILITAS GACHA (4 VARIAN)
        if (roll == 1) // Peluang 10% (Angka 1)
        {
            obtainedRarity = Rarity.Legendary;
            selectedPool = legendaryPool;
            currentAtkBuff = 20; // Buff ATK tertinggi untuk Legendary
        }
        else if (roll <= 3) // Peluang 20% (Angka 2, 3)
        {
            obtainedRarity = Rarity.Rare;
            selectedPool = rarePool;
            currentAtkBuff = 10; // Buff ATK menengah untuk Rare
        }
        else if (roll <= 6) // Peluang 30% (Angka 4, 5, 6)
        {
            obtainedRarity = Rarity.Common;
            selectedPool = commonPool;
            currentAtkBuff = 0;
        }
        else // Peluang 40% (Angka 7, 8, 9, 10)
        {
            obtainedRarity = Rarity.Rusty;
            selectedPool = rustyPool;
            currentAtkBuff = 0;
        }

        GameObject prefabToSpawn = null;
        if (selectedPool != null && selectedPool.Length > 0)
        {
            int randomWeaponIndex = Random.Range(0, selectedPool.Length);
            prefabToSpawn = selectedPool[randomWeaponIndex];
        }

        if (prefabToSpawn != null)
        {
            GameObject spawnedWeapon = Instantiate(prefabToSpawn, spawnPosition, Quaternion.identity);
            if (roomParent != null) spawnedWeapon.transform.parent = roomParent;
            return new GachaOutput { spawnedObject = spawnedWeapon, finalRarity = obtainedRarity };
        }
        return default;
    }

    public void ResetCurrentBuffs()
    {
        currentAtkBuff = 0;
        currentMaxHpBuff = 0;
    }
}