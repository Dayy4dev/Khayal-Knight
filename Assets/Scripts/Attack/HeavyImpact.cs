using UnityEngine;
using System.Collections.Generic;

[AddComponentMenu("Combat/Heavy Impact")]
public class HeavyImpact : MonoBehaviour
{
    [Header("Impact Area")]
    [SerializeField] private float damageRadius   = 3f;
    [SerializeField] private float impactRadius   = 3f;
    [SerializeField] private LayerMask impactLayerMask = ~0;

    [Header("Knockback")]
    [SerializeField] private float horizontalForce = 12f;
    [SerializeField] private float verticalForce   = 8f;
    [SerializeField] private bool  forceFalloff    = true;

    [Header("Stun")]
    [SerializeField] private float stunDuration    = 0.8f;

    [Header("Toggles")]
    [SerializeField] private bool enableCameraShake   = true;
    [SerializeField] private bool enableKnockback     = true;
    [SerializeField] private bool enableEnemyLaunch   = true;
    [SerializeField] private bool enableImpactVFX     = true;

    [Header("Camera Shake")]
    [SerializeField] private float cameraShakeDuration  = 0.35f;
    [SerializeField] private float cameraShakeAmplitude = 0.25f;
    [SerializeField] private float cameraShakeFrequency = 30f;

    [Header("Impact VFX")]
    [SerializeField] private GameObject impactVFXPrefab;
    [SerializeField] private Vector3 impactVFXOffset = new Vector3(0f, 0.1f, 1f);
    [SerializeField] private Vector3 impactVFXScale  = new Vector3(2f, 2f, 2f);
    [SerializeField] private float   vfxLifetime     = 0.7f;

    private HashSet<int> hitThisFrame = new HashSet<int>();

    private PlayerAttack playerAttack;

    void Awake()
    {
        playerAttack = GetComponentInParent<PlayerAttack>();
        if (playerAttack == null)
            playerAttack = FindFirstObjectByType<PlayerAttack>();
    }

    public void ExecuteImpact(System.Collections.Generic.HashSet<int> excludeIds = null)
    {
        hitThisFrame.Clear();

        Vector3 impactOrigin = transform.TransformPoint(impactVFXOffset);

        // 1. Area Damage 
        Collider[] hits = Physics.OverlapSphere(impactOrigin, damageRadius, impactLayerMask);

        foreach (var col in hits)
        {
            if (col == null) continue;
            if (col.CompareTag("Player")) continue;

            int instanceId = col.GetInstanceID();
            if (excludeIds != null && excludeIds.Contains(instanceId)) continue; // already hit by hitbox
            if (!hitThisFrame.Add(instanceId)) continue; // already processed

            // Damage
            IDamageable damageable = col.GetComponent<IDamageable>();
            if (damageable != null)
            {
                int finalDamage = 10;
                float stun = stunDuration;

                if (playerAttack != null)
                {
                    playerAttack.CalculateHitEffects(out finalDamage, out stun);
                }

                damageable.TakeDamage(finalDamage);
                Debug.Log($"[HeavyImpact] Hit {col.name} for {finalDamage} damage");

                // Stun
                if (col.TryGetComponent<EnemyStunHandler>(out var stunHandler))
                {
                    stunHandler.TriggerStun(stun > 0f ? stun : stunDuration);
                }
            }

            // Knockback + Launch
            if (enableKnockback || enableEnemyLaunch)
            {
                // First try the new EnemyKnockback system (NavMesh-aware)
                if (col.TryGetComponent<EnemyKnockback>(out var enemyKb))
                {
                    float dist = Vector3.Distance(col.transform.position, impactOrigin);
                    float falloff = 1f;
                    if (forceFalloff && impactRadius > 0f)
                        falloff = Mathf.Clamp01(1f - (dist / impactRadius));
                        
                    float hForce = enableKnockback ? horizontalForce * falloff : 0f;
                    float vForce = enableEnemyLaunch ? verticalForce * falloff : 0f;
                    
                    enemyKb.ApplyHeavyKnockback(impactOrigin, hForce, vForce);
                }
                else
                {
                    // Fallback to raw Rigidbody (for non-enemy objects like props)
                    Rigidbody rb = col.attachedRigidbody;
                    if (rb == null) rb = col.GetComponent<Rigidbody>();
                    if (rb != null)
                    {
                        Vector3 dir = (col.transform.position - impactOrigin);
                        dir.y = 0f;
                        float dist = dir.magnitude;
                        dir.Normalize();

                        float falloff = 1f;
                        if (forceFalloff && impactRadius > 0f)
                            falloff = Mathf.Clamp01(1f - (dist / impactRadius));

                        if (enableKnockback)
                        {
                            rb.AddForce(dir * horizontalForce * falloff, ForceMode.VelocityChange);
                        }

                        if (enableEnemyLaunch)
                        {
                            rb.AddForce(Vector3.up * verticalForce * falloff, ForceMode.VelocityChange);
                        }
                    }
                }
            }
        }

        //  2. Camera Shake 
        if (enableCameraShake && CameraShake.Instance != null)
        {
            CameraShake.Instance.Shake(cameraShakeDuration, cameraShakeAmplitude, cameraShakeFrequency);
        }

        //  3. Impact VFX
        if (enableImpactVFX && impactVFXPrefab != null)
        {
            GameObject vfx = Instantiate(impactVFXPrefab, impactOrigin, Quaternion.identity);
            vfx.transform.localScale = impactVFXScale;

            // Face the camera
            if (Camera.main != null)
            {
                Vector3 toCamera = Camera.main.transform.position - vfx.transform.position;
                vfx.transform.rotation = Quaternion.LookRotation(-toCamera);
            }

            // Restart animator from frame 0
            Animator animator = vfx.GetComponent<Animator>();
            if (animator != null)
            {
                animator.Play("vfx_boom", 0, 0f);
            }

            Destroy(vfx, vfxLifetime);
        }

        //  4. Notify other systems
        if (playerAttack != null)
        {
            playerAttack.FireHeavyImpactEvent();
        }

        Debug.Log($"[HeavyImpact] Impact executed at {impactOrigin} | Enemies hit: {hitThisFrame.Count}");
    }

    void OnDrawGizmosSelected()
    {
        Vector3 origin = transform.TransformPoint(impactVFXOffset);

        Gizmos.color = new Color(1f, 0.3f, 0f, 0.25f);
        Gizmos.DrawWireSphere(origin, damageRadius);

        Gizmos.color = new Color(1f, 0.6f, 0f, 0.15f);
        Gizmos.DrawWireSphere(origin, impactRadius);
    }
}
