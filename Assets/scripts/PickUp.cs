using UnityEngine;

public class PickUp : MonoBehaviour
{
    private inventory inventory;

    private void Awake()
    {
        inventory = GetComponent<inventory>();
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Item"))
        {
            Pak pak = collision.gameObject.GetComponent<Pak>();

            if (pak == null || pak.pickupData == null)
            {
                return;
            }

            int itemID = pak.pickupData.id;

            Debug.Log("[PICKUP] Item ID: " + itemID);

            inventory.sendtoinventory(itemID);
        }
    }
}