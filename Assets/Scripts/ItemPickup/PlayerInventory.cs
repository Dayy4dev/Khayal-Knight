using UnityEngine;
using UnityEngine.Events;

public class PlayerInventory : MonoBehaviour
{
    public const int SLOT_UNARMED = 0;
    public const int SLOT_WEAPON_1 = 1;
    public const int SLOT_WEAPON_2 = 2;
    public const int TOTAL_SLOTS = 3;

    [Header("Unarmed Weapon Data")]
    [SerializeField] private WeaponData unarmedData;

    [Header("Starting Weapons")]
    [SerializeField] private WeaponData balloonSwordData;
    [SerializeField] private WeaponData handCannonData;

    [Header("References")]
    [SerializeField] private PlayerAttack playerAttack;
    [SerializeField] private Transform dropPoint;
    [SerializeField] private float pickupRadius = 2f;
    [SerializeField] private LayerMask pickupLayer;

    [Header("Movement Speed Setup")]
    [SerializeField] private PlayerMovement playerMovement;
    [SerializeField] private float normalSpeed = 2f;
    [SerializeField] private float unarmedSpeed = 4f;

    [Header("Inventory Audio Settings")]
    [SerializeField] private AudioSource inventoryAudioSource;

    [SerializeField] private AudioClip switchWeaponSound;


    private WeaponData[] slots = new WeaponData[TOTAL_SLOTS];
    private int currentSlot = SLOT_UNARMED;
    private WeaponPickup nearestPickup;

    private PlayerHealth playerHealth;
    private Transform[] allChildrenCache;

    public UnityEvent<int, WeaponData> onSlotChanged;
    public UnityEvent<int> onActiveSlotChanged;
    public UnityEvent<WeaponPickup> onNearestPickupChanged;

    public int CurrentSlot => currentSlot;
    public WeaponData CurrentWeapon => slots[currentSlot];
    public WeaponData[] Slots => slots;

    void Awake()
    {
        if (playerAttack == null)
            playerAttack = GetComponent<PlayerAttack>();

        playerHealth = GetComponent<PlayerHealth>();

        if (playerMovement == null)
            playerMovement = GetComponent<PlayerMovement>();
    }

    void Start()
    {
        allChildrenCache = GetComponentsInChildren<Transform>(true);


        if (unarmedData != null)
        {
            slots[SLOT_UNARMED] = unarmedData;
        }


        if (balloonSwordData != null)
        {
            slots[SLOT_WEAPON_1] = balloonSwordData;
            onSlotChanged?.Invoke(SLOT_WEAPON_1, balloonSwordData);
        }

        if (handCannonData != null)
        {
            slots[SLOT_WEAPON_2] = handCannonData;
            onSlotChanged?.Invoke(SLOT_WEAPON_2, handCannonData);
        }

        currentSlot = SLOT_WEAPON_1;
        EquipSlot(SLOT_WEAPON_1);

        onActiveSlotChanged?.Invoke(currentSlot);
    }

    void Update()
    {
        if (GameManager.Instance != null && !GameManager.Instance.IsPlaying) return;
        if (playerHealth != null && playerHealth.IsDead)
        {
            if (nearestPickup != null)
            {
                nearestPickup = null;
                onNearestPickupChanged?.Invoke(null);
            }
            return;
        }

        HandlePickupDetection();
        HandlePickupInput();
        HandleDropInput();
        HandleSlotSwitch();
    }

    private string GetWeaponCategory(WeaponData data)
    {
        if (data == null || string.IsNullOrEmpty(data.weaponName)) return "Unknown";
        string nameLower = data.weaponName.ToLower();

        if (nameLower.Contains("sword") || nameLower.Contains("blade") || nameLower.Contains("calibur"))
            return "Sword";
        if (nameLower.Contains("cannon") || nameLower.Contains("blaster") || nameLower.Contains("artillery"))
            return "Cannon";
        if (nameLower.Contains("hammer") || nameLower.Contains("mallet"))
            return "Hammer";

        return "Unknown";
    }

    private string GetVisualWeaponName(string originalWeaponName)
    {
        string nameLower = originalWeaponName.ToLower();
        if (nameLower.Contains("balloonsword") || nameLower.Contains("baloonsword")) return "balloonsword";
        if (nameLower.Contains("handcannon") || nameLower.Contains("handcannpn")) return "handcannon";
        if (nameLower.Contains("toyhammer") || nameLower.Contains("squeekhammer")) return "squeekhammer";
        return nameLower;
    }

    public void TryPickup(WeaponPickup pickup)
    {
        WeaponData data = pickup.Data;
        string newCategory = GetWeaponCategory(data);
        if (newCategory != "Unknown")
        {
            for (int i = SLOT_WEAPON_1; i <= SLOT_WEAPON_2; i++)
            {
                if (slots[i] != null && GetWeaponCategory(slots[i]) == newCategory)
                {
                    Debug.LogWarning($"[Inventory] Gagal! Kamu sudah memiliki senjata tipe {newCategory} ({slots[i].weaponName}) di inventory.");
                    return;
                }
            }
        }

        int emptySlot = FindEmptyWeaponSlot();
        if (emptySlot != -1)
        {
            SetSlot(emptySlot, data);
            if (nearestPickup == pickup) { nearestPickup = null; onNearestPickupChanged?.Invoke(null); }
            pickup.OnPickedUp();
            SwitchToSlot(emptySlot);
        }
        else
        {
            if (currentSlot == SLOT_UNARMED)
            {
                DropFromSlot(SLOT_WEAPON_1);
                SetSlot(SLOT_WEAPON_1, data);
                if (nearestPickup == pickup) { nearestPickup = null; onNearestPickupChanged?.Invoke(null); }
                pickup.OnPickedUp();
                SwitchToSlot(SLOT_WEAPON_1);
            }
            else
            {
                DropFromSlot(currentSlot);
                SetSlot(currentSlot, data);
                if (nearestPickup == pickup) { nearestPickup = null; onNearestPickupChanged?.Invoke(null); }
                pickup.OnPickedUp();
                EquipSlot(currentSlot);
            }
        }
    }

    public void TryDrop()
    {
        if (currentSlot == SLOT_UNARMED) return;
        if (slots[currentSlot] == null) return;

        int droppedSlot = currentSlot;
        DropFromSlot(droppedSlot);

        if (TutorialManager.Instance != null) TutorialManager.Instance.sudahBuangSenjata = true;

        if (droppedSlot == SLOT_WEAPON_1 && slots[SLOT_WEAPON_2] != null)
        {
            SetSlot(SLOT_WEAPON_1, slots[SLOT_WEAPON_2]);
            SetSlot(SLOT_WEAPON_2, null);
            SwitchToSlot(SLOT_WEAPON_1);
            return;
        }
        if (droppedSlot == SLOT_WEAPON_2 && slots[SLOT_WEAPON_1] != null)
        {
            SwitchToSlot(SLOT_WEAPON_1);
            return;
        }
        SwitchToSlot(SLOT_UNARMED);
    }

    private void SetSlot(int index, WeaponData data)
    {
        slots[index] = data;
        onSlotChanged?.Invoke(index, data);
    }

    private void DropFromSlot(int index)
    {
        if (index == SLOT_UNARMED) return;
        WeaponData data = slots[index];
        if (data == null) return;

        if (data.pickupPrefab != null)
        {
            Vector3 spawnPos = dropPoint != null ? dropPoint.position : transform.position + transform.forward * 0.8f;
            if (dropPoint != null) spawnPos.y = dropPoint.position.y;


            GameObject dropped = Instantiate(data.pickupPrefab, spawnPos, Quaternion.identity);

            if (dropped.TryGetComponent<WeaponPickup>(out WeaponPickup wp)) wp.MarkAsDropped();

            if (DungeonManager.Instance != null)
            {
                DungeonManager.Instance.ParentItemToCurrentRoom(dropped.transform);
            }
        }

        SetSlot(index, null);
    }
    private int FindEmptyWeaponSlot()
    {
        for (int i = SLOT_WEAPON_1; i <= SLOT_WEAPON_2; i++) { if (slots[i] == null) return i; }
        return -1;
    }

    private void SwitchToSlot(int slotIndex)
    {
        if (slotIndex < 0 || slotIndex >= TOTAL_SLOTS) return;


        int oldSlot = currentSlot;

        currentSlot = slotIndex;

        EquipSlot(slotIndex);

        if (oldSlot != currentSlot && inventoryAudioSource != null && switchWeaponSound != null)
        {
            inventoryAudioSource.PlayOneShot(switchWeaponSound);
        }


        onActiveSlotChanged?.Invoke(currentSlot);
        onSlotChanged?.Invoke(currentSlot, slots[currentSlot]);
    }
    private void EquipSlot(int index)
    {
        WeaponData activeData = slots[index];
        string activeWeaponName = "";
        bool isUnarmedActive = false;

        if (index == SLOT_UNARMED)
        {
            activeWeaponName = "unarmed";
            isUnarmedActive = true;
        }
        else if (activeData != null)
        {
            string rawName = activeData.weaponName.Replace("_", " ").Replace("  ", " ").ToLower();
            activeWeaponName = GetVisualWeaponName(rawName);
            if (activeWeaponName == "unarmed") isUnarmedActive = true;
        }

        if (playerMovement != null)
        {
            playerMovement.moveSpeed = isUnarmedActive ? unarmedSpeed : normalSpeed;
        }

        WeaponHitbox foundHitbox = null;
        Weapon foundWeaponComponent = null;
        string weaponName = "";
        bool isWeaponActive = false;


        if (playerAttack != null)
        {
            Weapon oldWeapon = playerAttack.GetActiveWeapon();
            if (oldWeapon != null) oldWeapon.OnWeaponDeactivate();
        }

        foreach (Transform child in allChildrenCache)
        {
            if (child == null || child == transform) continue;
            string childName = child.gameObject.name.Replace("_", " ").Replace("  ", " ").ToLower();


            if (childName.EndsWith("hand"))
            {
                weaponName = childName.Substring(0, childName.Length - 4).Trim();
                isWeaponActive = !string.IsNullOrEmpty(activeWeaponName) && activeWeaponName == weaponName;
                child.gameObject.SetActive(isWeaponActive);

                if (isWeaponActive)
                {
                    foundHitbox = child.GetComponent<WeaponHitbox>();
                    if (foundHitbox == null) foundHitbox = child.GetComponentInChildren<WeaponHitbox>(true);

                    Weapon[] weapons = child.GetComponents<Weapon>();
                    if (weapons.Length == 0) weapons = child.GetComponentsInChildren<Weapon>(true);

                    foreach (Weapon w in weapons)
                    {
                        if (w != null)
                        {
                            foundWeaponComponent = w;
                            break;
                        }
                    }
                }
            }

            if (childName.EndsWith("back"))
            {
                weaponName = childName.Substring(0, childName.Length - 4).Trim();
                bool isOwnedInSlot1 = slots[SLOT_WEAPON_1] != null && GetVisualWeaponName(slots[SLOT_WEAPON_1].weaponName) == weaponName;
                bool isOwnedInSlot2 = slots[SLOT_WEAPON_2] != null && GetVisualWeaponName(slots[SLOT_WEAPON_2].weaponName) == weaponName;

                bool isWeaponOwned = isOwnedInSlot1 || isOwnedInSlot2;
                isWeaponActive = !string.IsNullOrEmpty(activeWeaponName) && activeWeaponName == weaponName;
                child.gameObject.SetActive(isWeaponOwned && !isWeaponActive);
            }
        }


        if (playerAttack != null)
        {
            playerAttack.SetWeaponHitbox(foundHitbox);

            WeaponData targetData = activeData != null ? activeData : slots[SLOT_UNARMED];
            playerAttack.EquipWeapon(targetData, foundWeaponComponent);

            if (foundWeaponComponent != null)
            {
                foundWeaponComponent.OnWeaponActivate();
            }
        }

        if (index == SLOT_UNARMED)
            Debug.Log("<color=white>[Weapon Check] Slot: Unarmed</color>");
        else if (activeData != null)
            Debug.Log($"<color=yellow>[Weapon Check] Slot: {activeData.weaponName} | DMGs: {activeData.damage}</color>");
    }

    private void HandleSlotSwitch()
    {
        if (Input.GetKeyDown(KeyCode.Alpha1)) SwitchToSlot(SLOT_UNARMED);
        if (Input.GetKeyDown(KeyCode.Alpha2)) SwitchToSlot(SLOT_WEAPON_1);
        if (Input.GetKeyDown(KeyCode.Alpha3)) SwitchToSlot(SLOT_WEAPON_2);

        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (scroll > 0f) ScrollToNextWeapon(+1);
        else if (scroll < 0f) ScrollToNextWeapon(-1);
    }

    private void ScrollToNextWeapon(int direction)
    {
        int next = currentSlot;
        for (int i = 0; i < TOTAL_SLOTS; i++)
        {
            next = (next + direction + TOTAL_SLOTS) % TOTAL_SLOTS;
            if (next == SLOT_UNARMED || slots[next] != null) { SwitchToSlot(next); return; }
        }
        SwitchToSlot(SLOT_UNARMED);
    }

    private void HandleDropInput() { if (Input.GetKeyDown(KeyCode.G)) TryDrop(); }

    private void HandlePickupDetection()
    {
        Collider[] hits = Physics.OverlapSphere(transform.position, pickupRadius, pickupLayer);
        WeaponPickup closest = null;
        float closestDist = float.MaxValue;

        foreach (Collider hit in hits)
        {
            if (!hit.TryGetComponent<WeaponPickup>(out WeaponPickup wp)) continue;
            float dist = Vector3.Distance(transform.position, hit.transform.position);
            if (dist < closestDist) { closestDist = dist; closest = wp; }
        }
        if (closest != nearestPickup) { nearestPickup = closest; onNearestPickupChanged?.Invoke(nearestPickup); }
    }

    private void HandlePickupInput() { if (Input.GetKeyDown(KeyCode.F) && nearestPickup != null) TryPickup(nearestPickup); }

    void OnDrawGizmosSelected() { Gizmos.color = Color.yellow; Gizmos.DrawWireSphere(transform.position, pickupRadius); }
}