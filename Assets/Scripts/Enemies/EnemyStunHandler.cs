using System.Collections;
using UnityEngine;
using UnityEngine.AI;

[AddComponentMenu("Enemy/Enemy Stun Handler")]
public class EnemyStunHandler : MonoBehaviour
{
    private NavMeshAgent navAgent;
    private Animator animator;
    
    // Properti status stun musuh yang bisa dicek oleh script AI lain
    public bool IsStunned { get; private set; }

    void Awake()
    {
        navAgent = GetComponent<NavMeshAgent>();
        animator = GetComponentInChildren<Animator>();
    }

    /// <summary>
    /// Memulai efek stun pada musuh dengan durasi tertentu
    /// </summary>
    public void TriggerStun(float duration)
    {
        if (IsStunned) 
            StopAllCoroutines(); // Reset durasi jika terkena hit combo beruntun sebelum pulih

        StartCoroutine(StunRoutine(duration));
    }

    private IEnumerator StunRoutine(float duration)
    {
        IsStunned = true;

        // 1. Hentikan NavMeshAgent agar tidak mengejar player
        if (navAgent != null && navAgent.isActiveAndEnabled && navAgent.isOnNavMesh)
        {
            navAgent.isStopped = true;
            navAgent.velocity = Vector3.zero; // Menggunakan properti bawaan standar Unity
        }

        // 2. Visual Feedback: Perlambat animasi musuh (Hit-stop effect)
        if (animator != null) 
            animator.speed = 0.1f; 

        yield return new WaitForSeconds(duration);

        // --- PROSES PEMULIHAN MUSUH ---
        if (animator != null) 
            animator.speed = 1f;

        if (navAgent != null && navAgent.isActiveAndEnabled && navAgent.isOnNavMesh)
        {
            navAgent.isStopped = false;
        }

        IsStunned = false;
    }
}