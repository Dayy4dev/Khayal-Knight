using UnityEngine;
using UnityEngine.AI;

// Attach ke setiap prefab enemy (MeleeEnemy, HostileAI, EnemyDummy).
// Script ini auto-scale stat enemy saat di-spawn berdasarkan floor saat ini.

[RequireComponent(typeof(IDamageable))]
public class EnemyScaling : MonoBehaviour
{
    [Header("Base Stats (diisi otomatis dari komponen enemy)")]
    [SerializeField] private int baseHP = 0;         // 0 = ambil dari komponen enemy
    [SerializeField] private int baseDamage = 0;     // 0 = ambil dari komponen enemy
    [SerializeField] private float baseSpeed = 0f;   // 0 = ambil dari NavMeshAgent

    void Start()
    {
        ApplyScaling();
    }

    void ApplyScaling()
    {
        if (FloorManager.Instance == null)
        {
            Debug.LogWarning("[EnemyScaling] FloorManager tidak ditemukan!");
            return;
        }

        float hpMult     = FloorManager.Instance.GetHPMultiplier();
        float dmgMult    = FloorManager.Instance.GetDamageMultiplier();
        float speedMult  = FloorManager.Instance.GetSpeedMultiplier();
        int   floor      = FloorManager.Instance.CurrentFloor;

        // ── Scale MeleeEnemy ─────────────────────────────────────────────────
        MeleeEnemy melee = GetComponent<MeleeEnemy>();
        if (melee != null)
        {
            int   baseH = baseHP     > 0 ? baseHP     : melee.GetBaseHP();
            int   baseD = baseDamage > 0 ? baseDamage : melee.GetBaseDamage();
            float baseS = baseSpeed  > 0 ? baseSpeed  : melee.GetBaseSpeed();

            melee.SetScaledStats(
                Mathf.RoundToInt(baseH * hpMult),
                Mathf.RoundToInt(baseD * dmgMult),
                baseS * speedMult
            );

            Debug.Log($"[EnemyScaling] MeleeEnemy floor {floor}: HP={Mathf.RoundToInt(baseH * hpMult)}, DMG={Mathf.RoundToInt(baseD * dmgMult)}, SPD={baseS * speedMult:F1}");
            return;
        }

        // ── Scale HostileAI (Ranged) ─────────────────────────────────────────
        HostileAI ranged = GetComponent<HostileAI>();
        if (ranged != null)
        {
            int   baseH = baseHP     > 0 ? baseHP     : ranged.GetBaseHP();
            int   baseD = baseDamage > 0 ? baseDamage : ranged.GetBaseDamage();
            float baseS = baseSpeed  > 0 ? baseSpeed  : ranged.GetBaseSpeed();

            ranged.SetScaledStats(
                Mathf.RoundToInt(baseH * hpMult),
                Mathf.RoundToInt(baseD * dmgMult),
                baseS * speedMult
            );

            Debug.Log($"[EnemyScaling] HostileAI floor {floor}: HP={Mathf.RoundToInt(baseH * hpMult)}, DMG={Mathf.RoundToInt(baseD * dmgMult)}, SPD={baseS * speedMult:F1}");
            return;
        }

        // ── Scale EnemyDummy ─────────────────────────────────────────────────
        EnemyDummy dummy = GetComponent<EnemyDummy>();
        if (dummy != null)
        {
            int baseH = baseHP > 0 ? baseHP : dummy.maxHealth;
            dummy.maxHealth = Mathf.RoundToInt(baseH * hpMult);

            Debug.Log($"[EnemyScaling] EnemyDummy floor {floor}: HP={dummy.maxHealth}");
        }
    }
}