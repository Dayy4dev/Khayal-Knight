using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class DungeonManager : MonoBehaviour
{
    public static DungeonManager Instance { get; private set; }

    [Header("References")]
    public DungeonGenerator dungeonGenerator;
    public Transform playerTransform;

    [Header("Boss Floor Setup")]
    // UBAH: Menggunakan Array untuk variasi Boss Room biasa[cite: 3]
    public GameObject[] bossRoomPrefabs;
    // TAMBAHAN: Prefab khusus untuk Final Boss di Lantai 25
    public GameObject finalBossRoomPrefab;
    // TAMBAHAN: UI Text atau Panel untuk menampilkan pesan To Be Continued
    public GameObject toBeContinuedUI;

    [Header("Spawn Settings")]
    public string spawnPointName = "SpawnPoint";
    public Vector3 fallbackOffset = new Vector3(0, 1, 0);

    [Header("Transition")]
    public float transitionDelay = 0.5f;

    [Header("Room UI Bar Setup")]
    public RoomUIBar roomUIBar;
    public TextMeshProUGUI floorUIText;
    private int maxEnemiesInRoom = 0;
    private int currentEnemiesInRoom = 0;

    [Header("Partner Wildcard Setup")]
    public GameObject partnerPrefab;
    [Range(0, 100)] public float partnerSpawnChance = 40f;
    [Header("Dungeon Audio Settings")]
    [SerializeField] private AudioSource dungeonAudioSource;
    [SerializeField] private AudioClip doorOpenSound;
    [SerializeField] private AudioClip roomEnterSound;
    [SerializeField] private AudioClip bossFloorEnterSound;

    private RoomController currentRoom;
    private Dictionary<RoomType, RoomController> roomMap = new Dictionary<RoomType, RoomController>();
    private RoomType currentRoomType = RoomType.Bottom;
    private bool centerCleared = false;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    void Start()
    {
        StartCoroutine(InitializeDungeon());
    }

    IEnumerator InitializeDungeon()
    {
        dungeonGenerator.GenerateDungeon();
        yield return null;
        RegisterAllRooms();
        TeleportPlayerToRoom(RoomType.Bottom);

        UpdateFloorUI();

        int floor = FloorManager.Instance != null ? FloorManager.Instance.CurrentFloor : 1;
        Debug.Log($"[DungeonManager] Floor {floor} dimulai.");
    }

    void RegisterAllRooms()
    {
        roomMap.Clear();
        foreach (var room in FindObjectsOfType<RoomController>())
        {
            if (!roomMap.ContainsKey(room.roomType))
                roomMap[room.roomType] = room;
            else
                Debug.LogWarning($"[DungeonManager] Duplikat room type: {room.roomType}");
        }
        Debug.Log($"[DungeonManager] {roomMap.Count} room terdaftar.");
    }

    public void OnRoomCleared(RoomController room)
    {
        Debug.Log($"[DungeonManager] {room.roomType} cleared!");

        float diceRoll = UnityEngine.Random.Range(0f, 100f);
        bool shouldPartnerSpawn = diceRoll <= partnerSpawnChance;

        if (shouldPartnerSpawn && partnerPrefab != null)
        {
            Debug.Log("[Wildcard Event] Partner muncul! Pintu ditahan sampai permen dipilih.");

            Vector3 spawnPos = Vector3.zero;
            Transform targetSpawnpointTransform = null;
            bool spawnPointFound = false;

            Transform[] childTransforms = room.GetComponentsInChildren<Transform>();
            foreach (Transform child in childTransforms)
            {
                if (child.CompareTag("PartnerSpawnpoint"))
                {
                    spawnPos = child.position;
                    targetSpawnpointTransform = child;
                    spawnPointFound = true;
                    break;
                }
            }

            if (!spawnPointFound)
            {
                spawnPos = room.transform.position;
            }

            GameObject partnerObj = Instantiate(partnerPrefab, spawnPos, Quaternion.identity);

            if (spawnPointFound && targetSpawnpointTransform != null)
            {
                partnerObj.transform.parent = targetSpawnpointTransform;
                Debug.Log($"[DungeonManager] Partner berhasil dimasukkan ke dalam child {targetSpawnpointTransform.name}");
            }
            else
            {
                partnerObj.transform.parent = room.transform;
            }

            PartnerSystem partnerSys = partnerObj.GetComponent<PartnerSystem>();

            if (partnerSys != null)
            {
                partnerSys.OnCandySelected += () => { ExecuteRoomClearLogic(room); };
            }
        }
        else
        {
            ExecuteRoomClearLogic(room);
        }
    }

    private void ExecuteRoomClearLogic(RoomController room)
    {
        LootboxRoomSpawner roomSpawner = room.GetComponentInChildren<LootboxRoomSpawner>();
        if (roomSpawner != null)
        {
            roomSpawner.SpawnRandomChest();
        }

        if (FloorManager.Instance != null && FloorManager.Instance.IsBossFloor)
        {
            // TAMBAHAN: Jika ini adalah Boss Room lantai 25 dan berhasil dibersihkan/clear
            if (FloorManager.Instance.CurrentFloor == 25)
            {
                TriggerToBeContinued();
                return;
            }

            Debug.Log("[DungeonManager] Boss Floor terdeteksi selesai. Mengaktifkan Portal di Boss Room.");
            ActivatePortalInRoom(room);
            return;
        }

        switch (room.roomType)
        {
            case RoomType.Bottom:
                OpenExitDoorsOfRoom(RoomType.Bottom);
                break;

            case RoomType.Center:
                centerCleared = true;
                OpenExitDoorsOfRoom(RoomType.Center);
                break;

            case RoomType.Left:
            case RoomType.Right:
            case RoomType.Top:
                ActivatePortalInRoom(room);
                break;
        }
    }

    // TAMBAHAN: Fungsi untuk menangani akhir game sementara
    private void TriggerToBeContinued()
    {
        Debug.Log("[DungeonManager] FINAL BOSS KALAH! TO BE CONTINUED...");
        if (toBeContinuedUI != null)
        {
            toBeContinuedUI.SetActive(true);
        }
        // Opsional: Kamu bisa mematikan input player atau mem-pause game di sini jika diperlukan
        Time.timeScale = 0f; 
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    public void OnPlayerEnterDoor(RoomType destination, GameObject player)
    {
        if (!roomMap.ContainsKey(destination))
        {
            Debug.LogWarning($"[DungeonManager] Room {destination} tidak ditemukan!");
            return;
        }

        currentRoomType = destination;
        currentRoom = roomMap[destination];
        currentRoom.ActivateRoom();

        switch (destination)
        {
            case RoomType.Center:
                if (roomMap.TryGetValue(RoomType.Bottom, out RoomController bottomRoom))
                {
                    foreach (var door in bottomRoom.exitDoors)
                    {
                        if (door != null) door.LockDoor();
                    }
                    Debug.Log("[DungeonManager] Bottom room doors locked (player masuk Center)");
                }
                break;

            case RoomType.Left:
            case RoomType.Right:
            case RoomType.Top:
                if (roomMap.TryGetValue(RoomType.Center, out RoomController centerRoom))
                {
                    foreach (var door in centerRoom.exitDoors)
                    {
                        if (door != null) door.LockDoor();
                    }
                    Debug.Log($"[DungeonManager] Center room doors locked (player masuk {destination})");
                }
                break;
        }

        TeleportPlayerToRoom(destination);
    }

    public void GoToNextMap()
    {
        StartCoroutine(LoadNextMap());
    }

    void OpenExitDoorsOfRoom(RoomType type)
    {
        bool doorUnlocked = false;

        if (roomMap.TryGetValue(type, out RoomController room))
        {
            foreach (var door in room.exitDoors)
            {
                if (door != null)
                {
                    door.UnlockDoor();
                    doorUnlocked = true;
                }
            }
        }

        if (doorUnlocked && dungeonAudioSource != null && doorOpenSound != null)
        {
            dungeonAudioSource.PlayOneShot(doorOpenSound);
        }
    }

    void ActivatePortalInRoom(RoomController room)
    {
        NextMapPortal portal = room.GetComponentInChildren<NextMapPortal>(true);
        if (portal != null)
        {
            portal.Activate();
        }
        else
        {
            Debug.LogWarning($"[DungeonManager] Tidak ada NextMapPortal di {room.roomType}! Langsung load map.");
            StartCoroutine(LoadNextMap());
        }
    }

    void TeleportPlayerToRoom(RoomType type)
    {
        if (playerTransform == null) { Debug.LogWarning("[DungeonManager] playerTransform belum di-assign!"); return; }
        if (!roomMap.TryGetValue(type, out RoomController room)) { Debug.LogWarning($"[DungeonManager] Room {type} tidak ditemukan!"); return; }

        Transform spawnPoint = FindSpawnPoint(room.transform);
        Vector3 targetPosition = spawnPoint != null
            ? spawnPoint.position
            : room.transform.position + fallbackOffset;

        CharacterController cc = playerTransform.GetComponent<CharacterController>();
        if (cc != null) cc.enabled = false;

        playerTransform.position = targetPosition;
        Physics.SyncTransforms();

        if (cc != null) cc.enabled = true;

        currentRoom = room;
        currentRoomType = type;
        Debug.Log($"[DungeonManager] Player diteleport ke {type}.");

        if (dungeonAudioSource != null && roomEnterSound != null)
        {
            dungeonAudioSource.PlayOneShot(roomEnterSound);
        }

        EnemySpawner spawner = room.GetComponentInChildren<EnemySpawner>();
        if (spawner != null)
        {
            spawner.SpawnEnemies();
        }

        RefreshRoomEnemyCounter(type, room.gameObject);

        bool harusMengunci = (maxEnemiesInRoom > 0);

        if (room.roomState == RoomState.Active && harusMengunci)
        {
            if (room.exitDoors != null)
            {
                foreach (var door in room.exitDoors)
                {
                    if (door != null && !door.isLocked)
                    {
                        door.LockDoor();
                    }
                }
            }
        }

        foreach (Transform child in room.GetComponentsInChildren<Transform>(true))
        {
            if (child.name.Contains("Block") || child.name.Contains("Blocker"))
            {
                if (child.gameObject.activeSelf)
                {
                    Vector3 currentPos = child.transform.localPosition;
                    child.transform.localPosition = new Vector3(currentPos.x, 0f, currentPos.z);

                    Vector3 currentScale = child.transform.localScale;
                    child.transform.localScale = new Vector3(currentScale.x, 40f, currentScale.z);

                    BoxCollider boxCol = child.GetComponent<BoxCollider>();
                    if (boxCol != null)
                    {
                        boxCol.isTrigger = false;
                    }
                }
            }
        }
    }

    Transform FindSpawnPoint(Transform parent)
    {
        Transform direct = parent.Find(spawnPointName);
        if (direct != null) return direct;
        foreach (Transform child in parent)
        {
            Transform found = FindSpawnPoint(child);
            if (found != null) return found;
        }
        return null;
    }

    IEnumerator LoadNextMap()
    {
        Debug.Log("[DungeonManager] Memuat map berikutnya...");
        yield return new WaitForSeconds(transitionDelay);

        if (FloorManager.Instance != null)
            FloorManager.Instance.AdvanceFloor();

        foreach (var room in FindObjectsOfType<RoomController>())
            Destroy(room.gameObject);

        roomMap.Clear();
        centerCleared = false;
        currentRoomType = RoomType.Bottom;

        yield return null;

        bool isBossFloor = FloorManager.Instance != null && FloorManager.Instance.IsBossFloor;

        if (isBossFloor)
        {
            Debug.Log("[DungeonManager] BOSS FLOOR! Spawning boss room...");
            SpawnBossRoom();

            if (dungeonAudioSource != null && bossFloorEnterSound != null)
            {
                dungeonAudioSource.PlayOneShot(bossFloorEnterSound);
            }
        }
        else
        {
            dungeonGenerator.GenerateDungeon();
        }

        yield return null;

        RegisterAllRooms();
        WaitForEndOfFrame wait = new WaitForEndOfFrame();
        yield return wait;
        TeleportPlayerToRoom(RoomType.Bottom);

        UpdateFloorUI();

        Debug.Log("[DungeonManager] Map baru loaded!");
    }

    // MODIFIKASI: Menangani spawn acak prefab boss biasa atau prefab final boss
    // MODIFIKASI: Menangani spawn acak prefab boss, final boss, serta mengubah ukurannya menjadi 60, 1, 60
    void SpawnBossRoom()
    {
        int currentFloor = FloorManager.Instance != null ? FloorManager.Instance.CurrentFloor : 1;
        GameObject selectedPrefab = null;

        // 1. Cek apakah ini lantai final boss
        if (currentFloor == 25)
        {
            selectedPrefab = finalBossRoomPrefab;
            Debug.Log("[DungeonManager] Memilih prefab FINAL BOSS untuk lantai 25.");
        }
        else if (bossRoomPrefabs != null && bossRoomPrefabs.Length > 0)
        {
            // Ambil acak dari list variasi boss biasa
            int randomIndex = UnityEngine.Random.Range(0, bossRoomPrefabs.Length);
            selectedPrefab = bossRoomPrefabs[randomIndex];
            Debug.Log($"[DungeonManager] Memilih variasi boss room index ke-{randomIndex}.");
        }

        if (selectedPrefab == null)
        {
            Debug.LogError("[DungeonManager] Prefab Boss Room belum di-assign di Inspector!");
            return;
        }

        // 2. Spawn prefab boss room
        GameObject bossRoomObj = Instantiate(selectedPrefab, Vector3.zero, Quaternion.identity);
        bossRoomObj.name = (currentFloor == 25) ? "FinalBossRoom" : "BossRoom";



        RoomController roomCtrl = bossRoomObj.GetComponent<RoomController>();
        if (roomCtrl != null)
        {
            roomCtrl.roomType = RoomType.Bottom;
            roomCtrl.GridPosition = Vector2Int.zero;
        }
    }

    public void RefreshRoomEnemyCounter(RoomType type, GameObject roomObject)
    {
        EnemySpawner spawner = roomObject.GetComponentInChildren<EnemySpawner>();

        if (spawner != null)
        {
            maxEnemiesInRoom = spawner.spawnCount;
        }
        else
        {
            maxEnemiesInRoom = 0;
        }

        currentEnemiesInRoom = maxEnemiesInRoom;

        Debug.Log($"[DungeonManager] Room {type} sukses disinkronkan. Total musuh: {maxEnemiesInRoom}");

        if (maxEnemiesInRoom > 0)
        {
            if (roomUIBar != null)
            {
                roomUIBar.ShowBar(true);

                if (FloorManager.Instance != null && FloorManager.Instance.IsBossFloor)
                {
                    // Teks UI kustom untuk lantai 25
                    if (FloorManager.Instance.CurrentFloor == 25)
                        roomUIBar.UpdateRoomText("FINAL BOSS");
                    else
                        roomUIBar.UpdateRoomText("Boss Room");
                }
                else
                {
                    roomUIBar.UpdateRoomText(type.ToString());
                }

                roomUIBar.UpdateBarValue(currentEnemiesInRoom, maxEnemiesInRoom);
            }

            if (currentRoom != null && currentRoom.exitDoors != null)
            {
                foreach (var door in currentRoom.exitDoors)
                {
                    if (door != null)
                    {
                        door.LockDoor();
                        Debug.Log($"[DungeonManager] Kunci pengaman dipaksa AKTIF pada: {door.name}");
                    }
                }
            }
        }
        else
        {
            if (roomUIBar != null) roomUIBar.ShowBar(false);
            OpenExitDoorsOfRoom(type);
        }
    }

    public void OnEnemyKilled()
    {
        currentEnemiesInRoom--;
        if (currentEnemiesInRoom < 0) currentEnemiesInRoom = 0;

        if (roomUIBar != null)
        {
            roomUIBar.UpdateBarValue(currentEnemiesInRoom, maxEnemiesInRoom);
        }
    }

    private void UpdateFloorUI()
    {
        if (floorUIText != null)
        {
            int floorSekarang = FloorManager.Instance != null ? FloorManager.Instance.CurrentFloor : 1;
            if (floorSekarang == 25)
            {
                floorUIText.text = "FINAL FLOOR";
            }
            else if (floorSekarang % 4 == 0)
            {
                floorUIText.text = "Boss Floor ";
            }
            else
            {
                floorUIText.text = "Floor " + floorSekarang;
            }
        }
    }

    public void ParentItemToCurrentRoom(Transform itemTransform)
    {
        if (currentRoom != null)
        {
            itemTransform.SetParent(currentRoom.transform);
        }
        else
        {
            Debug.LogWarning("[DungeonManager] Tidak ada currentRoom yang aktif! Barang tidak di-parent.");
        }
    }
}