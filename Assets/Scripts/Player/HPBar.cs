using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class HPBar : MonoBehaviour
{
    [Header("Dependencies")]
    public PlayerHealth playerHealth;  // Drag your Player object here
    public HealthData healthData;      // Optional fallback (legacy)
    public Image fillImage;            // Drag your 'Health Bar Fill' image here
    public TextMeshProUGUI hpText;     // Drag your UI 'Text (TMP)' here

    void Start()
    {
        // Auto-find PlayerHealth if not assigned in the inspector
        if (playerHealth == null)
        {
            GameObject player = GameObject.FindWithTag("Player");
            if (player != null) playerHealth = player.GetComponent<PlayerHealth>();
        }
    }

    void Update()
    {
        int currentHP = 0;
        int maxHP = 0;

        if (playerHealth != null)
        {
            // DIUBAH: Menggunakan fungsi Getter agar sinkron dengan variabel private di PlayerHealth
            currentHP = playerHealth.GetCurrentHealth();
            maxHP     = playerHealth.GetMaxHealth();
        }
        else if (healthData != null)
        {
            currentHP = healthData.currentHP;
            maxHP     = healthData.maxHP;
        }
        else
        {
            return;
        }

        // 1. Update the horizontal fill slider amount
        if (fillImage != null && maxHP > 0)
        {
            fillImage.fillAmount = (float)currentHP / maxHP;
        }

        // 2. Format and display the health numbers text (e.g., "100 / 100")
        if (hpText != null)
        {
            hpText.text = currentHP + " / " + maxHP;
        }
    }
}