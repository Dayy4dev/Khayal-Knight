using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
[RequireComponent(typeof(Animator))]
public class EnemyAI : MonoBehaviour
{
    public enum State { Chase, Retreat, AttackPlayer, AttackAirplane, Dead }
    [Header("Status")]
    public State currentState = State.Chase;

    [Header("References")]
    public NavMeshAgent agent;
    public Animator animator;
    public Transform player;              // Auto-cari via tag "Player" jika kosong
    public Transform airplaneTarget;      // Auto-cari via tag "Airplane" jika kosong
    public Transform firePoint;           // Tempat moncong senjata (Canon)
    public GameObject projectilePrefab;   // Prefab peluru (EnemyBullet)

    [Header("Layers")]
    public LayerMask terrainLayer;

    [Header("Stats")]
    public int maxHealth = 100;
    public int projectileDamage = 20;
    private int currentHealth;

    [Header("Chase & Retreat Settings")]
    public float chaseSpeed = 3.5f;
    public float stopChaseDistance = 1.5f;   // Jarak berhenti mendekat (nyaman menyerang)
    public float retreatDistance = 3f;       // Jika player lebih dekat dari ini -> mundur
    public float retreatStepDistance = 4f;   // Seberapa jauh mundur
    public bool faceTargetWhileMoving = true; // Menghadap player terus walau sedang mundur

    [Header("Combat Settings")]
    public float attackCooldown = 1f;        // Menggantikan fireRate agar mirip HostileAI
    private bool isOnAttackCooldown = false;
    public float projectileSpeed = 20f;
    public float accuracyError = 0.5f;       // 0 = tepat, makin tinggi makin meleset
    public bool usePrediction = true;        // Mengaktifkan fitur prediksi arah gerak target
    public int projectilePoolSize = 10;
    public float projectileLifetime = 3f;

    [Header("Shoot Settings (Detection)")]
    public float shootRange = 15f;           // Menggantikan engagementRange dari script lamamu

    [Header("Audio")]
    public AudioSource hostileAudioSource;
    public AudioClip rangedShootSound;
    public AudioClip[] enemyHurtSounds;
    public AudioClip enemyDeathSound;

    // Object Pooling untuk menghemat memori
    private readonly Queue<GameObject> projectilePool = new Queue<GameObject>();
    private Transform projectilePoolRoot;

    void Awake()
    {
        if (agent == null) agent = GetComponent<NavMeshAgent>();
        if (animator == null) animator = GetComponent<Animator>();
        if (hostileAudioSource == null) hostileAudioSource = GetComponent<AudioSource>();

        currentHealth = maxHealth;

        // Inisialisasi sistem Object Pool Peluru seperti di HostileAI
        InitializeProjectilePool();
    }

    void Start()
    {
        if (player == null)
        {
            GameObject playerObj = GameObject.FindWithTag("Player");
            if (playerObj != null) player = playerObj.transform;
        }
        if (airplaneTarget == null)
        {
            GameObject airplaneObj = GameObject.FindWithTag("Airplane");
            if (airplaneObj != null) airplaneTarget = airplaneObj.transform;
        }

        if (agent != null)
        {
            agent.speed = chaseSpeed;
            agent.updateRotation = !faceTargetWhileMoving;
        }
    }

    void Update()
    {
        if (currentState == State.Dead) return;

        float distToPlayer = player != null ? Vector3.Distance(transform.position, player.position) : float.MaxValue;
        float distToAirplane = airplaneTarget != null ? Vector3.Distance(transform.position, airplaneTarget.position) : float.MaxValue;

        // 1. PRIORITAS UTAMA: Interaksi dengan Player
        if (player != null && distToPlayer < retreatDistance)
        {
            Retreat();
        }
        else if (player != null && distToPlayer <= shootRange)
        {
            if (distToPlayer > stopChaseDistance)
            {
                ChasePlayer();
            }
            else
            {
                ShootPlayer();
            }
        }
        // 2. PRIORITAS KEDUA: Menyerang Pesawat jika Player berada di luar jangkauan tembak
        else if (airplaneTarget != null && distToAirplane <= shootRange)
        {
            AttackAirplane();
        }
        else
        {
            // Jika tidak ada target dalam jangkauan, beralih ke state Chase tapi berhenti bergerak
            currentState = State.Chase;
            if (agent != null) agent.isStopped = true;
        }

        UpdateAnimator();
    }

    void ChasePlayer()
    {
        currentState = State.Chase;
        if (agent != null && agent.isOnNavMesh)
        {
            agent.isStopped = false;
            agent.SetDestination(player.position);
        }

        if (faceTargetWhileMoving) FaceTarget(player.position);
    }

    void Retreat()
    {
        currentState = State.Retreat;

        Vector3 awayDir = (transform.position - player.position).normalized;
        Vector3 retreatPoint = transform.position + awayDir * retreatStepDistance;

        // Menggunakan NavMesh.SamplePosition agar musuh tidak mundur menembus tembok/keluar map
        if (NavMesh.SamplePosition(retreatPoint, out NavMeshHit hit, retreatStepDistance, NavMesh.AllAreas))
        {
            if (agent != null && agent.isOnNavMesh)
            {
                agent.isStopped = false;
                agent.SetDestination(hit.position);
            }
        }
        else
        {
            if (agent != null) agent.isStopped = true;
        }

        if (faceTargetWhileMoving) FaceTarget(player.position);
    }

    void ShootPlayer()
    {
        currentState = State.AttackPlayer;
        if (agent != null) agent.isStopped = true; // Berhenti jalan saat menembak

        FaceTarget(player.position);

        if (!isOnAttackCooldown)
        {
            ExecuteShoot(player);
            StartCoroutine(AttackCooldownRoutine());
        }
    }

    void AttackAirplane()
    {
        currentState = State.AttackAirplane;
        if (agent != null) agent.isStopped = true;

        FaceTarget(airplaneTarget.position);

        if (!isOnAttackCooldown)
        {
            ExecuteShoot(airplaneTarget); // Peluru mengincar pesawat, tapi animasi visual tetap berjalan
            StartCoroutine(AttackCooldownRoutine());
        }
    }

    private void ExecuteShoot(Transform target)
    {
        // Memicu trigger animasi serangan visual ("Shoot")
        if (animator != null) animator.SetTrigger("Shoot");

        // Memutar suara tembakan
        if (hostileAudioSource != null && rangedShootSound != null)
            hostileAudioSource.PlayOneShot(rangedShootSound);

        if (projectilePrefab == null || firePoint == null || target == null) return;

        // Mengambil peluru dari pool (menggantikan Instantiate biasa)
        GameObject bullet = GetProjectileFromPool();
        bullet.transform.position = firePoint.position;
        bullet.transform.rotation = Quaternion.identity;
        bullet.SetActive(true);

        // Mengirim data target ke script pergerakan peluru milikmu
        ProjectilePlane pPlane = bullet.GetComponent<ProjectilePlane>();
        if (pPlane != null) pPlane.SetTarget(target);

        // Jika peluru menggunakan script bawaan prototype lama (Bullet / Projectile), sinkronkan statistiknya di sini
        Bullet enemyBullet = bullet.GetComponent<Bullet>();
        if (enemyBullet != null) enemyBullet.Setup(projectileSpeed, projectileDamage);

        Rigidbody rb = bullet.GetComponent<Rigidbody>();
        if (rb == null) return;

        Vector3 targetPos = target.position + Vector3.up * 1f; // Menembak ke arah tengah tubuh target

        // FITUR CANGGIH: Prediksi pergerakan target (Lead Prediction)
        if (usePrediction)
        {
            float dist = Vector3.Distance(firePoint.position, targetPos);
            float travelTime = dist / projectileSpeed;

            Vector3 targetVel = Vector3.zero;
            NavMeshAgent targetAgent = target.GetComponent<NavMeshAgent>();
            Rigidbody targetRb = target.GetComponent<Rigidbody>();

            if (targetAgent != null) targetVel = targetAgent.velocity;
            else if (targetRb != null) targetVel = targetRb.linearVelocity;

            targetPos += targetVel * travelTime;
        }

        // FITUR CANGGIH: Menghitung lengkungan kompensasi gravitasi peluru
        Vector3 dir = (targetPos - firePoint.position).normalized;
        float distance = Vector3.Distance(firePoint.position, targetPos);
        float tEst = distance / projectileSpeed;
        float gravOff = 0.5f * Mathf.Abs(Physics.gravity.y) * tEst * tEst;
        if (distance > 0.001f)
            dir = (dir + Vector3.up * (gravOff / distance)).normalized;

        // FITUR CANGGIH: Efek Accuracy Error (Akurasi acak agar tembakan tidak melulu 100% lurus roboti)
        if (accuracyError > 0f)
        {
            dir += new Vector3(
                Random.Range(-accuracyError, accuracyError),
                Random.Range(-accuracyError, accuracyError),
                Random.Range(-accuracyError, accuracyError)) * 0.05f;
            dir.Normalize();
        }

        // Jalankan peluru menggunakan Rigidbody Velocity
        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
        rb.linearVelocity = dir * projectileSpeed;
        bullet.transform.forward = dir;

        // Kembalikan peluru otomatis ke pool setelah lifetime habis
        StartCoroutine(ReturnProjectileToPool(bullet, projectileLifetime));
    }

    private IEnumerator AttackCooldownRoutine()
    {
        isOnAttackCooldown = true;
        yield return new WaitForSeconds(attackCooldown);
        isOnAttackCooldown = false;
    }

    // ── Object Pooling System ──
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

    private IEnumerator ReturnProjectileToPool(GameObject projectile, float delay)
    {
        yield return new WaitForSeconds(delay);
        if (projectile != null && projectilePoolRoot != null)
        {
            projectile.SetActive(false);
            projectile.transform.SetParent(projectilePoolRoot);
            projectilePool.Enqueue(projectile);
        }
        else if (projectile != null)
        {
            Destroy(projectile);
        }
    }

    void FaceTarget(Vector3 targetPos)
    {
        Vector3 dir = targetPos - transform.position;
        dir.y = 0;
        if (dir == Vector3.zero) return;
        Quaternion look = Quaternion.LookRotation(dir.normalized);
        transform.rotation = Quaternion.Slerp(transform.rotation, look, Time.deltaTime * 5f);
    }

    void UpdateAnimator()
    {
        if (animator == null || agent == null) return;

        if (currentState == State.Retreat)
        {
            animator.SetFloat("Speed", -agent.velocity.magnitude);
        }
        else
        {
            animator.SetFloat("Speed", agent.velocity.magnitude);
        }
    }

   public void TakeDamage(int amount)
    {
        // Jika sudah mati, jangan proses damage lagi
        if (currentState == State.Dead) return;

        currentHealth -= amount;
        Debug.Log($"[EnemyAI] Kena hit! Sisa HP: {currentHealth}");

        // Memutar efek suara sakit acak
        if (hostileAudioSource != null && currentHealth > 0 && enemyHurtSounds != null && enemyHurtSounds.Length > 0)
        {
            AudioClip clip = enemyHurtSounds[Random.Range(0, enemyHurtSounds.Length)];
            if (clip != null) hostileAudioSource.PlayOneShot(clip);
        }

        // Jika HP di bawah atau sama dengan 0, panggil fungsi Die
        if (currentHealth <= 0) 
        {
            Die();
        }
    }

    void Die()
    {
        // Langsung kunci state ke Dead agar fungsi Update() berhenti memproses logic jalan/nembak
        currentState = State.Dead;
        
        Debug.LogWarning($"[EnemyAI] {gameObject.name} TELAH MATI! Memulai proses Destroy.");

        if (agent != null)
        {
            agent.isStopped = true;
            agent.enabled = false; // Matikan NavMeshAgent agar tidak bisa geser-geser lagi
        }

        if (projectilePoolRoot != null) Destroy(projectilePoolRoot.gameObject);

        // Matikan Collider agar peluru player tidak bisa menembak mayatnya lagi
        Collider col = GetComponent<Collider>();
        if (col != null) col.enabled = false;

        // --- PENGATURAN DELAY HANCUR ---
        // Jika kamu punya animasi mati, biarkan 3f - 5f. 
        // Jika TIDAK ADA animasi mati dan ingin musuh LANGSUNG HILANG, ubah angka 3f menjadi 0f
        float destroyDelay = 3f; 

        // Memutar suara kematian
        if (hostileAudioSource != null && enemyDeathSound != null)
        {
            hostileAudioSource.PlayOneShot(enemyDeathSound);
            destroyDelay = Mathf.Max(destroyDelay, enemyDeathSound.length);
        }

        if (animator != null) 
        {
            animator.SetTrigger("Die");
        }

        // Hancurkan GameObject musuh dari game
        Destroy(gameObject, destroyDelay);
    }

  
}