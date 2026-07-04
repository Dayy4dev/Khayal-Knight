using UnityEngine;

public class WeaponTester : MonoBehaviour
{
    private PlayerInventory inventory;
    private PlayerAttack playerAttack;

    void Awake()
    {
        // Mencari komponen yang ada di Player
        inventory = GetComponent<PlayerInventory>();
        playerAttack = GetComponent<PlayerAttack>();
    }

    void OnGUI()
    {
        // Membuat kotak background di pojok kiri atas layar
        GUILayout.BeginArea(new Rect(20, 20, 350, 150), GUI.skin.box);
        
        GUILayout.Label("=== WEAPON DEBUG TESTER ===", GUILayout.ExpandWidth(true));
        
        if (inventory != null)
        {
            GUILayout.Label("Slot Aktif: " + inventory.CurrentSlot);
            
            if (inventory.CurrentWeapon != null)
            {
                // Menampilkan DATA ASLI yang sedang aktif di inventory
                GUILayout.Label("Data Asset Aktif: " + inventory.CurrentWeapon.weaponName);
                GUILayout.Label("Damage Senjata: " + inventory.CurrentWeapon.damage);
            }
            else
            {
                GUILayout.Label("Data Asset Aktif: Bertangan Kosong (Unarmed)");
            }
        }
        else
        {
            GUILayout.Label("ERROR: PlayerInventory tidak ditemukan!", ColorRed());
        }

        GUILayout.EndArea();
    }

    private GUIStyle ColorRed()
    {
        GUIStyle style = new GUIStyle();
        style.normal.textColor = Color.red;
        return style;
    }
}