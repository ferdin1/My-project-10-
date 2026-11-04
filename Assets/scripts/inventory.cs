using UnityEngine;
using Unity.Netcode;
using System.Collections.Generic;

public class Inventory : NetworkBehaviour
{
    public static Inventory LocalInstance { get; private set; }

    public int maxItems = 10;
    public List<ItemInstance> items = new List<ItemInstance>();
    public int currentSlot = 0;

    public event System.Action OnInventoryChanged;

    [Header("Optional References")]
    [SerializeField] private ItemPlaceholder itemPlaceholder;

    private void Awake()
    {
        // Initialiseer lege slots
        while (items.Count < maxItems)
        {
            items.Add(new ItemInstance());
        }

        EnsureItemPlaceholder();
    }

    private void OnDestroy()
    {
        if (LocalInstance == this)
        {
            LocalInstance = null;
        }
    }

    public void EnsureItemPlaceholder()
    {
        if (itemPlaceholder == null)
        {
            itemPlaceholder = GetComponentInChildren<ItemPlaceholder>();
            if (itemPlaceholder == null)
            {
                // Maak automatisch het child GameObject aan op de speler die spawnt
                GameObject placeholderObj = new GameObject("ItemPlaceholder");
                placeholderObj.transform.SetParent(transform, false);
                placeholderObj.transform.localPosition = new Vector3(0.35f, 0.2f, 0f);

                SpriteRenderer sr = placeholderObj.AddComponent<SpriteRenderer>();
                sr.sortingOrder = 5;

                itemPlaceholder = placeholderObj.AddComponent<ItemPlaceholder>();
            }
        }
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        EnsureItemPlaceholder();

        if (IsOwner)
        {
            LocalInstance = this;
            OnInventoryChanged?.Invoke();
        }

        if (IsServer)
        {
            while (items.Count < maxItems)
            {
                items.Add(new ItemInstance());
            }
        }
    }

    private void Update()
    {
        // Alleen de owning client bestuurt selectie en input
        if (!IsOwner) return;

        float scroll = Input.mouseScrollDelta.y;
        if (scroll != 0)
        {
            if (scroll < 0) currentSlot++;
            else if (scroll > 0) currentSlot--;

            if (currentSlot >= maxItems) currentSlot = 0;
            if (currentSlot < 0) currentSlot = maxItems - 1;

            UpdateHeldItem();
        }

        // Cijfertoetsen 1 t/m 9 en 0 voor directe slot selectie
        for (int i = 0; i < Mathf.Min(maxItems, 9); i++)
        {
            if (Input.GetKeyDown(KeyCode.Alpha1 + i))
            {
                SelectSlot(i);
            }
        }
        if (maxItems >= 10 && Input.GetKeyDown(KeyCode.Alpha0))
        {
            SelectSlot(9);
        }
    }

    public void SelectSlot(int index)
    {
        if (index < 0 || index >= maxItems) return;
        currentSlot = index;
        UpdateHeldItem();
    }

    public void UpdateHeldItem()
    {
        EnsureItemPlaceholder();

        if (itemPlaceholder != null && currentSlot >= 0 && currentSlot < items.Count)
        {
            itemPlaceholder.SetItem(items[currentSlot]);
        }

        OnInventoryChanged?.Invoke();
    }

    // Public wrapper aangeroepen door de lokale client
    public void AddItem(ItemInstance instance)
    {
        if (instance == null || instance.IsEmpty) return;

        if (IsOwner)
        {
            AddItemServerRpc(instance.itemId, instance.amount, instance.durability);
        }
    }

    // Server-side verwerking van item toevoegen
    [ServerRpc(RequireOwnership = false)]
    private void AddItemServerRpc(string itemId, int amount, int durability)
    {
        var incoming = new ItemInstance
        {
            itemId = itemId,
            amount = amount,
            durability = durability,
            enchantments = new List<string>()
        };

        // Probeer eerst te stacken
        if (incoming.amount > 0 && TryStack(incoming)) return;

        // Zoek een leeg slot
        for (int i = 0; i < items.Count; i++)
        {
            if (items[i] == null || items[i].IsEmpty)
            {
                items[i] = incoming;
                UpdateSlotClientRpc(i, incoming);
                return;
            }
        }

        Debug.LogWarning("[Inventory] Inventory is vol!");
    }

    [ClientRpc]
    private void UpdateSlotClientRpc(int index, ItemInstance instance)
    {
        if (!IsOwner) return;

        while (items.Count <= index)
        {
            items.Add(new ItemInstance());
        }

        items[index] = instance;

        if (index == currentSlot)
        {
            UpdateHeldItem();
        }
        else
        {
            OnInventoryChanged?.Invoke();
        }
    }

    private bool TryStack(ItemInstance incoming)
    {
        var data = ItemDatabase.GetItem(incoming.itemId);
        if (data == null || !data.stackable) return false;

        for (int i = 0; i < items.Count; i++)
        {
            var slot = items[i];
            if (slot != null && !slot.IsEmpty && slot.itemId == incoming.itemId)
            {
                int space = data.maxStack - slot.amount;
                if (space <= 0) continue;

                int transfer = Mathf.Min(space, incoming.amount);
                slot.amount += transfer;
                incoming.amount -= transfer;
                items[i] = slot;
                UpdateSlotClientRpc(i, slot);

                if (incoming.amount <= 0) return true;
            }
        }
        return false;
    }
}
