using System.Collections.Generic;
using UnityEngine;

public class ItemDatabase : MonoBehaviour
{
    public List<ItemData> items = new List<ItemData>();

    public ItemData GetItem(int itemID)
    {
        Debug.Log("ich bin aangeroept");
        if (itemID < 0 || itemID >= items.Count)
        {
            Debug.LogError("Invalid Item ID!");
            return null;
        }

        return items[itemID];
    }
}