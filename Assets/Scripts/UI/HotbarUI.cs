using UnityEngine;
using UnityEngine.UI;

public class HotbarUI : MonoBehaviour
{
    [Header("Dependencies")]
    [SerializeField] private PlayerInventory playerInventory;
    private PlayerAttack playerAttack;

    [Header("Slot References (Inventory Window)")]
    [SerializeField] private WeaponSlotUI unarmedSlot;
    [SerializeField] private WeaponSlotUI primarySlot;
    [SerializeField] private WeaponSlotUI secondarySlot;

    [Header("Active Visuals")]
    [SerializeField] private Image unarmedBorder;
    [SerializeField] private Image primaryBorder;
    [SerializeField] private Image secondaryBorder;
    [SerializeField] private Color activeColor = Color.pink;
    [SerializeField] private Color inactiveColor = Color.white;

    // =========================================================================
    // JALUR AMAN: Cooldown Indicator Pedang Pink di dalam Hotbar
    // =========================================================================
    [Header("Hotbar Cooldown Fill")]
    [SerializeField] private Image cooldownIndicator;

    private void Start()
    {
        if (playerInventory == null)
            playerInventory = FindFirstObjectByType<PlayerInventory>();

        if (playerAttack == null)
            playerAttack = FindFirstObjectByType<PlayerAttack>();

        if (playerInventory != null)
        {
            playerInventory.onSlotChanged.AddListener(OnSlotChanged);
            playerInventory.onActiveSlotChanged.AddListener(OnActiveSlotChanged);

            RefreshAllSlots();
            OnActiveSlotChanged(playerInventory.CurrentSlot);
        }

        // Awal main sembunyikan tirai cooldown
        if (cooldownIndicator != null)
        {
            cooldownIndicator.fillAmount = 0f;
            cooldownIndicator.gameObject.SetActive(false);
        }
    }

    private void Update()
    {
        if (playerAttack == null || cooldownIndicator == null) return;

        // Ambil komponen senjata yang lagi aktif di tangan player saat ini
        Weapon activeWeaponComponent = playerAttack.GetActiveWeapon();

        if (activeWeaponComponent != null)
        {
            // Ambil persentase sisa waktu rehat (1f penuh -> perlahan ke 0f)
            float cooldownPct = activeWeaponComponent.GetCooldownPercentage();

            if (cooldownPct > 0f)
            {
                // Kalau lagi cooldown, tampilin gambarnya jika belum aktif
                if (!cooldownIndicator.gameObject.activeSelf)
                    cooldownIndicator.gameObject.SetActive(true);

                // Update fillAmount biar tirai pedangnya turun dari atas ke bawah
                cooldownIndicator.fillAmount = cooldownPct;
            }
            else
            {
                // Kalau cooldown udah 0 atau selesai, sembunyikan gambarnya
                if (cooldownIndicator.gameObject.activeSelf)
                {
                    cooldownIndicator.fillAmount = 0f;
                    cooldownIndicator.gameObject.SetActive(false);
                }
            }
        }
        else
        {
            // Jika tangan kosong / tidak megang senjata apapun
            if (cooldownIndicator.gameObject.activeSelf)
            {
                cooldownIndicator.fillAmount = 0f;
                cooldownIndicator.gameObject.SetActive(false);
            }
        }
    }

    public void RefreshAllSlots()
    {
        if (playerInventory == null) return;

        if (primarySlot != null)
            primarySlot.SetWeapon(playerInventory.Slots[PlayerInventory.SLOT_WEAPON_1]);
        if (secondarySlot != null)
            secondarySlot.SetWeapon(playerInventory.Slots[PlayerInventory.SLOT_WEAPON_2]);

        if (unarmedBorder != null) unarmedBorder.gameObject.SetActive(true);
        if (primaryBorder != null) primaryBorder.gameObject.SetActive(true);
        if (secondaryBorder != null) secondaryBorder.gameObject.SetActive(true);
    }

    private void OnDestroy()
    {
        if (playerInventory != null)
        {
            playerInventory.onSlotChanged.RemoveListener(OnSlotChanged);
            playerInventory.onActiveSlotChanged.RemoveListener(OnActiveSlotChanged);
        }
    }

    private void OnSlotChanged(int slotIndex, WeaponData data)
    {
        if (slotIndex == PlayerInventory.SLOT_WEAPON_1 && primarySlot != null)
            primarySlot.SetWeapon(data);
        else if (slotIndex == PlayerInventory.SLOT_WEAPON_2 && secondarySlot != null)
            secondarySlot.SetWeapon(data);
    }

    private void OnActiveSlotChanged(int activeSlotIndex)
    {
        if (unarmedBorder != null)
            unarmedBorder.color = (activeSlotIndex == PlayerInventory.SLOT_UNARMED) ? activeColor : inactiveColor;
        if (primaryBorder != null)
            primaryBorder.color = (activeSlotIndex == PlayerInventory.SLOT_WEAPON_1) ? activeColor : inactiveColor;
        if (secondaryBorder != null)
            secondaryBorder.color = (activeSlotIndex == PlayerInventory.SLOT_WEAPON_2) ? activeColor : inactiveColor;
    }
}