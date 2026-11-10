using UnityEngine;

public class Holder : MonoBehaviour
{
    public Transform spawnPoint;

    private GameObject currentObject;

    public void SetItem(ItemData item)
    {
        if (currentObject != null)
        {
            Destroy(currentObject);
            currentObject = null;
        }

        if (item == null || item.itemPrefab == null)
            return;

        // Spawn als child van SpawnPoint
        currentObject = Instantiate(
            item.itemPrefab,
            spawnPoint
        );

        // Lokale positie
        currentObject.transform.localPosition = Vector3.zero;
        currentObject.transform.localRotation = Quaternion.identity;
        currentObject.transform.localScale = Vector3.one;
        
    }
}