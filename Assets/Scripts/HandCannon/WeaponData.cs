using UnityEngine;

[CreateAssetMenu(fileName = "NewWeaponData", menuName = "Weapons/Weapon Data")]
public class WeaponData : ScriptableObject
{
    [Header("Info")]
    public string weaponName = "Unnamed Weapon";
    public Sprite icon;
    
    // --- TAMBAHAN BARU UNTUK MENYIMPAN RARITY DINAMIS ---
    [HideInInspector] 
    public string overrideRarity = "";

    [Header("Pickup")]
    [Tooltip("Prefab WeaponPickup yang di-spawn saat senjata dijatuhkan")]
    public GameObject pickupPrefab;

    [Header("Melee Stats")]
    public int   damage           = 10;
    public float attackDuration   = 0.2f;
    public float attackCooldown   = 0.5f;
    public float knockbackForce   = 5f;
    public float knockbackDuration = 0.2f;
    [Tooltip("Berapa kali ayunan sebelum senjata harus rehat/reload")]
    public int   maxComboCount    = 3; 
    [Tooltip("Durasi rehat/reload melee (detik)")]
    public float meleeReloadTime  = 1.5f;

    [Header("Ranged Stats")]
    public int   defaultCapacity = 0;
    public int   maxPoolSize     = 0;
    public float speed           = 0f;
    public int   magazineSize    = 0;
    public float reloadTime      = 0f;
    public float fireRate        = 0.3f; // cd tiap tembakan (detik)
}