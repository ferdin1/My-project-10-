using UnityEngine;
using TMPro;

/// <summary>
/// Beheert de overkoepelende Inventory UI (Hotbar Canvas).
/// Luistert naar het Inventory script van de lokale speler en stuurt de slots aan.
/// </summary>
public class InventoryUI : MonoBehaviour
{
    [Header("Referenties")]
    public Inventory inventory;
    public TMP_Text selectedItemText;
    public InventorySlotUI[] slots;

    private void Start()
    {
        FindInventory();
    }

    private void OnEnable()
    {
        if (inventory == null) FindInventory();
        if (inventory != null)
        {
            inventory.OnInventoryChanged += UpdateUI;
            UpdateUI();
        }
    }

    private void OnDisable()
    {
        if (inventory != null)
        {
            inventory.OnInventoryChanged -= UpdateUI;
        }
    }

    private void Update()
    {
        // Zoek de lokale speler zodra deze spawned
        if (inventory == null)
        {
            FindInventory();
        }
    }

    public void FindInventory()
    {
        if (inventory != null) return;

        if (Inventory.LocalInstance != null)
        {
            SetInventory(Inventory.LocalInstance);
            return;
        }

        Inventory found = Object.FindFirstObjectByType<Inventory>();
        if (found != null)
        {
            SetInventory(found);
        }
    }

    public void SetInventory(Inventory target)
    {
        if (inventory != null)
        {
            inventory.OnInventoryChanged -= UpdateUI;
        }

        inventory = target;

        if (inventory != null)
        {
            inventory.OnInventoryChanged += UpdateUI;

            if (slots != null)
            {
                for (int i = 0; i < slots.Length; i++)
                {
                    if (slots[i] != null)
                    {
                        slots[i].Init(i, inventory);
                    }
                }
            }

            UpdateUI();
        }
    }

    public void UpdateUI()
    {
        if (inventory == null) return;

        // 1. Toon naam van het actieve item boven de hotbar
        if (selectedItemText != null)
        {
            if (inventory.currentSlot >= 0 && inventory.currentSlot < inventory.items.Count)
            {
                var curItem = inventory.items[inventory.currentSlot];
                if (curItem != null && !curItem.IsEmpty)
                {
                    var data = ItemDatabase.GetItem(curItem.itemId);
                    string itemName = (data != null && !string.IsNullOrEmpty(data.itemName)) ? data.itemName : curItem.itemId;
                    selectedItemText.text = $"{itemName} (Slot {inventory.currentSlot + 1})";
                }
                else
                {
                    selectedItemText.text = $"Slot {inventory.currentSlot + 1} [Leeg]";
                }
            }
        }

        // 2. Update alle slot weergaven
        if (slots != null)
        {
            for (int i = 0; i < slots.Length; i++)
            {
                if (slots[i] == null) continue;

                ItemInstance item = (i < inventory.items.Count) ? inventory.items[i] : null;
                bool isSelected = (i == inventory.currentSlot);
                slots[i].Set(item, isSelected);
            }
        }
    }
}
