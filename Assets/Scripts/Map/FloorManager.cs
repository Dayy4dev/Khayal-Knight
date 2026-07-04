using UnityEngine;

public class FloorManager : MonoBehaviour
{
    public static FloorManager Instance { get; private set; }

    // Floor saat ini, mulai dari 1
    public int CurrentFloor { get; private set; } = 1;

    // UBAH DI SINI: Deteksi kelipatan 4 ATAU floor 25 sebagai Boss Floor[cite: 4]
    public bool IsBossFloor => (CurrentFloor % 4 == 0) || (CurrentFloor == 25);

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    public void AdvanceFloor()
    {
        CurrentFloor++;
        Debug.Log($"[FloorManager] Floor {CurrentFloor}" + (IsBossFloor ? " — BOSS FLOOR!" : ""));
    }

    public void ResetFloor()
    {
        CurrentFloor = 1;
        Debug.Log("[FloorManager] Floor direset ke 1.");
    }

    public float GetHPMultiplier() { return 1f + (CurrentFloor - 1) * 0.3f; }
    public float GetDamageMultiplier() { return 1f + (CurrentFloor - 1) * 0.2f; }
    public float GetSpeedMultiplier() { return Mathf.Min(1f + (CurrentFloor - 1) * 0.1f, 1.5f); }
}