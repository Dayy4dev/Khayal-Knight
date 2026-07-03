using UnityEngine;
using System.Collections;
using UnityEngine.Pool;

public class HandCannon : Weapon
{
    [Header("Weapon Settings")]
    [SerializeField] private Bullet bulletPrefab;
    [SerializeField] private Transform firePoint;

    [Header("References")]
    [SerializeField] private WeaponData weaponData;
    [SerializeField] private PlayerMovement playerMovement;

    // Internal state
    private int magazineSize;
    private float reloadTime;
    private float fireRate;
    private int currentBullet;
    private bool isReloading;
    private Coroutine reloadCoroutine;
    private float remainingReloadTime;
    private float fireRateTimer;

    private IObjectPool<Bullet> bulletPool;

    private void Awake()
    {
        bulletPool = new ObjectPool<Bullet>(
            CreateBullet,
            OnTakeFromPool,
            OnReturnedToPool,
            OnDestroyPoolObject,
            true,
            20,
            50
        );
    }

    private void Start()
    {
        if (weaponData != null)
        {
            reloadTime = weaponData.reloadTime;
            magazineSize = weaponData.magazineSize;
            fireRate = weaponData.fireRate;
        }

        if (playerMovement == null)
            playerMovement = GetComponentInParent<PlayerMovement>();

        if (shootVFXSpawnPoint == null)
            shootVFXSpawnPoint = firePoint;

        currentBullet = magazineSize;
    }

    private void Update()
    {
        if (fireRateTimer > 0f)
            fireRateTimer -= Time.deltaTime;

            if (Input.GetMouseButtonDown(0))
    {
        // Panggil fungsi pembatas kita
        if (CanShoot())
        {
            FireBullet(); // Ganti dengan fungsi asli menembak peluru kamu
        }
        else
        {
            Debug.Log("[HandCannon] Gagal menembak! Kamu harus menahan KLIK KANAN.");
        }
    }
    }
    public bool CanShoot()
{
    // Input.GetMouseButton(1) artinya klik kanan SEDANG DITAHAN
    bool isAiming = Input.GetMouseButton(1); 

    // Senjata HANYA boleh menembak jika sedang Aiming
    return isAiming;
}

// Contoh pembungkus fungsi menembak bawaan kamu
private void FireBullet()
{
    if (currentBullet > 0 && !isReloading && fireRateTimer <= 0f)
    {
        // ... Jalankan logika Pooling Bullet bawaan kamu yang sudah ada ...
        // Bullet bulletInstance = bulletPool.Get();
        
        currentBullet--;
        fireRateTimer = 1f / fireRate;
        Debug.Log("BOOM! Berhasil menembak karena klik kanan ditahan.");
    }
}

    public bool CanFire()
    {
        return !isReloading && currentBullet > 0 && fireRateTimer <= 0f;
    }

    public override bool CanAttack()
    {
        return CanFire();
    }

    public void ConsumeBullet()
    {
        currentBullet--;

        fireRateTimer = fireRate;

        if (currentBullet <= 0)
        {
            reloadCoroutine = StartCoroutine(ReloadCoroutine());
        }
    }

public override void Attack()
{
    if (!CanFire() || playerMovement == null) return;

    Bullet bullet = bulletPool.Get();
    bullet.transform.position = firePoint.position;

    Vector3 targetPos = playerMovement.GetMouseTargetPosition;
    targetPos.y = firePoint.position.y; // Menyamakan tinggi agar peluru tidak menukik/menanjak

    Vector3 shootDirection = (targetPos - firePoint.position).normalized;
    bullet.transform.rotation = Quaternion.LookRotation(shootDirection);

    base.SpawnShootVFX();

    // --- LOGIKA KALKULASI BUFF DAMAGE RANGE SESUAI MATRIKS STAT ---
    int baseWeaponDamage = (weaponData != null) ? weaponData.damage : 10;
    int finalPeluruDamage = baseWeaponDamage;

    // 1. Cari komponen PlayerAttack di Parent GameObject (Player)
    PlayerAttack playerAttack = GetComponentInParent<PlayerAttack>();
    if (playerAttack != null)
    {
        // 2. Ambil nilai permanentDamageBuff dari PlayerAttack
        int buffDmg = playerAttack.GetPermanentDamageBuff();

        // 3. Jalankan rumus jika ada buff damage yang aktif dan magazineSize valid
        if (buffDmg > 0 && magazineSize > 0)
        {
            float setengahMagazine = (float)magazineSize / 2f;
            finalPeluruDamage += Mathf.RoundToInt((float)buffDmg / setengahMagazine);
        }
    }

    // 4. Kirim hasil kalkulasi damage yang sudah mencakup buff ke peluru
    bullet.Setup(20f, finalPeluruDamage); 

    ConsumeBullet();
}

    private void OnDisable()
    {
        // Jika senjata dihancurkan/dibuang saat reload, amankan statenya
        if (reloadCoroutine != null)
        {
            StopCoroutine(reloadCoroutine);
            reloadCoroutine = null;
        }

        // RESET UTAMA: Jika dibuang, paksa senjata dalam kondisi siap pakai saat diambil lagi
        isReloading = false;
        remainingReloadTime = 0f;
    }

    public void ResetReloadState()
    {
        if (reloadCoroutine != null)
        {
            StopCoroutine(reloadCoroutine);
            reloadCoroutine = null;
        }
        isReloading = false;
        currentBullet = magazineSize;
        remainingReloadTime = 0f;
    }

    public override void OnWeaponDeactivate()
    {
        if (isReloading && reloadCoroutine != null)
        {
            StopCoroutine(reloadCoroutine);
            reloadCoroutine = null;
            Debug.Log($"Reload dipause. Sisa waktu: {remainingReloadTime:F1}s");
        }
    }

    public override void OnWeaponActivate()
    {
        // Jika peluru kosong dan sedang tidak reload, paksa reload baru
        if (currentBullet <= 0 && !isReloading)
        {
            reloadCoroutine = StartCoroutine(ReloadCoroutine());
            return;
        }

        // Resume reload yang tertunda
        if (isReloading && reloadCoroutine == null && remainingReloadTime > 0)
        {
            reloadCoroutine = StartCoroutine(ReloadCoroutineWithRemainingTime(remainingReloadTime));
            Debug.Log($"Reload dilanjutkan. Sisa waktu: {remainingReloadTime:F1}s");
        }
    }

    private IEnumerator ReloadCoroutine()
    {
        isReloading = true;
        remainingReloadTime = reloadTime;
        Debug.Log("Reloading... " + reloadTime + "s");

        while (remainingReloadTime > 0f)
        {
            remainingReloadTime -= Time.deltaTime;
            yield return null; // Tunggu sampai frame berikutnya
        }

        CompleteReload();
    }

    private IEnumerator ReloadCoroutineWithRemainingTime(float remainingTime)
    {
        isReloading = true;
        remainingReloadTime = remainingTime;
        Debug.Log($"Reloading resumed... {remainingTime:F1}s");

        while (remainingReloadTime > 0f)
        {
            remainingReloadTime -= Time.deltaTime;
            yield return null;
        }

        CompleteReload();
    }

    private void CompleteReload()
    {
        currentBullet = magazineSize;
        isReloading = false;
        reloadCoroutine = null;
        remainingReloadTime = 0f;
        Debug.Log("Reloaded!");
    }

    //pooled object

    private Bullet CreateBullet()
    {
        Bullet bulletInstance = Instantiate(bulletPrefab);
        bulletInstance.SetPool(bulletPool);
        return bulletInstance;
    }

    private void OnTakeFromPool(Bullet bullet)
    {
        bullet.gameObject.SetActive(true);
    }

    private void OnReturnedToPool(Bullet bullet)
    {
        bullet.gameObject.SetActive(false);
    }

    private void OnDestroyPoolObject(Bullet bullet)
    {
        Destroy(bullet.gameObject);
    }

    public override float GetCooldownPercentage()
    {
        if (isReloading && reloadTime > 0f)
        {
            // Menghitung persentase sisa reload (1 = baru mulai reload, 0 = selesai)
            return remainingReloadTime / reloadTime;
        }
        return 0f;
    }
}