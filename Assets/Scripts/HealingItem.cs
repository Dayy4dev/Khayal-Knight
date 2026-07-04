using UnityEngine;

public class HealingItem : MonoBehaviour
{
    public int healAmount = 20;

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            ItemManager itemManager = other.GetComponent<ItemManager>();
            if (itemManager != null)
            {
                itemManager.currentHealth += healAmount;
                itemManager.Heal();
                gameObject.SetActive(false);
            }
        }
    }
}
