using UnityEngine;

public class EquipableItem : MonoBehaviour
{
    private Collider itemCollider;

    void Start()
    {
        itemCollider = GetComponent<Collider>();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            ItemManager itemManager = other.GetComponent<ItemManager>();
            if (itemManager != null)
            {
                toEquip(itemManager.ItemSocket.transform);
                Debug.Log("Item equipped!");
            }
        }
    }

    void toEquip(Transform equipSocket)
    {
        transform.SetParent(equipSocket, false);

        transform.localPosition = Vector3.zero;
        transform.localRotation = Quaternion.identity;

        if (itemCollider != null)
        {
            itemCollider.enabled = false;
        }
    }
}
