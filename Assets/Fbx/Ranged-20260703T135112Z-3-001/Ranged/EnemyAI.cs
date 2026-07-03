    using UnityEngine;
using UnityEngine.AI;

// Taruh script ini di GameObject karakter RANGED.
// Wajib ada komponen: NavMeshAgent, Animator, Collider.
[RequireComponent(typeof(NavMeshAgent))]
[RequireComponent(typeof(Animator))]
public class EnemyAI : MonoBehaviour
{
    public enum State { Chase, Retreat, AttackPlayer, AttackAirplane, Dead }
    public State currentState = State.Chase;

    [Header("References")]
    public Transform player;              // auto-cari via taag "Player" kalau kosong
    public Transform airplaneTarget;       // auto-cari via tag "Airplane" kalau kosong
    public GameObject projectilePrefab;
    public Transform firePoint;

    [Header("Chase Settings")]
    public float chaseSpeed = 3.5f;
    public float stopChaseDistance = 1.5f;   // jarak berhenti mendekat (nyaman menyerang)
    public float retreatDistance = 3f;       // kalau player lebih dekat dari ini -> mundur
    public float retreatStepDistance = 4f;   // seberapa jauh mundur tiap kali cari titik mundur
    public bool faceTargetWhileMoving = true; // hadap player terus walau lagi mundur (biar natural)

    [Header("Shoot Settings")]
    public float shootRange = 15f;
    public float fireRate = 1.5f;
    private float fireCooldown;

    [Header("Health")]
    public int maxHealth = 100;
    private int currentHealth;

    private NavMeshAgent agent;
    private Animator animator;

    void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        animator = GetComponent<Animator>();
        currentHealth = maxHealth;
    }

    void Start()
    {
        if (player == null)
        {
            var p = GameObject.FindGameObjectWithTag("Player");
            if (p != null) player = p.transform;
        }
        if (airplaneTarget == null)
        {
            var a = GameObject.FindGameObjectWithTag("Airplane");
            if (a != null) airplaneTarget = a.transform;
        }
        agent.speed = chaseSpeed;

        // Rotasi diatur manual (bukan oleh NavMeshAgent) supaya karakter
        // bisa tetap menghadap player walau sedang mundur.
        agent.updateRotation = !faceTargetWhileMoving;
    }

void Update()
{
    if (currentState == State.Dead) return;

    // Paksa cooldown berkurang setiap frame
    if (fireCooldown > 0)
    {
        fireCooldown -= Time.deltaTime;
    }

    // 1. Ambil jarak ke player
    float distToPlayer = player != null ? Vector3.Distance(transform.position, player.position) : float.MaxValue;

    // 2. PRIORITAS UTAMA: Interaksi dengan Player
    if (player != null && distToPlayer < retreatDistance)
    {
        Retreat();
    }
    else if (player != null && distToPlayer <= shootRange)
    {
        if (distToPlayer > stopChaseDistance)
        {
            ChasePlayer(distToPlayer);
        }
        else
        {
            // Sudah dalam jarak tembak & tidak terlalu dekat/jauh -> tembak player
            ShootPlayer();
        }
    }
    // 3. PRIORITAS KEDUA: Menyerang Pesawat jika Player jauh
    else if (airplaneTarget != null && Vector3.Distance(transform.position, airplaneTarget.position) <= shootRange)
    {
        AttackAirplane();
    }
    else
    {
        currentState = State.Chase;
        agent.isStopped = true; 
    }

    UpdateAnimator();
}

void AttackAirplane()
{
    currentState = State.AttackAirplane;
    agent.isStopped = true; 

    FaceTarget(airplaneTarget.position);

    // LOG UNTUK UTING: Cek apakah kode masuk ke fungsi serang pesawat
    Debug.Log($"Mencoba Menyerang Pesawat. Cooldown Saat Ini: {fireCooldown}");

    if (fireCooldown <= 0f)
    {
        Shoot();
        fireCooldown = fireRate; // Reset cooldown (misal 1.5 detik)
    }
}

void ShootPlayer()
{
    currentState = State.AttackPlayer;
    agent.isStopped = true;

    FaceTarget(player.position);

    Debug.Log($"Mencoba Menyerang Player. Cooldown Saat Ini: {fireCooldown}");

    if (fireCooldown <= 0f)
    {
        Shoot(player);
        fireCooldown = fireRate;
    }
}

void Shoot(Transform target = null)
{
    // LOG UNTUK UTING: Mengonfirmasi fungsi tembak C# berjalan
    Debug.LogWarning("KODE C# FIRING: MEMICU ANIMATOR SHOOT SEKARANG!");

    animator.SetTrigger("Shoot");

    if (projectilePrefab != null && firePoint != null)
    {
        GameObject proj = Instantiate(projectilePrefab, firePoint.position, firePoint.rotation);
        ProjectilePlane p = proj.GetComponent<ProjectilePlane>();
        // Kalau target eksplisit dikasih (misal player), pakai itu.
        // Kalau tidak, default ke airplaneTarget (dipakai AttackAirplane).
        if (p != null) p.SetTarget(target != null ? target : airplaneTarget);
    }
}

// HAPUS dua fungsi ChasePlayer() yang lama, dan gunakan satu fungsi ini saja:
void ChasePlayer(float dist)
{
    currentState = State.Chase;
    agent.isStopped = false; // Membuka kunci NavMesh Agent agar bisa jalan lagi
    agent.SetDestination(player.position);

    if (faceTargetWhileMoving) FaceTarget(player.position);
}


    void ChasePlayer()
    {
        float dist = Vector3.Distance(transform.position, player.position);

        if (dist < retreatDistance)
        {
            Retreat();
            return;
        }

        currentState = State.Chase;
        agent.isStopped = false;
        agent.SetDestination(player.position);
        agent.isStopped = dist <= stopChaseDistance;

        if (faceTargetWhileMoving) FaceTarget(player.position);
    }

    void Retreat()
    {
        currentState = State.Retreat;

        // Titik mundur: arah berlawanan dari player, dari posisi karakter sekarang
        Vector3 awayDir = (transform.position - player.position).normalized;
        Vector3 retreatPoint = transform.position + awayDir * retreatStepDistance;

        // Cari titik valid terdekat di NavMesh supaya tidak keluar area
        if (UnityEngine.AI.NavMesh.SamplePosition(retreatPoint, out var hit, retreatStepDistance, UnityEngine.AI.NavMesh.AllAreas))
        {
            agent.isStopped = false;
            agent.SetDestination(hit.position);
        }
        else
        {
            agent.isStopped = true;
        }

        if (faceTargetWhileMoving) FaceTarget(player.position);
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
    if (currentState == State.Retreat)
    {
        // Jika sedang mundur, kirim nilai negatif ke parameter Speed
        animator.SetFloat("Speed", -agent.velocity.magnitude);
    }
    else
    {
        // Jika maju atau idle, kirim nilai positif seperti biasa
        animator.SetFloat("Speed", agent.velocity.magnitude);
    }
}

    public void TakeDamage(int amount)
    {
        if (currentState == State.Dead) return;

        currentHealth -= amount;
        if (currentHealth <= 0) Die();
    }

    void Die()
    {
        currentState = State.Dead;
        agent.isStopped = true;
        agent.enabled = false;

        var col = GetComponent<Collider>();
        if (col != null) col.enabled = false;

        animator.SetTrigger("Die");

        // Hapus objek setelah animasi mati selesai (sesuaikan durasinya)
        Destroy(gameObject, 5f);
    }
}
