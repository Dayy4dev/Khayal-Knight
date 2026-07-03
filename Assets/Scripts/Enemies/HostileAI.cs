using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.AI;

public class HostileAI : MonoBehaviour, IDamageable
{
    [Header("References")]
    [SerializeField] private NavMeshAgent navAgent;
    [SerializeField] private Animator animator;
    [SerializeField] private Transform playerTransform;
    [SerializeField] private Transform firePoint;
    [SerializeField] private GameObject projectilePrefab;

    [Header("Layers")]
    [SerializeField] private LayerMask terrainLayer;
    [SerializeField] private LayerMask playerLayerMask;

    [Header("Stats")]
    [SerializeField] private int maxHealth = 80;
    [SerializeField] private int projectileDamage = 15;
    private int currentHealth;

    [Header("UI")]
    private EnemyHealthBar healthBar;

    [Header("Patrol Settings")]
    [SerializeField] private float patrolRadius = 10f;
    private Vector3 currentPatrolPoint;
    private bool hasPatrolPoint;

    [Header("Combat Settings")]
    [SerializeField] private float attackCooldown = 1f;
    private bool isOnAttackCooldown;
    [SerializeField] private float projectileSpeed = 20f;
    [SerializeField] private float accuracyError = 0.5f;   // 0 = tepat, makin tinggi makin meleset
    [SerializeField] private bool usePrediction = true;    // toggle lead prediction
    [SerializeField] private int projectilePoolSize = 10;
    [SerializeField] private float projectileLifetime = 3f;

    [Header("Detection Ranges")]
    [SerializeField] private float visionRange = 20f;
    [SerializeField] private float engagementRange = 10f;

    [Header("Audio")]
    [SerializeField] private AudioSource hostileAudioSource;
    [SerializeField] private AudioClip rangedShootSound;

    [Header("Visual Feedback")]
    [SerializeField] private float hitFlashDuration = 0.1f;
    [SerializeField] private Color hitFlashColor = Color.red;
    [SerializeField] private AudioClip[] enemyHurtSounds;
[SerializeField] private AudioClip enemyDeathSound;

    private bool isPlayerVisible;
    private bool isPlayerInRange;
    private readonly Queue<GameObject> projectilePool = new Queue<GameObject>();
    private Transform projectilePoolRoot;
    private EnemySpawner spawner;
    private bool isDead = false;
    private EnemyStunHandler stunHandler;

    // Hit flash
    private SkinnedMeshRenderer[] skinnedMeshRenderers;
    private Color[][] originalColors;
    private Coroutine hitFlashCoroutine;
    private bool renderersCached = false;
    private readonly string colorPropertyName = "_BaseColor";

    // ── Scaling API ───────────────────────────────────────────────────────────

    public int GetBaseHP() => maxHealth;
    public int GetBaseDamage() => projectileDamage;
    public float GetBaseSpeed() => navAgent != null ? navAgent.speed : 3.5f;

    void Start()
    {
        stunHandler = GetComponent<EnemyStunHandler>();
    }
    public void SetScaledStats(int hp, int dmg, float speed)
    {
        maxHealth = hp;
        projectileDamage = dmg;
        currentHealth = hp;

        if (navAgent != null) navAgent.speed = speed;
        if (healthBar != null) healthBar.SetMaxHealth(hp);
    }

    // ── Unity Lifecycle ───────────────────────────────────────────────────────

    private void Awake()
    {
        GameObject playerObj = GameObject.FindWithTag("Player");
        if (playerObj != null) playerTransform = playerObj.transform;

        if (navAgent == null) navAgent = GetComponent<NavMeshAgent>();
        if (animator == null) animator = GetComponent<Animator>();
        if (hostileAudioSource == null) hostileAudioSource = GetComponent<AudioSource>();

        healthBar = GetComponentInChildren<EnemyHealthBar>();

        spawner = GetComponentInParent<EnemySpawner>();
        if (spawner == null) spawner = FindObjectOfType<EnemySpawner>();

        currentHealth = maxHealth;
        if (healthBar != null) healthBar.SetMaxHealth(maxHealth);

        InitializeProjectilePool();
    }

    private void Update()
    {
        if (isDead) return;

        DetectPlayer();
        UpdateBehaviourState();

        if (stunHandler != null && stunHandler.IsStunned) return;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, engagementRange);
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, visionRange);
    }

    // ── Detection ─────────────────────────────────────────────────────────────

    private void DetectPlayer()
    {
        if (playerTransform == null)
        {
            GameObject playerObj = GameObject.FindWithTag("Player");
            if (playerObj != null) playerTransform = playerObj.transform;
        }

        if (playerTransform == null)
        {
            isPlayerVisible = false;
            isPlayerInRange = false;
            return;
        }

        float dist = Vector3.Distance(transform.position, playerTransform.position);
        isPlayerVisible = dist <= visionRange;
        isPlayerInRange = dist <= engagementRange;
    }

    // ── Projectile Pool ───────────────────────────────────────────────────────

    private void InitializeProjectilePool()
    {
        if (projectilePrefab == null) return;

        projectilePoolRoot = new GameObject("ProjectilePool - " + gameObject.name).transform;

        for (int i = 0; i < projectilePoolSize; i++)
        {
            GameObject p = Instantiate(projectilePrefab, projectilePoolRoot);
            p.SetActive(false);
            projectilePool.Enqueue(p);
        }
    }

    private GameObject GetProjectileFromPool()
    {
        if (projectilePool.Count == 0)
        {
            GameObject p = Instantiate(projectilePrefab, projectilePoolRoot);
            p.SetActive(false);
            projectilePool.Enqueue(p);
        }
        return projectilePool.Dequeue();
    }

    private void FireProjectile()
    {
        if (projectilePrefab == null || firePoint == null || playerTransform == null) return;

        if (hostileAudioSource != null && rangedShootSound != null)
            hostileAudioSource.PlayOneShot(rangedShootSound);

        GameObject bullet = GetProjectileFromPool();
        bullet.transform.position = firePoint.position;
        bullet.transform.rotation = Quaternion.identity;
        bullet.SetActive(true);

        // Sinkronkan damage ke Projectile sesuai scaling
        Projectile proj = bullet.GetComponent<Projectile>();
        if (proj != null) proj.damage = projectileDamage;

        // EnemyBullet prefab uses Bullet.cs — configure speed & damage
        Bullet enemyBullet = bullet.GetComponent<Bullet>();
        if (enemyBullet != null) enemyBullet.Setup(projectileSpeed, projectileDamage);

        Rigidbody rb = bullet.GetComponent<Rigidbody>();
        if (rb == null) return;

        Vector3 targetPos = playerTransform.position + Vector3.up * 1f;

        if (usePrediction)
        {
            float dist = Vector3.Distance(firePoint.position, targetPos);
            float travelTime = dist / projectileSpeed;

            // Coba ambil velocity dari NavMeshAgent atau Rigidbody player
            Vector3 playerVel = Vector3.zero;
            NavMeshAgent pAgent = playerTransform.GetComponent<NavMeshAgent>();
            Rigidbody pRb = playerTransform.GetComponent<Rigidbody>();

            if (pAgent != null) playerVel = pAgent.velocity;
            else if (pRb != null) playerVel = pRb.linearVelocity;

            targetPos += playerVel * travelTime;
        }

        Vector3 dir = (targetPos - firePoint.position).normalized;
        float distance = Vector3.Distance(firePoint.position, targetPos);
        float tEst = distance / projectileSpeed;
        float gravOff = 0.5f * Mathf.Abs(Physics.gravity.y) * tEst * tEst;
        if (distance > 0.001f)
            dir = (dir + Vector3.up * (gravOff / distance)).normalized;

        if (accuracyError > 0f)
        {
            dir += new Vector3(
                Random.Range(-accuracyError, accuracyError),
                Random.Range(-accuracyError, accuracyError),
                Random.Range(-accuracyError, accuracyError)) * 0.05f;
            dir.Normalize();
        }

        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
        rb.linearVelocity = dir * projectileSpeed;
        bullet.transform.forward = dir;

        StartCoroutine(ReturnProjectileToPool(bullet, projectileLifetime));
    }

    private IEnumerator ReturnProjectileToPool(GameObject projectile, float delay)
    {
        yield return new WaitForSeconds(delay);

        // Pastikan projectile dan pool root-nya masih ada sebelum dikembalikan
        if (projectile != null && projectilePoolRoot != null)
        {
            projectile.SetActive(false);
            projectile.transform.SetParent(projectilePoolRoot);
            projectilePool.Enqueue(projectile);
        }
        else if (projectile != null && projectilePoolRoot == null)
        {
            // Jika pool-nya sudah hancur (musuh mati), hancurkan saja pelurunya langsung
            Destroy(projectile);
        }
    }

    private void UpdateBehaviourState()
    {
        if (!isPlayerVisible && !isPlayerInRange) PerformPatrol();
        else if (isPlayerVisible && !isPlayerInRange) PerformChase();
        else if (isPlayerVisible && isPlayerInRange) PerformAttack();

        UpdateAnimatorParameters();
    }

    private void FindPatrolPoint()
    {
        float rx = Random.Range(-patrolRadius, patrolRadius);
        float rz = Random.Range(-patrolRadius, patrolRadius);
        Vector3 candidate = new Vector3(transform.position.x + rx, transform.position.y, transform.position.z + rz);

        if (Physics.Raycast(candidate, -transform.up, 2f, terrainLayer))
        {
            currentPatrolPoint = candidate;
            hasPatrolPoint = true;
        }
    }

    private void PerformPatrol()
    {
        if (!hasPatrolPoint) FindPatrolPoint();

        if (hasPatrolPoint && navAgent != null && navAgent.isOnNavMesh)
            navAgent.SetDestination(currentPatrolPoint);

        if (Vector3.Distance(transform.position, currentPatrolPoint) < 1f)
            hasPatrolPoint = false;
    }

    private void PerformChase()
    {
        if (playerTransform != null && navAgent != null && navAgent.isOnNavMesh)
            navAgent.SetDestination(playerTransform.position);
    }

    private void PerformAttack()
    {
        if (navAgent != null && navAgent.isOnNavMesh)
            navAgent.SetDestination(transform.position); // berhenti di tempat

        if (playerTransform != null)
        {
            Vector3 lookTarget = new Vector3(playerTransform.position.x, transform.position.y, playerTransform.position.z);
            transform.LookAt(lookTarget);
        }

        if (!isOnAttackCooldown)
        {
            FireProjectile();
            StartCoroutine(AttackCooldownRoutine());
        }
    }

    private IEnumerator AttackCooldownRoutine()
    {
        isOnAttackCooldown = true;
        yield return new WaitForSeconds(attackCooldown);
        isOnAttackCooldown = false;
    }

    private void UpdateAnimatorParameters()
    {
        if (animator == null || navAgent == null) return;

        Vector3 localVelocity = transform.InverseTransformDirection(navAgent.velocity);
        animator.SetFloat("Horizontal", localVelocity.x);
        animator.SetFloat("Vertical", localVelocity.z);
    }

    public void TakeDamage(int amount)
    {
        if (isDead) return; // Jaga agar tidak memproses damage jika sudah mati

    currentHealth = Mathf.Clamp(currentHealth - amount, 0, maxHealth);

        if (healthBar != null) healthBar.SetHealth(currentHealth);

    Debug.Log($"[HostileAI] {gameObject.name} kena {amount} damage — HP: {currentHealth}/{maxHealth}");

        if (currentHealth <= 0)
        {
            isDead = true; // Tandai sudah mati
            Die();
        }
    }

    private void Die()
{
    Debug.Log($"[HostileAI] {gameObject.name} mati!");

    if (spawner != null) spawner.NotifyEnemyDestroyed(gameObject);

    if (projectilePoolRoot != null)
    {
        Destroy(projectilePoolRoot.gameObject);
    }

    // Mematikan agent dan collider agar tidak mengganggu game
    if (navAgent != null) navAgent.enabled = false;
    Collider col = GetComponent<Collider>();
    if (col != null) col.enabled = false;

    float destroyDelay = 0.1f;

    // --- TAMBAHAN: Memutar suara kematian ---
    if (hostileAudioSource != null && enemyDeathSound != null)
    {
        hostileAudioSource.PlayOneShot(enemyDeathSound);
        // Menunggu suara selesai sebelum menghancurkan GameObject
        destroyDelay = Mathf.Max(destroyDelay, enemyDeathSound.length);
    }

    Destroy(gameObject, destroyDelay);
}
}