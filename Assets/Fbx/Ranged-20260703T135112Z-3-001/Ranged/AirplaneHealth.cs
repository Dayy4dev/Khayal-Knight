using UnityEngine;

// Contoh sederhana untuk objek airplane, supaya bisa "ditembak"
// Sesuaikan/hapus kalau airplane-mu sudah punya sistem health sendiri.
public class AirplaneHealth : MonoBehaviour, IAirplaneDamageable
{
    public int maxHealth = 50;
    private int currentHealth;

    void Awake()
    {
        currentHealth = maxHealth;
    }

    public void TakeDamage(int amount)
    {
        currentHealth -= amount;
        if (currentHealth <= 0)
        {
            // Ganti dengan efek ledakan / animasi jatuh sesuai game-mu
            Destroy(gameObject);
        }
    }
}
