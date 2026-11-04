using UnityEngine;

/// <summary>
/// DE BASISKLASSE VOOR ALLE ITEM-LOGICA.
/// 
/// Hang een script dat overerft van 'ItemBase' aan de prefab van je item (bijv. op je zwaard-prefab).
/// Zodra de speler het item selecteert, spawnt de placeholder dit object en roept automatisch 'Initialize()' aan.
/// Zo heb je direct toegang tot de data van het specifieke item (durability, amount) én de speler.
/// </summary>
public abstract class ItemBase : MonoBehaviour
{
    /// <summary>
    /// De dynamische gegevens van dit specifieke exemplaar (huidige durability, amount, etc.).
    /// </summary>
    public ItemInstance Instance { get; private set; }

    /// <summary>
    /// De statische data uit de ItemDatabase (naam, maxDurability, etc.).
    /// </summary>
    public ItemData Data { get; private set; }

    /// <summary>
    /// Verwijzing naar de inventory van de speler die dit object vasthoudt.
    /// </summary>
    public Inventory PlayerInventory { get; private set; }

    /// <summary>
    /// Verwijzing naar de placeholder waar dit object in zit.
    /// </summary>
    public ItemPlaceholder Holder { get; private set; }

    /// <summary>
    /// Wordt automatisch aangeroepen door de ItemPlaceholder zodra het object in de hand/boven het hoofd spawnt.
    /// </summary>
    public virtual void Initialize(ItemInstance instance, ItemData data, Inventory inventory, ItemPlaceholder holder)
    {
        Instance = instance;
        Data = data;
        PlayerInventory = inventory;
        Holder = holder;
    }

    /// <summary>
    /// Wordt aangeroepen wanneer de speler met de linkermuisknop klikt (bijv. aanvallen, blok plaatsen, eten).
    /// Overschrijf deze functie in jouw eigen script!
    /// </summary>
    public virtual void OnUse()
    {
        // Kan worden overschreven in subklassen (bijv. zwaard, bouwblok)
    }

    /// <summary>
    /// Wordt aangeroepen bij een rechtermuisklik (bijv. blokkeren met zwaard, rotatie van een bouwblok).
    /// </summary>
    public virtual void OnSecondaryUse()
    {
        // Optioneel te overschrijven
    }

    /// <summary>
    /// Handige helper om durability van dit item te verlagen.
    /// Zodra durability op 0 komt, kan het item breken.
    /// </summary>
    public void DamageDurability(int amount = 1)
    {
        if (Instance == null || Data == null || Data.maxDurability <= 0) return;

        Instance.durability -= amount;
        Debug.Log($"[{Data.itemName}] Durability verminderd met {amount}. Huidige durability: {Instance.durability}/{Data.maxDurability}");

        if (Instance.durability <= 0)
        {
            OnItemBroken();
        }
    }

    /// <summary>
    /// Wordt aangeroepen wanneer de durability van het item 0 bereikt.
    /// </summary>
    protected virtual void OnItemBroken()
    {
        Debug.Log($"💥 Het item {Data.itemName} is gebroken!");
        if (Holder != null)
        {
            Holder.Clear();
        }
    }
}
