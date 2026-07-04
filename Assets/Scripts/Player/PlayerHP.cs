using UnityEngine;

public class PlayerHP : MonoBehaviour
{
    [Header("Data Asset")]
    public HealthData healthData; // Drag your 'PlayerHPAsset' here

    void Awake()
    {
        // Automatically fills your health data to max on game start
        if (healthData != null)
        {
            healthData.ResetHealth();
        }
    }
}