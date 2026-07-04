using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

// Attach ke prefab Boss.
// Boss punya HP besar, spawn minion berkala, dan 2 fase (fase 2 saat HP < 50%).

public class BossController : MonoBehaviour, IDamageable
{
    [Header("Boss Stats")]
    public int maxHealth = 2000;
    public int contactDamage = 30;
    private int currentHealth;

    [Header("Minion Settings")]
    public GameObject[] minionPrefabs;          // Prefab minion yang di-spawn
    public Transform[] minionSpawnPoints;       // Titik spawn minion
    public int maxMinions = 4;                  // Maks minion aktif sekaligus
    public float minionSpawnInterval = 8f;      // Interval spawn minion (detik)
    public int minionsPerWave = 2;              // Jumlah minion per wave

    [Header("Movement")]
    public float moveSpeed = 4f;
    public float chargeSpeed = 12f;
    public float engagementRange = 3f;
    public float visionRange = 30f;

    [Header("Phase 2")]
    [Range(0f, 1f)]
    public float phase2Threshold = 0.5f;       // Masuk fase 2 saat HP < 50%
    public float phase2SpeedBonus = 2f;        // Bonus speed di fase 2
    public float phase2SpawnIntervalBonus = 3f; // Spawn minion lebih cepat di fase 2

    [Header("UI")]
    public EnemyHealthBar healthBar;

    [Header("References")]
    private NavMeshAgent navAgent;
    private Transform playerTransform;
    private Animator animator;

    // State
    private bool isPhase2 = false;
    private bool isDead = false;
    private List<GameObject> activeMinions = new List<GameObject>();
    private Coroutine minionSpawnCoroutine;

    // ── EnemyScaling API (boss juga di-scale) ────────────────────────────────

    public int   GetBaseHP()     => maxHealth;
    public int   GetBaseDamage() => contactDamage;
    public float GetBaseSpeed()  => moveSpeed;

    public void SetScaledStats(int hp, int dmg, float speed)
    {
        maxHealth     = hp;
        contactDamage = dmg;
        moveSpeed     = speed;
        currentHealth = hp;
        if (healthBar != null) healthBar.SetMaxHealth(hp);
        if (navAgent != null) navAgent.speed = speed;
    }

    // ── Unity Lifecycle ──────────────────────────────────────────────────────

    void Awake()
    {
        navAgent = GetComponent<NavMeshAgent>();
        animator = GetComponent<Animator>();

        if (navAgent != null) navAgent.speed = moveSpeed;

        GameObject playerObj = GameObject.FindWithTag("Player");
        if (playerObj != null) playerTransform = playerObj.transform;
    }

    void Start()
    {
        currentHealth = maxHealth;
        if (healthBar != null) healthBar.SetMaxHealth(maxHealth);

        // Mulai spawn minion
        minionSpawnCoroutine = StartCoroutine(MinionSpawnRoutine());

        Debug.Log("[Boss] Boss aktif!");
    }

    void Update()
    {
        if (isDead) return;

        if (playerTransform == null)
        {
            GameObject playerObj = GameObject.FindWithTag("Player");
            if (playerObj != null) playerTransform = playerObj.transform;
        }

        UpdateMovement();
        CheckPhase2();
    }

    // ── Movement ─────────────────────────────────────────────────────────────

    void UpdateMovement()
    {
        if (playerTransform == null || navAgent == null) return;

        float dist = Vector3.Distance(transform.position, playerTransform.position);

        if (dist <= visionRange)
        {
            navAgent.SetDestination(playerTransform.position);
            navAgent.stoppingDistance = engagementRange * 0.8f;
        }
    }

    // ── Phase 2 ──────────────────────────────────────────────────────────────

    void CheckPhase2()
    {
        if (isPhase2) return;
        if ((float)currentHealth / maxHealth <= phase2Threshold)
            EnterPhase2();
    }

    void EnterPhase2()
    {
        isPhase2 = true;
        Debug.Log("[Boss] FASE 2 DIMULAI!");

        // Boost speed
        if (navAgent != null) navAgent.speed = moveSpeed + phase2SpeedBonus;

        // Restart spawn routine dengan interval lebih cepat
        if (minionSpawnCoroutine != null) StopCoroutine(minionSpawnCoroutine);
        minionSpawnCoroutine = StartCoroutine(MinionSpawnRoutine());

        // Spawn wave minion langsung saat masuk fase 2
        StartCoroutine(SpawnMinionWave());
    }

    // ── Minion Spawning ──────────────────────────────────────────────────────

    IEnumerator MinionSpawnRoutine()
    {
        float interval = isPhase2
            ? minionSpawnInterval - phase2SpawnIntervalBonus
            : minionSpawnInterval;

        interval = Mathf.Max(interval, 2f); // Minimum 2 detik

        while (!isDead)
        {
            yield return new WaitForSeconds(interval);
            StartCoroutine(SpawnMinionWave());

            // Recalculate interval (kalau masuk fase 2 di tengah jalan)
            interval = isPhase2
                ? minionSpawnInterval - phase2SpawnIntervalBonus
                : minionSpawnInterval;
            interval = Mathf.Max(interval, 2f);
        }
    }

    IEnumerator SpawnMinionWave()
    {
        // Bersihkan list minion yang sudah mati
        activeMinions.RemoveAll(m => m == null);

        if (activeMinions.Count >= maxMinions) yield break;
        if (minionPrefabs == null || minionPrefabs.Length == 0) yield break;

        int toSpawn = Mathf.Min(minionsPerWave, maxMinions - activeMinions.Count);

        for (int i = 0; i < toSpawn; i++)
        {
            Vector3 spawnPos = GetMinionSpawnPosition(i);
            GameObject prefab = minionPrefabs[Random.Range(0, minionPrefabs.Length)];

            if (prefab == null) continue;

            GameObject minion = Instantiate(prefab, spawnPos, Quaternion.identity);
            activeMinions.Add(minion);

            // Scale minion berdasarkan floor juga
            EnemyScaling scaling = minion.GetComponent<EnemyScaling>();
            // EnemyScaling.Start() akan handle ini otomatis

            Debug.Log($"[Boss] Spawn minion: {prefab.name}");
            yield return new WaitForSeconds(0.3f); // Jeda antar spawn
        }
    }

    Vector3 GetMinionSpawnPosition(int index)
    {
        // Pakai spawn point kalau ada
        if (minionSpawnPoints != null && minionSpawnPoints.Length > 0)
            return minionSpawnPoints[index % minionSpawnPoints.Length].position;

        // Fallback: random di sekitar boss
        float angle = index * (360f / minionsPerWave);
        float radius = 3f;
        Vector3 offset = new Vector3(
            Mathf.Cos(angle * Mathf.Deg2Rad) * radius,
            0,
            Mathf.Sin(angle * Mathf.Deg2Rad) * radius
        );
        return transform.position + offset;
    }

    // ── Contact Damage ───────────────────────────────────────────────────────

    void OnCollisionStay(Collision collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            PlayerHealth ph = collision.gameObject.GetComponent<PlayerHealth>();
            if (ph != null) ph.TakeDamage(contactDamage);
        }
    }

    // ── IDamageable ──────────────────────────────────────────────────────────

    public void TakeDamage(int amount)
    {
        if (isDead) return;

        currentHealth -= amount;
        currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth);

        if (healthBar != null) healthBar.SetHealth(currentHealth);

        Debug.Log($"[Boss] Kena {amount} damage — HP: {currentHealth}/{maxHealth}");

        if (currentHealth <= 0) Die();
    }

    void Die()
    {
        isDead = true;

        // Hentikan spawn minion
        if (minionSpawnCoroutine != null) StopCoroutine(minionSpawnCoroutine);

        // Hancurkan semua minion aktif
        foreach (var minion in activeMinions)
            if (minion != null) Destroy(minion);

        Debug.Log("[Boss] BOSS MATI! Game selesai / lanjut ke next floor.");

        // Beritahu RoomController bahwa boss mati
        RoomController room = GetComponentInParent<RoomController>();
        if (room != null)
            room.ForceRoomClear();

        Destroy(gameObject, 0.5f);
    }
}