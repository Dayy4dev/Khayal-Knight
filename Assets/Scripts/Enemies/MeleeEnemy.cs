using UnityEngine;
using System.Collections;
using UnityEngine.AI;

[AddComponentMenu("Enemies/Melee NavMesh Enemy")]
public class MeleeEnemy : MonoBehaviour, IDamageable
{
    [Header("References")]
    [SerializeField] private NavMeshAgent navAgent;
    [SerializeField] private Animator animator;
    private Transform playerTransform;
    private EnemySpawner spawner;

    [Header("Stats")]
    [SerializeField] private int maxHealth = 100;
    private int currentHealth;

    [Header("UI")]
    [SerializeField] private EnemyHealthBar healthBar;

    [Header("Layers")]
    [SerializeField] private LayerMask terrainLayer;      // Layer lantai/ground untuk patrol
    [SerializeField] private LayerMask playerLayerMask;   // Layer player untuk deteksi hit

    [Header("Patrol Settings")]
    [SerializeField] private float patrolRadius = 8f;
    private Vector3 currentPatrolPoint;
    private bool hasPatrolPoint;

    [Header("Combat Settings")]
    [SerializeField] private int damage = 10;
    [SerializeField] private float attackCooldown = 1.2f;
    private bool isOnAttackCooldown;

    [Header("Detection Ranges")]
    [SerializeField] private float visionRange = 10f;       // Jarak mulai mengejar
    [SerializeField] private float engagementRange = 1.8f;  // Jarak mulai memukul

    [Header("Audio")]
    [SerializeField] private AudioSource enemyAudioSource;
    [SerializeField] private AudioClip meleeAttackSound;

    [Header("Visual Feedback")]
    [SerializeField] private float hitFlashDuration = 0.1f;
    [SerializeField] private Color hitFlashColor = Color.red;

    private bool isPlayerVisible;
    private bool isPlayerInRange;
    private EnemyStunHandler stunHandler;

    // Hit flash
    private SkinnedMeshRenderer[] skinnedMeshRenderers;
    private Color[][] originalColors;
    private Coroutine hitFlashCoroutine;
    private bool renderersCached = false;
    private readonly string colorPropertyName = "_BaseColor";

    // ── Scaling API ───────────────────────────────────────────────────────────

    public int GetBaseHP() => maxHealth;
    public int GetBaseDamage() => damage;
    public float GetBaseSpeed() => navAgent != null ? navAgent.speed : 3.5f;


    void Start()
    {
        stunHandler = GetComponent<EnemyStunHandler>();
    }
    public void SetScaledStats(int hp, int dmg, float speed)
    {
        maxHealth = hp;
        currentHealth = hp;
        damage = dmg;

        if (navAgent != null) navAgent.speed = speed;
        if (healthBar != null) healthBar.SetMaxHealth(hp);
    }

    // ── Unity Lifecycle ───────────────────────────────────────────────────────

    private void Awake()
    {
        if (playerTransform == null)
        {
            GameObject playerObj = GameObject.FindWithTag("Player");
            if (playerObj != null) playerTransform = playerObj.transform;
        }

        if (navAgent == null) navAgent = GetComponent<NavMeshAgent>();
        if (animator == null) animator = GetComponentInChildren<Animator>();
        if (enemyAudioSource == null) enemyAudioSource = GetComponent<AudioSource>();

        // Cari spawner: cek parent dulu, lalu scene
        spawner = GetComponentInParent<EnemySpawner>();
        if (spawner == null) spawner = FindObjectOfType<EnemySpawner>();

        currentHealth = maxHealth;
        if (healthBar != null) healthBar.SetMaxHealth(maxHealth);
    }

    private void Update()
    {
        DetectPlayer();
        UpdateBehaviourState();
        UpdateAnimation();
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

    // ── Behaviour State Machine ───────────────────────────────────────────────

    private void UpdateBehaviourState()
    {
        if (playerTransform == null) { PerformPatrol(); return; }

        if (!isPlayerVisible && !isPlayerInRange) PerformPatrol();
        else if (isPlayerVisible && !isPlayerInRange) PerformChase();
        else if (isPlayerVisible && isPlayerInRange) PerformAttack();
    }

    private void FindPatrolPoint()
    {
        float rx = Random.Range(-patrolRadius, patrolRadius);
        float rz = Random.Range(-patrolRadius, patrolRadius);
        Vector3 candidate = new Vector3(transform.position.x + rx, transform.position.y, transform.position.z + rz);

        if (Physics.Raycast(candidate + Vector3.up * 2f, Vector3.down, out RaycastHit hit, 5f, terrainLayer))
        {
            currentPatrolPoint = hit.point;
            hasPatrolPoint = true;
        }
    }

    private void PerformPatrol()
    {
        if (!hasPatrolPoint) FindPatrolPoint();

        if (hasPatrolPoint && navAgent != null && navAgent.isOnNavMesh)
            navAgent.SetDestination(currentPatrolPoint);

        if (Vector3.Distance(transform.position, currentPatrolPoint) < 1.5f)
            hasPatrolPoint = false;
    }

    private void PerformChase()
    {
        if (playerTransform == null || navAgent == null || !navAgent.isOnNavMesh) return;

        navAgent.stoppingDistance = 0f;
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
            //Picu animasi attack acak
            TriggerRandomAttackAnimation();

            // Eksekusi damage dan cooldown
            ExecuteMeleeStrike();
            StartCoroutine(AttackCooldownRoutine());
        }
    }

    private void ExecuteMeleeStrike()
    {
        if (enemyAudioSource != null && meleeAttackSound != null)
            enemyAudioSource.PlayOneShot(meleeAttackSound);

        // if (animator != null) animator.SetTrigger("Attack");

        Vector3 spherePos = transform.position + transform.forward * (engagementRange * 0.5f);
        Collider[] hits = Physics.OverlapSphere(spherePos, engagementRange, playerLayerMask);

        foreach (var c in hits)
        {
            if (c == null) continue;

            if (c.CompareTag("Player"))
                Debug.Log($"[MeleeEnemy] {gameObject.name} memukul player: {c.gameObject.name}");

            if (c.TryGetComponent<PlayerHealth>(out var ph)) ph.TakeDamage(damage);
            else if (c.TryGetComponent<IDamageable>(out var d)) d.TakeDamage(damage);
        }
    }

    private IEnumerator AttackCooldownRoutine()
    {
        isOnAttackCooldown = true;
        yield return new WaitForSeconds(attackCooldown);
        isOnAttackCooldown = false;
    }

    private void UpdateAnimation()
    {
        if (animator == null || navAgent == null) return;

        Vector3 localVelocity = transform.InverseTransformDirection(navAgent.velocity);
        animator.SetFloat("Horizontal", localVelocity.x);
        animator.SetFloat("Vertical", localVelocity.z);
    }

    private void TriggerRandomAttackAnimation()
    {
        if (animator == null) return;

        // Pilih angka acak 1, 2, atau 3
        int randomAttack = Random.Range(1, 4);

        // Reset trigger lain agar tidak bentrok
        animator.ResetTrigger("Attack1");
        animator.ResetTrigger("Attack2");
        animator.ResetTrigger("Attack3");

        // Picu trigger yang terpilih
        if (randomAttack == 1) animator.SetTrigger("Attack1");
        else if (randomAttack == 2) animator.SetTrigger("Attack2");
        else animator.SetTrigger("Attack3");
    }

    // ── IDamageable ───────────────────────────────────────────────────────────

    public void TakeDamage(int amount)
    {
        currentHealth = Mathf.Clamp(currentHealth - amount, 0, maxHealth);

        if (healthBar != null) healthBar.SetHealth(currentHealth);

        // Hit flash
        if (!renderersCached) CacheRenderers();
        if (skinnedMeshRenderers != null && skinnedMeshRenderers.Length > 0)
        {
            if (hitFlashCoroutine != null) StopCoroutine(hitFlashCoroutine);
            hitFlashCoroutine = StartCoroutine(HitFlash());
        }

        Debug.Log($"[MeleeEnemy] {gameObject.name} kena {amount} damage — HP: {currentHealth}/{maxHealth}");

        if (currentHealth <= 0) Die();
    }

    private void CacheRenderers()
    {
        if (renderersCached) return;
        skinnedMeshRenderers = GetComponentsInChildren<SkinnedMeshRenderer>();
        if (skinnedMeshRenderers.Length == 0) return;

        originalColors = new Color[skinnedMeshRenderers.Length][];
        for (int i = 0; i < skinnedMeshRenderers.Length; i++)
        {
            Material[] mats = skinnedMeshRenderers[i].materials;
            originalColors[i] = new Color[mats.Length];
            for (int j = 0; j < mats.Length; j++)
            {
                if (mats[j].HasProperty(colorPropertyName))
                    originalColors[i][j] = mats[j].GetColor(colorPropertyName);
            }
        }
        renderersCached = true;
    }

    private IEnumerator HitFlash()
    {
        ChangeMeshColor(hitFlashColor);
        yield return new WaitForSeconds(hitFlashDuration);
        ResetMeshColor();
        hitFlashCoroutine = null;
    }

    private void ChangeMeshColor(Color color)
    {
        for (int i = 0; i < skinnedMeshRenderers.Length; i++)
        {
            if (skinnedMeshRenderers[i] == null) continue;
            Material[] mats = skinnedMeshRenderers[i].materials;
            for (int j = 0; j < mats.Length; j++)
            {
                if (mats[j].HasProperty(colorPropertyName))
                    mats[j].SetColor(colorPropertyName, color);
            }
        }
    }

    private void ResetMeshColor()
    {
        for (int i = 0; i < skinnedMeshRenderers.Length; i++)
        {
            if (skinnedMeshRenderers[i] == null) continue;
            Material[] mats = skinnedMeshRenderers[i].materials;
            for (int j = 0; j < mats.Length; j++)
            {
                if (mats[j].HasProperty(colorPropertyName))
                    mats[j].SetColor(colorPropertyName, originalColors[i][j]);
            }
        }
    }

    private void Die()
    {
        Debug.Log($"[MeleeEnemy] {gameObject.name} mati!");

        if (spawner != null) spawner.NotifyEnemyDestroyed(gameObject);

        Destroy(gameObject, 0.1f);
    }
}