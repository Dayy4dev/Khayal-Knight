using UnityEngine;
using System;

[AddComponentMenu("Player/Player Attack")]
public class PlayerAttack : MonoBehaviour
{
    [Header("Combo Settings")]
    [Tooltip("Jeda waktu maksimal (detik) antar hit agar combo tidak ter-reset")]
    [SerializeField] private float comboResetDuration = 1.0f;

    [Header("Knockback Settings")]
    [SerializeField] private bool enableUnarmedKnockback = true;
    [SerializeField] private bool applyVerticalForceToUnarmed = false;
    [SerializeField] private bool enableSwordKnockback = false;
    [SerializeField] private bool enableHammerKnockback = true;

    [Header("References")]
    [SerializeField] private PlayerHealth playerHealth;

    private int currentComboHit = 0;
    private float lastAttackTime = 0f;

    private WeaponData currentWeaponData;
    private Weapon activeWeaponComponent;
    private WeaponHitbox weaponHitbox;
    private int permanentDamageBuff = 0;

    public event Action<string> OnWeaponEquipped;

    public event Action<int> OnAttackExecuted;

    public event Action OnHeavyImpactExecuted;

    void Awake()
    {
        if (playerHealth == null)
            playerHealth = GetComponent<PlayerHealth>();
    }

    public void EquipWeapon(WeaponData data, Weapon weaponComponent)
    {
        currentWeaponData = data;
        activeWeaponComponent = weaponComponent;
        ResetCombo();

        string category = GetWeaponCategory(data);
        OnWeaponEquipped?.Invoke(category);
    }

    public void SetWeaponHitbox(WeaponHitbox hitbox)
    {
        weaponHitbox = hitbox;
    }

    public void ApplyPermanentDamageBuff(int amount)
    {
        permanentDamageBuff += amount;
    }

    public Weapon GetActiveWeapon()
    {
        return activeWeaponComponent;
    }

    public void ResetCombo()
    {
        currentComboHit = 0;
    }

    public int GetCurrentComboHit()
    {
        return currentComboHit;
    }

    void Update()
    {
        // Jika game sedang dipause, jangan panggil serangan
        if (GameManager.Instance != null && !GameManager.Instance.IsPlaying) return;

        if (Input.GetMouseButtonDown(0)) 
        {
            // 1. Ambil komponen senjata yang sedang aktif saat ini
            Weapon activeWeapon = GetActiveWeapon(); 

            // 2. Cek apakah senjata itu adalah HandCannon
            if (activeWeapon is HandCannon handCannon)
            {
                // 3. Panggil fungsi pengaman CanShoot()
                if (!handCannon.CanShoot())
                {
                    Debug.Log("[PlayerAttack] Serangan dibatalkan karena player tidak menahan klik kanan!");
                    return; 
                }
            }

            // Jika lolos semua pengaman, eksekusi serangan
            ExecuteAttack();
        }
    }

    private void ExecuteAttack()
    {
        if (playerHealth != null && playerHealth.IsDead) return;

        // 1. Cek apakah senjata siap dipakai (tidak sedang reload/rehat)
        if (activeWeaponComponent != null && !activeWeaponComponent.CanAttack()) return;

        // 2. CEK COMBO COOLDOWN — jangan advance combo jika animasi sebelumnya
        //    masih dalam cooldown (mencegah combo skip)
        MCAnimationController animCtrl = FindFirstObjectByType<MCAnimationController>();
        if (animCtrl != null && animCtrl.IsInComboCooldown) return;

        // 3. ATUR COMBO TERLEBIH DAHULU SEBELUM HITBOX SENJATA AKTIF
        float timeSinceLastAttack = Time.time - lastAttackTime;
        if (timeSinceLastAttack > comboResetDuration)
        {
            currentComboHit = 1; // Mulai combo baru (Hit pertama)
        }
        else
        {
            int maxCombo = (currentWeaponData != null) ? currentWeaponData.maxComboCount : 3;
            currentComboHit = (currentComboHit % maxCombo) + 1;
        }

        lastAttackTime = Time.time;

        string weaponName = (currentWeaponData != null) ? currentWeaponData.weaponName : "Tangan Kosong";
        Debug.Log($"[Attack Melee] Mengayunkan {weaponName} | Combo Hit: {currentComboHit}");

        // Notify animation system — fires for EVERY click, even when combo value wraps
        OnAttackExecuted?.Invoke(currentComboHit);

        // 4. JALANKAN LOGIKA SERANGAN SENJATA
        if (activeWeaponComponent != null)
        {
            activeWeaponComponent.Attack(); 
        }

        // 5. AKTIFKAN HITBOX SENJATA PALING TERAKHIR AGAR MEMBACA COMBO YANG SUDAH UP-TO-DATE
        if (weaponHitbox != null)
        {
            weaponHitbox.ActivateHitbox();
        }
    }

    public void CalculateHitEffects(out int finalDamage, out float stunDuration)
    {
        // --- PENGAMAN UTAMA: Jika data senjata null, langsung hitung Tangan Kosong + Buff ---
        if (currentWeaponData == null)
        {
            finalDamage = 10 + permanentDamageBuff; 
            stunDuration = 0.3f;
            Debug.LogWarning("[PlayerAttack] currentWeaponData null! Menggunakan rumus Tangan Kosong + Buff.");
            return;
        }

        // Ambil kategori secara aman
        string category = GetWeaponCategory(currentWeaponData);

        // --- RUMUS MELEE UNTUK CANNON/HANDCANNON ---
        if (category == "Cannon")
        {
            finalDamage = currentWeaponData.damage + permanentDamageBuff;
            stunDuration = 0f; 
            return;
        }

        // --- RUMUS MELEE UNTUK SWORD (BALLOONSWORD) & HAMMER ---
        int baseTotal = currentWeaponData.damage + permanentDamageBuff;
        finalDamage = baseTotal;
        stunDuration = 0.3f;

        if (category == "Sword" && currentComboHit == 3)
        {
            finalDamage = Mathf.RoundToInt(baseTotal * 1.20f);
        }
        else if (category == "Hammer" && currentComboHit == 2)
        {
            finalDamage = Mathf.RoundToInt(baseTotal * 1.25f);
            stunDuration = 0.8f;
        }
        else if (category == "Hammer" && currentComboHit == 3)
        {
            finalDamage = Mathf.RoundToInt(baseTotal * 1.50f);
            stunDuration = 1.0f;
        }
    }
    
    public int GetPermanentDamageBuff()
    {
        return permanentDamageBuff;
    }

    private string GetWeaponCategory(WeaponData data)
    {
        if (data == null || string.IsNullOrEmpty(data.weaponName)) return "Unarmed";
        string nameLower = data.weaponName.ToLower();

        if (nameLower.Contains("sword") || nameLower.Contains("blade") || nameLower.Contains("calibur"))
            return "Sword";
        if (nameLower.Contains("hammer") || nameLower.Contains("mallet"))
            return "Hammer";
        if (nameLower.Contains("cannon") || nameLower.Contains("blaster"))
            return "Cannon";
        if (nameLower.Contains("unarmed") || nameLower.Contains("punch") || nameLower.Contains("fist"))
            return "Unarmed";

        return "Unarmed";
    }

    public WeaponData GetCurrentWeaponData()
    {
        return currentWeaponData; 
    }

    public void ApplyKnockback(GameObject target, float verticalForce = 0f)
    {
        if (target == null) return;

        string category = GetWeaponCategory(currentWeaponData);
        
        if (category == "Sword" && !enableSwordKnockback) return;
        if (category == "Hammer" && !enableHammerKnockback) return;
        if (category == "Unarmed" && !enableUnarmedKnockback) return;

        if (category == "Unarmed" && !applyVerticalForceToUnarmed)
        {
            verticalForce = 0f;
        }

        float force = 5f;
        if (category == "Hammer") force = 8f; 

        // Try NavMesh-aware knockback first
        if (target.TryGetComponent<EnemyKnockback>(out var enemyKb))
        {
            if (verticalForce > 0f)
                enemyKb.ApplyHeavyKnockback(transform.position, force, verticalForce);
            else
                enemyKb.ApplyKnockback(transform.position, force);
            return;
        }

        // Fallback for non-enemies (e.g. breakable props with Rigidbody)
        Rigidbody targetRb = target.GetComponent<Rigidbody>();
        if (targetRb != null)
        {
            Vector3 knockbackDirection = (target.transform.position - transform.position);
            knockbackDirection.y = 0; 
            if (knockbackDirection.sqrMagnitude < 0.001f) knockbackDirection = transform.forward;
            knockbackDirection.Normalize();

            targetRb.AddForce(knockbackDirection * force, ForceMode.VelocityChange);

            if (verticalForce > 0f)
            {
                targetRb.AddForce(Vector3.up * verticalForce, ForceMode.VelocityChange);
            }
        }
    }

    public void FireHeavyImpactEvent()
    {
        OnHeavyImpactExecuted?.Invoke();
    }
}