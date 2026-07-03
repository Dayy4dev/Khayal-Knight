using UnityEngine;
using System.Collections;

public class BalloonSword : Weapon
{
    private WeaponHitbox hitbox;

    [SerializeField] private WeaponData weaponData;

    private int maxComboCount;
    private float meleeReloadTime;
    private int currentComboLeft;
    private bool isReheating;
    private float remainingReheatTime;
    private int damage;

    private Coroutine reheatCoroutine;

    private void Awake()
    {
        hitbox = GetComponent<WeaponHitbox>();
        if (hitbox == null)
        {
            Debug.LogWarning("[BalloonSword] No WeaponHitbox component found!");
        }
    }

    private void Start()
    {
        if (weaponData != null)
        {
            maxComboCount = weaponData.maxComboCount;
            meleeReloadTime = weaponData.meleeReloadTime;
            damage = weaponData.damage;
        }
        currentComboLeft = maxComboCount;
    }

    // =========================================================================
    // PERBAIKAN: Masukkan fungsi OnEnable ini di BalloonSword.cs lu
    // =========================================================================
    private void OnEnable()
    {
        // Pas senjata dipegang lagi, cek apakah combo-nya lagi kosong
        // DAN sisa waktu rehatnya kemarin masih ada!
        if (currentComboLeft <= 0 && remainingReheatTime > 0f)
        {
            if (reheatCoroutine != null) StopCoroutine(reheatCoroutine);

            // PASTIKAN di dalam kurung ini isinya: remainingReheatTime (bukan remainingReloadTime!)
            reheatCoroutine = StartCoroutine(ReheatWithRemainingTime(remainingReheatTime));

            Debug.Log($"[BalloonSword] Rehat dilanjutkan. Sisa waktu: {remainingReheatTime:F1}s");
        }
        else if (currentComboLeft <= 0 && remainingReheatTime <= 0f)
        {
            // Antisipasi darurat jika waktu rehat di latar belakang sudah habis 
            // tapi combo belum ter-reset
            CompleteReheat();
        }
    }

    // =========================================================================
    // PERBAIKAN: Sesuaikan fungsi OnDisable lu agar seperti ini
    // =========================================================================
    private void OnDisable()
    {
        // 🛑 JANGAN direset remainingReheatTime ke 0f di sini!
        // Biarkan nilainya tetap tersimpan agar bisa dibaca pas OnEnable di atas.
        if (reheatCoroutine != null)
        {
            StopCoroutine(reheatCoroutine);
            reheatCoroutine = null;
        }

        // Matikan status rehat lokal saat masuk kantong agar tidak merusak visual hotbar
        isReheating = false;
    }

    public override bool CanAttack()
    {
        return !isReheating && currentComboLeft > 0;
    }


    public override void Attack()
    {
        if (!CanAttack() || hitbox == null) return;

        hitbox.ActivateHitbox();
        Invoke(nameof(DeactivateHitbox), 0.2f);

        BalloonSwordVFX vfx = GetComponent<BalloonSwordVFX>();
        if (vfx != null) vfx.PlayVFX();

        currentComboLeft--;

        if (currentComboLeft <= 0)
        {
            reheatCoroutine = StartCoroutine(ReheatCoroutine());
        }
    }

    private void DeactivateHitbox()
    {
        if (hitbox != null)
            hitbox.DeactivateHitbox();

        BalloonSwordVFX vfx = GetComponent<BalloonSwordVFX>();
        if (vfx != null) vfx.StopVFX();
    }

    public override void OnWeaponDeactivate()
    {
        if (isReheating && reheatCoroutine != null)
        {
            StopCoroutine(reheatCoroutine);
            reheatCoroutine = null;
            Debug.Log($"[BalloonSword] Rehat dipause. Sisa: {remainingReheatTime:F1}s");
        }
    }

  public override void OnWeaponActivate()
    {
        PlayerAttack playerAttack = GetComponentInParent<PlayerAttack>();

        if (playerAttack != null)
        {
            WeaponData dataAktif = playerAttack.GetCurrentWeaponData(); 
            
            if (dataAktif != null)
            {
                weaponData = dataAktif;
                damage = dataAktif.damage;
                Debug.Log($"[BalloonSword] Berhasil sinkronisasi data! Senjata: {weaponData.weaponName} | Damage: {damage}");
            }
        }
    }
    public int GetDamageValue()
    {
        return damage;
    }
    private IEnumerator ReheatCoroutine()
    {
        isReheating = true;
        remainingReheatTime = meleeReloadTime;
        Debug.Log("[BalloonSword] Senjata lelah, sedang rehat...");

        while (remainingReheatTime > 0f)
        {
            remainingReheatTime -= Time.deltaTime;
            yield return null;
        }

        CompleteReheat();
    }

    private IEnumerator ReheatWithRemainingTime(float remainingTime)
    {
        isReheating = true;
        remainingReheatTime = remainingTime;

        while (remainingReheatTime > 0f)
        {
            remainingReheatTime -= Time.deltaTime;
            yield return null;
        }

        CompleteReheat();
    }

    private void CompleteReheat()
    {
        currentComboLeft = maxComboCount;
        isReheating = false;
        reheatCoroutine = null;
        remainingReheatTime = 0f;
        Debug.Log("[BalloonSword] Rehat selesai! Siap menebas lagi.");
    }

    public override float GetCooldownPercentage()
    {
        if (isReheating && meleeReloadTime > 0f)
        {
            return remainingReheatTime / meleeReloadTime;
        }
        return 0f;
    }

}