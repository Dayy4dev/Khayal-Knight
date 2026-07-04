using UnityEngine;

public class EquipmentUI : MonoBehaviour
{
    [Header("Dependencies")]
    [SerializeField] private PlayerInventory playerInventory;

    [Header("Slot References")]
    [SerializeField] private WeaponSlotUI primarySlot;
    [SerializeField] private WeaponSlotUI secondarySlot;

    private void Start()
    {
        if (playerInventory == null)
            playerInventory = FindFirstObjectByType<PlayerInventory>();

        if (playerInventory != null)
        {
            playerInventory.onSlotChanged.AddListener(OnSlotChanged);
        }

        // Force a graphic refresh on boot up as a safety net
        Invoke(nameof(RefreshUI), 0.1f);
    }
    
    private void OnDestroy()
    {
        if (playerInventory != null)
        {
            playerInventory.onSlotChanged.RemoveListener(OnSlotChanged);
        }
    }
    
    private void OnSlotChanged(int slotIndex, WeaponData data)
    {
        // Refresh the specific slot that changed
        if (slotIndex == PlayerInventory.SLOT_WEAPON_1 && primarySlot != null)
        {
            primarySlot.SetWeapon(data);
        }
        else if (slotIndex == PlayerInventory.SLOT_WEAPON_2 && secondarySlot != null)
        {
            secondarySlot.SetWeapon(data);
        }
    }

    public void RefreshUI()
    {
        // Verify the manager instance exists in the scene
        if (playerInventory == null) return;

        // Push data to the individual slots
        if (primarySlot != null)
        {
            primarySlot.SetWeapon(playerInventory.Slots[PlayerInventory.SLOT_WEAPON_1]);
        }

        if (secondarySlot != null)
        {
            secondarySlot.SetWeapon(playerInventory.Slots[PlayerInventory.SLOT_WEAPON_2]);
        }
    }
}