using UnityEngine;
using System.Collections.Generic;

[AddComponentMenu("Inventory/Item Database")]
public class ItemDatabase : MonoBehaviour
{
    public static ItemDatabase Instance { get; private set; }

    [Tooltip("Alle ItemData ScriptableObjects in de game.")]
    public List<ItemData> items = new();

    private Dictionary<string, ItemData> _lookup;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        RebuildLookup();
    }

    public void RebuildLookup()
    {
        _lookup = new Dictionary<string, ItemData>(items.Count);
        foreach (var data in items)
        {
            if (data != null && !string.IsNullOrEmpty(data.id))
            {
                _lookup[data.id] = data;
            }
        }
    }

    public ItemData GetItemById(string id)
    {
        if (string.IsNullOrEmpty(id)) return null;
        if (_lookup == null) RebuildLookup();
        _lookup.TryGetValue(id, out var data);
        return data;
    }

    /// <summary>
    /// Statische hulpmethode om overal in code eenvoudig een ItemData op te halen via id.
    /// </summary>
    public static ItemData GetItem(string id)
    {
        if (string.IsNullOrEmpty(id)) return null;

        if (Instance == null)
        {
            // Zoek database in de scene als Awake nog niet is aangeroepen
            Instance = Object.FindFirstObjectByType<ItemDatabase>();
        }

        return Instance != null ? Instance.GetItemById(id) : null;
    }
}
