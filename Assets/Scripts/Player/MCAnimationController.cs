using UnityEngine;
using UnityEngine.Animations.Rigging;
using System;

[AddComponentMenu("Player/MC Animation Controller")]
[RequireComponent(typeof(Animator))]
public class MCAnimationController : MonoBehaviour
{
    public const int WEAPON_SWORD   = 0;
    public const int WEAPON_HAMMER  = 1;
    public const int WEAPON_CANNON  = 2;
    public const int WEAPON_UNARMED = 3;

    private static readonly int WeaponIndex = Animator.StringToHash("WeaponIndex");
    private static readonly int Attack1     = Animator.StringToHash("Attack1");
    private static readonly int Attack2     = Animator.StringToHash("Attack2");
    private static readonly int Attack3     = Animator.StringToHash("Attack3");

    private Animator animator;
    private PlayerAttack playerAttack;

    private float comboCooldownTimer;
    private bool isInComboCooldown;
    private int currentWeaponIndex = WEAPON_SWORD;

    public bool IsInComboCooldown => isInComboCooldown;

    [Header("Hammer Attack2 Buffer")]
    [Tooltip("How long (seconds) a buffered Attack2 input stays valid during Attack1 wind-up.")]
    [SerializeField] private float comboInputBuffer = 0.4f;

    public event Action OnHammerAttack2Fired;
    public event Action OnHammerAttack3Fired;

    private bool hammerAttack2Buffered;  
    private float hammerBufferExpiry;    

    void Awake()
    {
        animator = GetComponent<Animator>();
        playerAttack = GetComponent<PlayerAttack>();
        if (playerAttack == null)
            playerAttack = FindFirstObjectByType<PlayerAttack>();
    }

    void OnEnable()
    {
        if (playerAttack != null)
        {
            playerAttack.OnWeaponEquipped += HandleWeaponEquipped;
            playerAttack.OnAttackExecuted += HandleAttackExecuted;
        }
    }

    void OnDisable()
    {
        if (playerAttack != null)
        {
            playerAttack.OnWeaponEquipped -= HandleWeaponEquipped;
            playerAttack.OnAttackExecuted -= HandleAttackExecuted;
        }
    }

    void Start()
    {
        ApplyWeaponIndex(currentWeaponIndex);
        EnsureAimRigLayer();
    }

    private void EnsureAimRigLayer()
    {
        Transform aimRigTransform = transform.Find("AimRig");
        if (aimRigTransform == null) return;

        var aimRig = aimRigTransform.GetComponent<Rig>();
        if (aimRig == null) return;

        var rigBuilder = GetComponent<RigBuilder>();
        if (rigBuilder == null) return;

        for (int i = 0; i < rigBuilder.layers.Count; i++)
        {
            if (rigBuilder.layers[i].rig == aimRig)
                return;
        }

        rigBuilder.layers.Add(new UnityEngine.Animations.Rigging.RigLayer(aimRig));
        Debug.Log("[MCAnimCtrl] AimRig added to RigBuilder layers.");
    }

    void Update()
    {
        if (animator == null) return;
        if (GameManager.Instance != null && !GameManager.Instance.IsPlaying) return;

        UpdateComboCooldown();
        FlushHammerAttack2Buffer();
    }


    private void HandleWeaponEquipped(string category)
    {
        int newIndex = CategoryToIndex(category);
        ApplyWeaponIndex(newIndex);
        ResetAnimationState();
    }

    private void ApplyWeaponIndex(int index)
    {
        currentWeaponIndex = index;
        if (animator != null)
            animator.SetFloat(WeaponIndex, (float)index);
    }

    private static int CategoryToIndex(string category)
    {
        if (string.IsNullOrEmpty(category)) return WEAPON_UNARMED;
        switch (category)
        {
            case "Sword":   return WEAPON_SWORD;
            case "Hammer":  return WEAPON_HAMMER;
            case "Cannon":  return WEAPON_CANNON;
            case "Unarmed": return WEAPON_UNARMED;
            default:        return WEAPON_UNARMED;
        }
    }

    private void HandleAttackExecuted(int comboHit)
    {
        if (isInComboCooldown) return;

        if (currentWeaponIndex == WEAPON_HAMMER && comboHit == 2)
        {
            hammerAttack2Buffered = true;
            hammerBufferExpiry = Time.time + comboInputBuffer;
            Debug.Log("[MCAnimCtrl] Hammer Attack2 buffered.");
            int maxCombo = GetMaxCombo();
            if (comboHit >= maxCombo)
            {
                isInComboCooldown = true;
                comboCooldownTimer = GetComboCooldownDuration();
            }
            return;
        }

        switch (comboHit)
        {
            case 1: animator.SetTrigger(Attack1); break;
            case 2: 
                animator.SetTrigger(Attack2); 
                if (currentWeaponIndex == WEAPON_HAMMER)
                    OnHammerAttack2Fired?.Invoke();
                break;
            case 3: 
                animator.SetTrigger(Attack3); 
                if (currentWeaponIndex == WEAPON_HAMMER)
                    OnHammerAttack3Fired?.Invoke();
                break;
        }

        int maxComboPassed = GetMaxCombo();
        if (comboHit >= maxComboPassed)
        {
            isInComboCooldown = true;
            comboCooldownTimer = GetComboCooldownDuration();
        }
    }

    private void FlushHammerAttack2Buffer()
    {
        if (!hammerAttack2Buffered) return;

        if (Time.time > hammerBufferExpiry)
        {
            hammerAttack2Buffered = false;
            Debug.Log("[MCAnimCtrl] Hammer Attack2 buffer expired.");
            return;
        }

        AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(1); // Attack layer = 1
        bool attack1Playing = stateInfo.IsName("Attack1");
        bool attack1NearEnd = attack1Playing && stateInfo.normalizedTime >= 0.65f;
        bool notInAttack1   = !attack1Playing;

        if (notInAttack1 || attack1NearEnd)
        {
            hammerAttack2Buffered = false;
            animator.SetTrigger(Attack2);
            OnHammerAttack2Fired?.Invoke();
            Debug.Log("[MCAnimCtrl] Hammer Attack2 trigger fired from buffer.");
        }
    }

    private void UpdateComboCooldown()
    {
        if (!isInComboCooldown) return;

        comboCooldownTimer -= Time.deltaTime;
        if (comboCooldownTimer <= 0f)
        {
            isInComboCooldown = false;
            comboCooldownTimer = 0f;
        }
    }

    private float GetComboCooldownDuration()
    {
        WeaponData data = GetCurrentWeaponData();
        if (data == null) return 0.5f;

        string cat = GetWeaponCategory();
        if (cat == "Cannon") return data.reloadTime > 0f ? data.reloadTime : data.attackCooldown;
        return data.attackCooldown > 0f ? data.attackCooldown : 0.5f;
    }

    private int GetMaxCombo()
    {
        WeaponData data = GetCurrentWeaponData();
        if (data == null) return 1;

        string cat = GetWeaponCategory();
        if (cat == "Cannon") return data.magazineSize > 0 ? data.magazineSize : 1;
        return data.maxComboCount > 0 ? data.maxComboCount : 1;
    }

    private WeaponData GetCurrentWeaponData()
    {
        return playerAttack != null ? playerAttack.GetCurrentWeaponData() : null;
    }

    private string GetWeaponCategory()
    {
        WeaponData data = GetCurrentWeaponData();
        if (data == null || string.IsNullOrEmpty(data.weaponName)) return "Unarmed";
        string n = data.weaponName.ToLower();
        if (n.Contains("sword") || n.Contains("blade") || n.Contains("calibur")) return "Sword";
        if (n.Contains("hammer") || n.Contains("mallet")) return "Hammer";
        if (n.Contains("cannon") || n.Contains("blaster")) return "Cannon";
        return "Unarmed";
    }

    private void ResetAnimationState()
    {
        isInComboCooldown = false;
        comboCooldownTimer = 0f;
        hammerAttack2Buffered = false;
    }

    public void OnWeaponChanged()
    {
        ResetAnimationState();
    }
}