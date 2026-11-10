using System;
using System.Collections.Generic;
using UnityEngine;

public class inventory : MonoBehaviour
{
    public List<ItemData> items = new List<ItemData>();
    public Transform prefab;
    public int maxinventorysize;

    [SerializeField] private ItemDatabase database;
    [SerializeField] private Holder holder;
    private void Awake()
    {
        database = FindFirstObjectByType<ItemDatabase>();        holder = GetComponent<Holder>();
        if (holder == null)
        {Debug.Log("holder not found!");}
        // Maak lege inventory-slots aan
        for (int i = 0; i < maxinventorysize; i++)
        {
            items.Add(null);
        }

        if (database == null)
        {
            Debug.LogError("ItemDatabase not found!");
        }
    }

    public void sendtoinventory(int itemID)
    {
        Debug.Log("SEND TO INVENTORY AANGEROEPEN!");

        if (database == null)
        {
            Debug.LogError("Database is null!");
            return;
        }

        // Haal item op uit database
        ItemData item = database.GetItem(itemID);

        if (item == null)
            return;

        // Zoek eerste lege slot
        for (int i = 0; i < items.Count; i++)
        {
            if (items[i] == null)
            {
                // Maak instantie van ScriptableObject
                ItemData newItem = Instantiate(item);

                // Vul lege slot
                items[i] = newItem;

                Debug.Log("Item added to slot: " + i);
                Debug.Log("Item name: " + newItem.name);

                return;
            }
        }

        Debug.Log("Inventory is full!");
    }

    int hotbar = 0;

    void Update()
    {
        // Scrollwiel
        float scroll = Input.GetAxis("Mouse ScrollWheel");

        if (scroll > 0f)
        {
            hotbar++;
            veranderd();
        }
        else if (scroll < 0f)
        {
            hotbar--;
            veranderd();
        }

        // Toetsen 1 t/m 9
        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            hotbar = 0;
            veranderd();
        }
        else if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            hotbar = 1;
            veranderd();
        }
        else if (Input.GetKeyDown(KeyCode.Alpha3))
        {
            hotbar = 2;
            veranderd();
        }
        else if (Input.GetKeyDown(KeyCode.Alpha4))
        {
            hotbar = 3;
            veranderd();
        }
        else if (Input.GetKeyDown(KeyCode.Alpha5))
        {
            hotbar = 4;
            veranderd();
        }
        else if (Input.GetKeyDown(KeyCode.Alpha6))
        {
            hotbar = 5;
            veranderd();
        }
        else if (Input.GetKeyDown(KeyCode.Alpha7))
        {
            hotbar = 6;
            veranderd();
        }
        else if (Input.GetKeyDown(KeyCode.Alpha8))
        {
            hotbar = 7;
            veranderd();
        }
        else if (Input.GetKeyDown(KeyCode.Alpha9))
        {
            hotbar = 8;
            veranderd();
        }
    }

    public void veranderd()
    {
        if (hotbar >= items.Count)
            hotbar = 0;

        if (hotbar < 0)
            hotbar = items.Count - 1;

        ItemData selectedItem = items[hotbar];

        Debug.Log("Hotbar index: " + hotbar);

        if (selectedItem == null)
        {
            Debug.Log("Slot " + hotbar + " is leeg!");
        }
        else
        {
            Debug.Log("Item gevonden: " + selectedItem.name);
            Debug.Log("Prefab: " + selectedItem.itemPrefab);
        }

        holder.SetItem(selectedItem);
    }
}
