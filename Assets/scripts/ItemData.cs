using UnityEngine;

/// <summary>
/// Bepaalt waar het item fysiek geplaatst moet worden op de speler wanneer het vastgehouden wordt.
/// </summary>
public enum HoldPosition
{
    Hand,       // Standaard in de hand (bijv. zwaard, boog, pikhouweel, gereedschap)
    OverHead    // Boven het hoofd (bijv. bouwbare blokken, zware kratten, grote stenen)
}

[CreateAssetMenu(fileName = "NewItemData", menuName = "Inventory/Item Data")]
public class ItemData : ScriptableObject
{
    [Header("Basis Informatie")]
    [Tooltip("Unieke code voor dit item, bijv. 'iron_sword' of 'wood_block'.")]
    public string id;

    [Tooltip("De naam die in de UI getoond wordt.")]
    public string itemName;

    [TextArea] 
    public string description;

    [Header("Visuele Weergave")]
    [Tooltip("Het 2D icoontje voor in de UI.")]
    public Sprite icon;

    [Tooltip("Optionele sprite voor als je geen prefab gebruikt.")]
    public Sprite holdItem;

    [Header("Eigenschappen")]
    [Tooltip("Maximale duurzaamheid. 0 betekent dat het item niet stuk kan.")]
    public int maxDurability = 0;

    [Tooltip("Mag dit item gestapeld worden in 1 slot?")]
    public bool stackable = false;

    [Tooltip("Maximale aantal per slot als het stackable is.")]
    public int maxStack = 1;

    [Header("Item Logica & Prefab")]
    [Tooltip("Waar op de speler moet dit object gehouden worden?")]
    public HoldPosition holdPosition = HoldPosition.Hand;

    [Tooltip("Hét fysieke GameObject (prefab) met jouw eigen logica-script (zoals SwordItem, BuildingItem). Wordt in de speler gespawnd!")]
    public GameObject itemPrefab;
}
