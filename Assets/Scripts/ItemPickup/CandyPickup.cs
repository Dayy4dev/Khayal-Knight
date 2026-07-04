using UnityEngine;

/// <summary>
/// Item candy di dunia. Player cukup menabrak untuk heal.
/// Gunakan CandyData ScriptableObject untuk mengatur healAmount yang berbeda-beda.
/// </summary>
public class CandyPickup : MonoBehaviour
{
    // -------------------------------------------------------------------------
    // Inspector
    // -------------------------------------------------------------------------

    [Header("Data")]
    [SerializeField] private CandyData candyData;

    [Header("Animation")]
    [SerializeField] private bool enableFloat     = true;
    [SerializeField] private float floatAmplitude = 0.2f;
    [SerializeField] private float floatSpeed     = 2f;
    [SerializeField] private Vector3 rotationSpeed = new Vector3(0f, 90f, 0f);

    [Header("VFX / SFX (opsional)")]
    [SerializeField] private GameObject pickupEffect;

    // -------------------------------------------------------------------------
    // State
    // -------------------------------------------------------------------------

    private float startY;
    private bool pickedUp;

    // -------------------------------------------------------------------------
    // Unity Lifecycle
    // -------------------------------------------------------------------------

    void Awake()
    {
        startY = transform.position.y;

        if (candyData == null)
            Debug.LogWarning($"[CandyPickup] {gameObject.name} tidak punya CandyData!");
    }

    void Update()
    {
        if (!enableFloat || pickedUp) return;

        transform.Rotate(rotationSpeed * Time.deltaTime);
        float newY = startY + Mathf.Sin(Time.time * floatSpeed) * floatAmplitude;
        transform.position = new Vector3(transform.position.x, newY, transform.position.z);
    }

    void OnTriggerEnter(Collider other)
    {
        if (pickedUp) return;
        if (candyData == null) return;

        // Deteksi player via tag atau komponen
        if (!other.CompareTag("Player")) return;

        PlayerHealth health = other.GetComponent<PlayerHealth>();
        if (health == null) return;

        pickedUp = true;

        health.Heal(candyData.healAmount);
        Debug.Log($"[CandyPickup] Player healed {candyData.healAmount} HP dari {candyData.candyName}.");

        // Spawn efek pickup jika ada
        if (pickupEffect != null)
            Instantiate(pickupEffect, transform.position, Quaternion.identity);

        Destroy(gameObject);
    }
}