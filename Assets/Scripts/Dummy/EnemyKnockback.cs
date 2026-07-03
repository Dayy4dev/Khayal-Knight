using System.Collections;
using UnityEngine;
using UnityEngine.AI;

[AddComponentMenu("Enemy/Enemy Knockback")]
public class EnemyKnockback : MonoBehaviour, IKnockbackable
{

    [Header("Knockback")]
    [Tooltip("Enable horizontal knockback on normal hits.")]
    [SerializeField] private bool enableKnockback = true;

    [Tooltip("Horizontal impulse force applied during normal knockback.")]
    [SerializeField] private float horizontalForce = 10f;

    [Tooltip("How long the knockback force is actively applied (seconds).")]
    [SerializeField] private float forceDuration = 0.4f;

    [Tooltip("If true, ForceMode.VelocityChange is used (ignores Rigidbody mass).")]
    [SerializeField] private bool ignoreMass = true;

    [Tooltip("Layer mask for filtering what this enemy can hit/interact with.")]
    [SerializeField] private LayerMask hitLayerMask = ~0;

    [Header("Launch (Heavy Impact Only)")]
    [Tooltip("Enable vertical launch. Only triggered by HeavyImpact, not normal hits.")]
    [SerializeField] private bool enableLaunch = true;

    [Tooltip("Upward impulse force for Heavy Impact launch.")]
    [SerializeField] private float verticalForce = 8f;

    [Tooltip("Optional multiplier applied to gravity while airborne. >1 = heavier fall.")]
    [SerializeField] private float gravityMultiplier = 1.0f;

    [Tooltip("Delay in seconds before the vertical launch force is applied (syncs to animation).")]
    [SerializeField] private float launchDelay = 0.05f;

    [Header("Recovery")]
    [Tooltip("Extra seconds to wait after ground detection before re-enabling NavMesh.")]
    [SerializeField] private float recoveryDelay = 0.15f;

    [Tooltip("Safety timeout: force recovery if enemy is airborne longer than this (seconds).")]
    [SerializeField] private float maxAirTime = 4f;

    [Header("Ground Detection")]
    [Tooltip("Layer(s) considered as ground for landing detection.")]
    [SerializeField] private LayerMask groundLayer;

    [Tooltip("Radius of the sphere used for ground SphereCast.")]
    [SerializeField] private float groundCheckRadius = 0.25f;

    [Tooltip("How far below the enemy's feet the ground cast reaches.")]
    [SerializeField] private float groundCheckDistance = 0.3f;

    private NavMeshAgent navAgent;
    private Rigidbody rb;
    private Collider col;
    private Animator animator;

    private bool isKnockbackActive = false;
    private Coroutine knockbackCoroutine;
    private Vector3 preKnockbackPosition;

    public bool IsInKnockback => isKnockbackActive;

    private void Awake()
    {
        navAgent = GetComponent<NavMeshAgent>();
        rb = GetComponent<Rigidbody>();
        col = GetComponent<Collider>();
        animator = GetComponentInChildren<Animator>();

        if (rb == null)
            Debug.LogWarning($"[EnemyKnockback] {gameObject.name}: No Rigidbody found! Knockback will not work.");

        if (navAgent == null)
            Debug.LogWarning($"[EnemyKnockback] {gameObject.name}: No NavMeshAgent found.");

        // Start with Rigidbody kinematic - NavMeshAgent drives movement normally.
        if (rb != null)
        {
            rb.isKinematic = true;
            rb.useGravity = false;
        }
    }

    public void TakeKnockback(Vector3 direction, float force, float duration)
    {
        if (!enableKnockback) return;

        Vector3 flatDir = direction;
        flatDir.y = 0f;
        if (flatDir.sqrMagnitude < 0.001f) flatDir = transform.forward;
        flatDir.Normalize();

        float f = force > 0f ? force : horizontalForce;
        float d = duration > 0f ? duration : forceDuration;

        StopKnockback();
        knockbackCoroutine = StartCoroutine(KnockbackRoutine(flatDir, f, 0f, d, false));
    }

    public void ApplyKnockback(Vector3 impactOrigin, float hForce = 0f, float hDuration = 0f)
    {
        if (!enableKnockback) return;

        Vector3 dir = (transform.position - impactOrigin);
        dir.y = 0f;
        if (dir.sqrMagnitude < 0.001f) dir = transform.forward;
        dir.Normalize();

        float f = hForce > 0f ? hForce : horizontalForce;
        float d = hDuration > 0f ? hDuration : forceDuration;

        StopKnockback();
        knockbackCoroutine = StartCoroutine(KnockbackRoutine(dir, f, 0f, d, false));
    }

    public void ApplyHeavyKnockback(Vector3 impactOrigin,
                                     float hForce = 0f,
                                     float vForce = 0f,
                                     float hDuration = 0f,
                                     float overrideLaunchDelay = -1f)
    {
        Vector3 dir = (transform.position - impactOrigin);
        dir.y = 0f;
        if (dir.sqrMagnitude < 0.001f) dir = transform.forward;
        dir.Normalize();

        float f = hForce > 0f ? hForce : horizontalForce;
        float vf = vForce > 0f ? vForce : verticalForce;
        float d = hDuration > 0f ? hDuration : forceDuration;
        float ld = overrideLaunchDelay >= 0f ? overrideLaunchDelay : launchDelay;

        bool doLaunch = enableLaunch && vf > 0f;

        StopKnockback();
        knockbackCoroutine = StartCoroutine(KnockbackRoutine(dir, f, doLaunch ? vf : 0f, d, doLaunch, ld));
    }

    private IEnumerator KnockbackRoutine(Vector3 dir,
                                          float hForce,
                                          float vForce,
                                          float activeDuration,
                                          bool isLaunch,
                                          float lDelay = 0f)
    {
        if (rb == null) yield break;

        isKnockbackActive = true;

        // Store pre-knockback position as a safe fallback for NavMesh recovery
        preKnockbackPosition = transform.position;

        //  1. DISABLE NavMeshAgent 
        bool agentWasEnabled = false;
        if (navAgent != null && navAgent.isActiveAndEnabled)
        {
            agentWasEnabled = true;
            navAgent.ResetPath();
            navAgent.velocity = Vector3.zero;
            navAgent.enabled = false;
        }

        //  2. ENABLE Rigidbody physics 
        // Lift slightly above ground to prevent collider penetration
        float groundLift = 0.05f;
        transform.position += Vector3.up * groundLift;

        rb.isKinematic = false;
        rb.useGravity = true;
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;

        //  3. Freeze animator during knockback 
        if (animator != null) animator.speed = 0f;

        //  4. Apply horizontal impulse immediately 
        ForceMode forceMode = ignoreMass ? ForceMode.VelocityChange : ForceMode.Impulse;
        if (enableKnockback && hForce > 0f)
            rb.AddForce(dir * hForce, forceMode);

        // Small upward nudge to counteract first-frame gravity and prevent ground sink
        rb.linearVelocity += Vector3.up * 1.5f;

        //  5. Apply vertical impulse (launch) after optional delay 
        if (isLaunch && vForce > 0f)
        {
            if (lDelay > 0f)
                yield return new WaitForSeconds(lDelay);

            if (rb != null && !rb.isKinematic)
            {
                rb.AddForce(Vector3.up * vForce, forceMode);
                Debug.Log($"[EnemyKnockback] {gameObject.name} launched! hForce={hForce} vForce={vForce}");
            }
        }

        //  6. Wait for active force phase -
        yield return new WaitForSeconds(activeDuration);

        //  7. GROUND DETECTION - wait until landing -
        if (isLaunch)
        {
            float airTimer = 0f;
            bool landed = false;

            yield return new WaitForSeconds(0.2f); // ensure enemy has left ground

            while (!landed && airTimer < maxAirTime)
            {
                if (gravityMultiplier > 1f && rb != null && !rb.isKinematic)
                    rb.AddForce(Physics.gravity * (gravityMultiplier - 1f), ForceMode.Acceleration);

                landed = IsGrounded();
                airTimer += Time.deltaTime;
                yield return null;
            }

            if (!landed)
                Debug.LogWarning($"[EnemyKnockback] {gameObject.name} ground timeout after {maxAirTime}s - forcing recovery.");
        }

        //  8. Short recovery pause 
        yield return new WaitForSeconds(recoveryDelay);

        //  9. RE-ENABLE NavMeshAgent with Warp to sync position
        rb.collisionDetectionMode = CollisionDetectionMode.Discrete;

        if (agentWasEnabled)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.isKinematic = true;
            rb.useGravity = false;

            navAgent.enabled = true;

            if (navAgent.isOnNavMesh)
            {
                navAgent.Warp(transform.position);
            }
            else
            {
                NavMeshHit hit;
                // Try increasingly larger radii to find the NavMesh
                if (NavMesh.SamplePosition(transform.position, out hit, 10f, NavMesh.AllAreas))
                {
                    navAgent.Warp(hit.position);
                    Debug.Log($"[EnemyKnockback] {gameObject.name} warped to nearest NavMesh point.");
                }
                else if (NavMesh.SamplePosition(preKnockbackPosition, out hit, 5f, NavMesh.AllAreas))
                {
                    // Fallback: warp back to where the enemy was before knockback
                    transform.position = hit.position;
                    navAgent.Warp(hit.position);
                    Debug.Log($"[EnemyKnockback] {gameObject.name} warped back to pre-knockback position.");
                }
                else
                {
                    // Last resort: force warp to pre-knockback position even if not on NavMesh
                    transform.position = preKnockbackPosition;
                    navAgent.Warp(preKnockbackPosition);
                    Debug.LogWarning($"[EnemyKnockback] {gameObject.name} force-warped to pre-knockback position (no NavMesh found).");
                }
            }
        }
        else
        {
            if (rb != null)
            {
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
                rb.isKinematic = true;
                rb.useGravity = false;
            }
        }

        //  10. Resume animator 
        if (animator != null) animator.speed = 1f;

        isKnockbackActive = false;
        knockbackCoroutine = null;
        Debug.Log($"[EnemyKnockback] {gameObject.name} recovered - NavMesh resumed.");
    }

    // Ground Detection

    private bool IsGrounded()
    {
        if (col == null) return true;
        Vector3 origin = GetColliderBottom() + Vector3.up * (groundCheckRadius + 0.05f);
        return Physics.SphereCast(origin, groundCheckRadius, Vector3.down, out _,
            groundCheckDistance + groundCheckRadius, groundLayer, QueryTriggerInteraction.Ignore);
    }

    private Vector3 GetColliderBottom()
    {
        if (col is CapsuleCollider cap)
        {
            Vector3 center = transform.TransformPoint(cap.center);
            return center - Vector3.up * (cap.height * 0.5f * transform.lossyScale.y);
        }
        return col.bounds.min;
    }

    // Interruption

    public void StopKnockback()
    {
        if (knockbackCoroutine != null)
        {
            StopCoroutine(knockbackCoroutine);
            knockbackCoroutine = null;
        }

        if (isKnockbackActive)
        {
            if (rb != null)
            {
                rb.collisionDetectionMode = CollisionDetectionMode.Discrete;
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
                rb.isKinematic = true;
                rb.useGravity = false;
            }

            if (navAgent != null && !navAgent.enabled)
            {
                navAgent.enabled = true;
                if (navAgent.isOnNavMesh)
                {
                    navAgent.Warp(transform.position);
                }
                else
                {
                    NavMeshHit hit;
                    if (NavMesh.SamplePosition(transform.position, out hit, 10f, NavMesh.AllAreas))
                    {
                        navAgent.Warp(hit.position);
                    }
                    else
                    {
                        // Fallback: warp back to pre-knockback position
                        transform.position = preKnockbackPosition;
                        navAgent.Warp(preKnockbackPosition);
                    }
                }
            }

            if (animator != null) animator.speed = 1f;
            isKnockbackActive = false;
        }
    }

    private void OnDisable()
    {
        StopKnockback();
    }
}