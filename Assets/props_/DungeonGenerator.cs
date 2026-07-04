using System.Collections.Generic;
using UnityEngine;

public class DungeonGenerator : MonoBehaviour
{
    [Header("Room Variants (isi 1-3 prefab per zona)")]
    public GameObject[] bottomRoomVariants;  // Spawn room
    public GameObject[] centerRoomVariants;  // Room tengah
    public GameObject[] leftRoomVariants;    // Pilihan kiri
    public GameObject[] rightRoomVariants;   // Pilihan kanan
    public GameObject[] topRoomVariants;     // Pilihan atas

    [Header("Settings")]
    public float roomSize = 30f;

    private Dictionary<Vector2Int, GameObject> spawnedRooms = new Dictionary<Vector2Int, GameObject>();

    // Dipanggil oleh DungeonManager, bukan Start()
    public void GenerateDungeon()
    {
        spawnedRooms.Clear();

        CreateRoom(GetRandomVariant(bottomRoomVariants), new Vector2Int(0,  0), RoomType.Bottom);
        CreateRoom(GetRandomVariant(centerRoomVariants), new Vector2Int(0,  1), RoomType.Center);
        CreateRoom(GetRandomVariant(leftRoomVariants),   new Vector2Int(-1, 1), RoomType.Left);
        CreateRoom(GetRandomVariant(rightRoomVariants),  new Vector2Int(1,  1), RoomType.Right);
        CreateRoom(GetRandomVariant(topRoomVariants),    new Vector2Int(0,  2), RoomType.Top);

        Debug.Log("[DungeonGenerator] Dungeon berhasil di-generate!");
    }

    GameObject GetRandomVariant(GameObject[] variants)
    {
        if (variants == null || variants.Length == 0)
        {
            Debug.LogWarning("[DungeonGenerator] Array variasi kosong!");
            return null;
        }

        List<GameObject> valid = new List<GameObject>();
        foreach (var v in variants)
            if (v != null) valid.Add(v);

        if (valid.Count == 0)
        {
            Debug.LogWarning("[DungeonGenerator] Semua slot variasi null!");
            return null;
        }

        return valid[Random.Range(0, valid.Count)];
    }

    void CreateRoom(GameObject prefab, Vector2Int gridPos, RoomType roomType)
    {
        if (prefab == null)
        {
            Debug.LogWarning($"[DungeonGenerator] Prefab null untuk {roomType}!");
            return;
        }

        if (spawnedRooms.ContainsKey(gridPos))
        {
            Debug.LogError($"[DungeonGenerator] Grid {gridPos} sudah terisi!");
            return;
        }

        Vector3 worldPos = new Vector3(gridPos.x * roomSize, 0, gridPos.y * roomSize);
        GameObject roomObj = Instantiate(prefab, worldPos, Quaternion.identity);
        roomObj.name = $"{roomType}_Room [{gridPos.x},{gridPos.y}]";

        RoomController roomCtrl = roomObj.GetComponent<RoomController>();
        if (roomCtrl != null)
        {
            roomCtrl.roomType = roomType;
            roomCtrl.GridPosition = gridPos;
            roomCtrl.ZoneName = roomType.ToString();
        }
        else
        {
            Debug.LogWarning($"[DungeonGenerator] {roomObj.name} tidak punya RoomController!");
        }

        spawnedRooms.Add(gridPos, roomObj);
    }
}