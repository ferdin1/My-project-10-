using Unity.Netcode;
using UnityEngine;
using System.Collections.Generic;

[RequireComponent(typeof(NetworkObject))]
public class oppak : NetworkBehaviour
{
    [Header("Item Config")]
    [Tooltip("ID van het item dat moet worden opgepakt (moet bestaan in ItemDatabase)")]
    public string itemId = "iron_sword";
    public int amount = 1;
    public int customDurability = -1; // -1 = maxDurability uit ItemData

    private void OnTriggerEnter2D(Collider2D other)
    {
        // Alleen spelers mogen items oppakken
        if (!other.CompareTag("Player")) return;

        // Controleer of dit de lokale speler is (Owner)
        NetworkObject playerNetObj = other.GetComponent<NetworkObject>();
        if (playerNetObj == null || !playerNetObj.IsLocalPlayer) return;

        // Zoek de ItemData op in de ItemDatabase
        ItemData data = ItemDatabase.GetItem(itemId);
        if (data == null)
        {
            Debug.LogWarning($"[oppak] ItemData met id '{itemId}' niet gevonden in ItemDatabase!");
            return;
        }

        // Maak een nieuwe ItemInstance op basis van ItemData
        var instance = new ItemInstance
        {
            itemId = data.id,
            amount = amount,
            durability = customDurability >= 0 ? customDurability : data.maxDurability,
            enchantments = new List<string>()
        };

        // 1. Voeg toe aan inventory
        Inventory inventory = other.GetComponent<Inventory>();
        if (inventory != null)
        {
            inventory.AddItem(instance);
        }

        // 2. Als de speler een ItemPlaceholder heeft en die is leeg, plaats het er direct in
        ItemPlaceholder placeholder = other.GetComponentInChildren<ItemPlaceholder>();
        if (placeholder != null && !placeholder.HasItem)
        {
            placeholder.SetItem(instance);
        }

        // 3. Verberg / despawn het pickup object voor iedereen over het netwerk
        PakOpServerRpc();
    }

    [ServerRpc(RequireOwnership = false)]
    void PakOpServerRpc()
    {
        GetComponent<NetworkObject>().Despawn();
    }
}