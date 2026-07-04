using UnityEngine;

public class EnemySpawner : MonoBehaviour
{
    [Header("Spawn Settings")]
    public GameObject enemyPrefab;
    public int spawnCount = 3;
    public float spawnRadius = 5f; 

    [Header("Layer Mask Setup")]
    public LayerMask groundLayer;  

    // ─── TAMBAHKAN VARIABEL INI UTK PENGAMAN ────────────────────────
    private bool hasSpawned = false;
    public bool HasSpawned => hasSpawned; // Properti agar bisa dibaca script lain
    // ────────────────────────────────────────────────────────────────

    public void SpawnEnemies()
    {
        // JIKA SUDAH PERNAH SPAWN, JANGAN JALANKAN LAGI!
        if (hasSpawned) return; 

        if (enemyPrefab == null)
        {
            Debug.LogWarning($"[EnemySpawner] Enemy Prefab belum di-assign di {gameObject.name}!");
            return;
        }

        for (int i = 0; i < spawnCount; i++)
        {
            Vector2 randomCircle = Random.insideUnitCircle * spawnRadius;
            Vector3 spawnPos = new Vector3(
                transform.position.x + randomCircle.x,
                transform.position.y,
                transform.position.z + randomCircle.y
            );

            if (Physics.Raycast(spawnPos + Vector3.up * 5f, Vector3.down, out RaycastHit hit, 10f, groundLayer))
            {
                spawnPos = hit.point;
            }

            GameObject enemy = Instantiate(enemyPrefab, spawnPos, Quaternion.identity, transform.parent);
            enemy.tag = "Enemy";
        }

        hasSpawned = true; // Tandai bahwa spawner ini sudah memunculkan musuh!
        Debug.Log($"[EnemySpawner] Berhasil spawn {spawnCount} musuh di {transform.parent.name}");
    }

    public void NotifyEnemyDestroyed(GameObject enemy)
    {
        Debug.Log($"[EnemySpawner] Musuh {enemy.name} melaporkan kematian.");
        if (DungeonManager.Instance != null)
        {
            DungeonManager.Instance.OnEnemyKilled();
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, spawnRadius);
    }
}