using UnityEngine;

public class LootboxRoomSpawner : MonoBehaviour
{
    [Header("Pilihan Peti yang Bisa Muncul (Isi dengan 3 Prefab Peti)")]
    public GameObject[] chestPrefabs; // Masukkan prefab PetiSword, PetiHandCannon, PetiSqueekHammer ke array ini

    [Header("Titik Muncul Peti (Kosongkan jika ingin tepat di tengah ruangan)")]
    public Transform chestSpawnPoint;

    // Fungsi ini dipanggil oleh DungeonManager saat ruangan clear
    public void SpawnRandomChest()
    {
        if (chestPrefabs == null || chestPrefabs.Length == 0)
        {
            Debug.LogWarning($"[LootboxRoomSpawner] Array peti kosong di {gameObject.name}!");
            return;
        }

        // 1. ACAK INDEKS: Memilih 1 nomor acak dari total prefab yang terdaftar (0 sampai 2)
        int randomIndex = Random.Range(0, chestPrefabs.Length);
        GameObject selectedChestPrefab = chestPrefabs[randomIndex];

        // 2. TENTUKAN POSISI: Menggunakan titik khusus atau koordinat pusat ruangan
        Vector3 spawnPosition = chestSpawnPoint != null ? chestSpawnPoint.position : transform.position;
        
        // Kasih sedikit jarak ke atas lantai agar tidak ambles ke bawah map isometric
        spawnPosition.y += 0.2f; 

        // 3. SPAWN PETI: Memunculkan 1 peti hasil acakan ke dalam game
        if (selectedChestPrefab != null)
        {
            GameObject spawnedChest = Instantiate(selectedChestPrefab, spawnPosition, Quaternion.identity);
            
            // Masukkan peti sebagai anak (child) dari ruangan ini agar ikut terhapus saat ganti floor level
            spawnedChest.transform.parent = this.transform;

            Debug.Log($"<color=orange>🎁 [Lootbox Spawn]</color> Ruangan Clear! Memunculkan 1 peti acak: <b>{spawnedChest.name}</b>");
        }
    }
}