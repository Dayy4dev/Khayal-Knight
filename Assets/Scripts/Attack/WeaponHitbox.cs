using UnityEngine;
using System;
using System.Collections;

public class WeaponHitbox : MonoBehaviour
{
    public event Action<GameObject> OnEnemyHit;

    private Collider hitboxCollider;

    void Awake()
    {
        hitboxCollider = GetComponent<Collider>();

        if (hitboxCollider != null)
        {
            hitboxCollider.isTrigger = true;
            hitboxCollider.enabled = false;
        }
        else
        {
            Debug.LogError($"[WeaponHitbox] No Collider found on '{gameObject.name}'. " +
                "OnTriggerEnter requires the collider to be on the same GameObject.");
        }
    }

    public void ActivateHitbox()
    {
        StopAllCoroutines();
        StartCoroutine(HitboxRoutine());
    }

    public void DeactivateHitbox()
    {
        if (hitboxCollider != null)
        {
            hitboxCollider.enabled = false;
        }
    }

    private IEnumerator HitboxRoutine()
    {
        if (hitboxCollider != null) hitboxCollider.enabled = true;
        yield return new WaitForSeconds(0.2f);
        if (hitboxCollider != null) hitboxCollider.enabled = false;
    }

private void OnTriggerEnter(Collider other)
    {
        // Cegah melukai diri sendiri atau objek sesama Player
        if (other.CompareTag("Player")) return;

        IDamageable enemyHealth = other.GetComponent<IDamageable>();

        if (enemyHealth != null)
        {
            int damageYangDiberikan = 0;

            // --- PERBAIKAN UTAMA: Mencari PlayerAttack dari root objek tertinggi (Player) ---
            PlayerAttack playerAttack = transform.root.GetComponentInChildren<PlayerAttack>();

            if (playerAttack != null)
            {
                // Panggil kalkulasi terpusat (Base Damage + Buff Damage + Combo Multiplier)
                playerAttack.CalculateHitEffects(out damageYangDiberikan, out _);
            }
            else
            {
                // Backup darurat jika PlayerAttack benar-benar tidak ada di dunia
                BalloonSword sword = GetComponentInParent<BalloonSword>();
                if (sword != null)
                {
                    damageYangDiberikan = sword.GetDamageValue();
                }
                else
                {
                    ToyHammer hammer = GetComponentInParent<ToyHammer>();
                    if (hammer != null)
                    {
                        // Anda bisa menambahkan fungsi GetDamageValue() di ToyHammer jika dibutuhkan
                        damageYangDiberikan = 35; 
                    }
                }
            }

            // Pengaman akhir: Jika damage masih 0 atau minus, beri nilai default 10
            if (damageYangDiberikan <= 0) damageYangDiberikan = 10;

            // 1. Berikan damage hasil kalkulasi buff ke musuh!
            enemyHealth.TakeDamage(damageYangDiberikan);
            Debug.Log($"[Hitbox Melee] Berhasil memukul {other.name} | Total Damage + Buff: {damageYangDiberikan}");

            // Notify listeners (e.g. ToyHammer for HeavyImpact dedup)
            OnEnemyHit?.Invoke(other.gameObject);

            // 2. Berikan efek dorongan (Knockback)
            if (playerAttack != null)
            {
                playerAttack.ApplyKnockback(other.gameObject);
            }

            // 3. Berikan efek Stun jika ada komponen handler stun di musuh
            if (other.TryGetComponent<EnemyStunHandler>(out var stunHandler))
            {
                stunHandler.TriggerStun(0.3f); 
            }
        }
    }
}