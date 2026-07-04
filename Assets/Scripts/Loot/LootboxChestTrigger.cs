using UnityEngine;

public class LootboxChestTrigger : MonoBehaviour
{
    [Header("Referensi Otak Gacha")]
    public LootboxManager lootboxManager; 

    [Header("Rarity Hasil Gacha")]
    public LootboxManager.Rarity weaponRarity; 

    [Header("Titik Muncul Senjata")]
    public Transform mySpawnPoint; 

    [Header("Identitas Senjata Box Ini")]
    public string weaponName; 

    // SUDAH DISESUAIKAN MENJADI 4 VARIAN
    [Header("Pool Varian Model 3D")]
    public GameObject[] legendaryPrefabs; 
    public GameObject[] rarePrefabs; 
    public GameObject[] commonPrefabs; 
    public GameObject[] rustyPrefabs; 

    [Header("Visual Animation Settings")]
    public float rotationSpeed = 70f; 
    public float hoverSpeed = 3f; 
    public float hoverAmount = 0.15f; 

    [Header("Tombol Interaksi")]
    // PERBAIKAN: Ubah default key menjadi Q sesuai instruksi Anda
    public KeyCode interactionKey = KeyCode.Q; 

    // --- TAMBAHAN KODE UI DI SINI ---
    [Header("UI Interaction Settings")]
    [Tooltip("Tarik game object UI petunjuk (misal teks 'Press Q to Open') ke sini")]
    public GameObject interactionUiPrompt; 

    // --- TAMBAHKAN AUDIO SETTING DI SINI ---
    [Header("Audio Settings")]
    public AudioClip openLootboxSound; 

    private bool isPlayerNearby = false; 
    private bool isOpened = false; 
    private GameObject mySpawnedWeapon; 
    private Vector3 startSpawnPos; 

    private void Start()
    {
        if (lootboxManager == null) 
        {
            lootboxManager = LootboxManager.Instance; 
        }

        if (mySpawnPoint == null) 
        {
            mySpawnPoint = transform.Find("SpawnPoint"); 
            if (mySpawnPoint == null) mySpawnPoint = this.transform; 
        }

        // Pastikan di awal game UI petunjuknya mati/sembunyi
        if (interactionUiPrompt != null)
        {
            interactionUiPrompt.SetActive(false);
        }
    }

    private void Update()
    {
        if (isPlayerNearby && !isOpened && Input.GetKeyDown(interactionKey)) 
        {
            OpenChest(); 
        }

        if (mySpawnedWeapon != null) 
        {
            mySpawnedWeapon.transform.Rotate(Vector3.up * rotationSpeed * Time.deltaTime, Space.World); 
            float newY = startSpawnPos.y + Mathf.Sin(Time.time * hoverSpeed) * hoverAmount; 
            mySpawnedWeapon.transform.position = new Vector3(mySpawnedWeapon.transform.position.x, newY, mySpawnedWeapon.transform.position.z); 
        }
    }

   private void OpenChest()
    {
        if (lootboxManager != null && mySpawnPoint != null) 
        {
            isOpened = true; 
            startSpawnPos = mySpawnPoint.position; 

            // --- SEGERA SEMBUNYIKAN UI SAAT PETI DIBUKA ---
            if (interactionUiPrompt != null)
            {
                interactionUiPrompt.SetActive(false);
            }

            // --- TAMBAHAN KODE AUDIO UTK LOOTBOX ---
            if (openLootboxSound != null) 
            {
                AudioSource.PlayClipAtPoint(openLootboxSound, transform.position); 
            }
            // ---------------------------------------

            Transform currentRoomParent = this.transform.parent; 

            // Mengirimkan 4 parameter pool varian ke LootboxManager
            LootboxManager.GachaOutput result = lootboxManager.OpenWeaponBox(
                legendaryPrefabs, rarePrefabs, commonPrefabs, rustyPrefabs, weaponName, mySpawnPoint.position, currentRoomParent 
            );
            
            mySpawnedWeapon = result.spawnedObject; 
            weaponRarity = result.finalRarity; 
            
            if (mySpawnedWeapon != null) 
            {
                WeaponPickup pickupComponent = mySpawnedWeapon.GetComponent<WeaponPickup>(); 
                if (pickupComponent != null) 
                {
                    // string ini otomatis berubah jadi "Legendary", "Rare", "Common", atau "Rusty"
                    pickupComponent.customRarity = weaponRarity.ToString(); 
                }
            }
        }
        else 
        {
            Debug.LogError($"LootboxManager atau MySpawnPoint belum di-assign pada {gameObject.name}!"); 
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        // Player mendekat -> Nyalakan UI petunjuk jika peti belum dibuka
        if (other.CompareTag("Player") && !isOpened) 
        {
            isPlayerNearby = true; 
            if (interactionUiPrompt != null)
            {
                interactionUiPrompt.SetActive(true);
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        // Player menjauh -> Matikan UI petunjuk
        if (other.CompareTag("Player")) 
        {
            isPlayerNearby = false; 
            if (interactionUiPrompt != null)
            {
                interactionUiPrompt.SetActive(false);
            }
        }
    }
}