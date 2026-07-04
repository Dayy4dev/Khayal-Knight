using UnityEngine;

[CreateAssetMenu(fileName = "NewPlayerData", menuName = "Scriptable Objects/Player Data")]
public class HealthData : ScriptableObject
{
    public int maxHP = 100;
    public int currentHP;

    // A helper function to initialize health values easily
    public void ResetHealth()
    {
        currentHP = maxHP;
    }

    public void TakeDamage(int damageAmount)
    {
        currentHP -= damageAmount;
        currentHP = Mathf.Clamp(currentHP, 0, maxHP);
    }
}