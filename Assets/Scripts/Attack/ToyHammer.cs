using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class ToyHammer : Weapon
{
    private WeaponHitbox hitbox;
    [SerializeField] private WeaponData weaponData;

    [Header("Heavy Impact")]
    [SerializeField] private HeavyImpact heavyImpact;
    [Tooltip("Delay (seconds) before Heavy Impact fires after Attack3 starts. " +
             "Tune this to match the animation's impact frame.")]
    [SerializeField] private float heavyImpactDelay = 2.4f;

    // State Melee Reload/Rehat
    private int maxComboCount;
    private float meleeReloadTime;
    private int currentComboLeft;
    private bool isReheating;
    private float remainingReheatTime;
    private Coroutine reheatCoroutine;
    private int damage;


    private Coroutine heavyImpactCoroutine;
    private HashSet<int> hitboxDamagedEnemies = new HashSet<int>();

    private void Awake()
    {
        hitbox = GetComponent<WeaponHitbox>();
        if (hitbox == null)
        {
            Debug.LogWarning("[ToyHammer] No WeaponHitbox component found!");
        }

        if (heavyImpact == null)
            heavyImpact = GetComponent<HeavyImpact>();

        // Track enemies damaged by the hitbox so HeavyImpact can skip them
        if (hitbox != null)
            hitbox.OnEnemyHit += TrackHitboxHit;
    }

    private void OnDestroy()
    {
        if (hitbox != null)
            hitbox.OnEnemyHit -= TrackHitboxHit;
    }

    private void TrackHitboxHit(GameObject enemy)
    {
        hitboxDamagedEnemies.Add(enemy.GetInstanceID());
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

    private void OnDisable()
    {
        if (reheatCoroutine != null)
        {
            StopCoroutine(reheatCoroutine);
            reheatCoroutine = null;
        }
        StopHeavyImpactCoroutine();
        isReheating = false;
        remainingReheatTime = 0f;
    }

    public override bool CanAttack()
    {
        return !isReheating && currentComboLeft > 0;
    }

    public override void Attack()
    {
        if (!CanAttack() || hitbox == null) return;

        // Determine which combo hit this is
        PlayerAttack playerAttack = GetComponentInParent<PlayerAttack>();
        int comboHit = (playerAttack != null) ? playerAttack.GetCurrentComboHit() : 1;

        // All attacks: activate hitbox immediately for damage/knockback
        hitbox.ActivateHitbox();
        Invoke(nameof(DeactivateHitbox), 0.3f);

        // Attack3 (Heavy Finisher): also schedule Heavy Impact for area effects
        // (camera shake, VFX, area knockback/launch) after a delay
        if (comboHit == 3 && heavyImpact != null)
        {
            StopHeavyImpactCoroutine();
            heavyImpactCoroutine = StartCoroutine(HeavyImpactDelayed());
            Debug.Log("[ToyHammer] Attack3 — Heavy Impact scheduled via coroutine.");
        }

        // Deduct combo charge
        currentComboLeft--;

        if (currentComboLeft <= 0)
        {
            reheatCoroutine = StartCoroutine(ReheatCoroutine());
        }
    }

    private IEnumerator HeavyImpactDelayed()
    {
        yield return new WaitForSeconds(heavyImpactDelay);
        if (heavyImpact != null)
        {
            // Pass enemies already damaged by the hitbox to prevent double-damage
            heavyImpact.ExecuteImpact(hitboxDamagedEnemies);
        }
        hitboxDamagedEnemies.Clear();
        heavyImpactCoroutine = null;
    }

    private void StopHeavyImpactCoroutine()
    {
        if (heavyImpactCoroutine != null)
        {
            StopCoroutine(heavyImpactCoroutine);
            heavyImpactCoroutine = null;
        }
    }

    private void DeactivateHitbox()
    {
        if (hitbox != null)
            hitbox.DeactivateHitbox();
    }

    public override void OnWeaponDeactivate()
    {
        StopHeavyImpactCoroutine();

        if (isReheating && reheatCoroutine != null)
        {
            StopCoroutine(reheatCoroutine);
            reheatCoroutine = null;
            Debug.Log($"[ToyHammer] Rehat dipause. Sisa: {remainingReheatTime:F1}s");
        }
    }

    public override void OnWeaponActivate()
    {
        if (currentComboLeft <= 0 && !isReheating)
        {
            reheatCoroutine = StartCoroutine(ReheatCoroutine());
            return;
        }

        if (isReheating && reheatCoroutine == null && remainingReheatTime > 0)
        {
            reheatCoroutine = StartCoroutine(ReheatWithRemainingTime(remainingReheatTime));
            Debug.Log($"[ToyHammer] Rehat dilanjutkan. Sisa: {remainingReheatTime:F1}s");
        }
    }

    private IEnumerator ReheatCoroutine()
    {
        isReheating = true;
        remainingReheatTime = meleeReloadTime;
        Debug.Log("[ToyHammer] Senjata lelah, sedang rehat...");

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
        Debug.Log("[ToyHammer] Rehat selesai! Siap memukul lagi.");
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
