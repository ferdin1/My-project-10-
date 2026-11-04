using UnityEngine;

/// <summary>
/// VOORBEELD: EEN BOUWBAAR BLOK / ITEM BOVEN HET HOOFD
/// 
/// In ItemData staat voor dit item: 'holdPosition = HoldPosition.OverHead'.
/// De ItemPlaceholder plaatst dit object dus automatisch netjes boven het hoofd van de speler!
/// </summary>
public class BuildingItem : ItemBase
{
    [Header("Bouw Instellingen")]
    [Tooltip("De prefab die in de speelwereld geplaatst moet worden wanneer je bouwt.")]
    public GameObject worldBlockPrefab;

    /// <summary>
    /// Wordt aangeroepen bij linkermuisklik: plaats het blok in de wereld!
    /// </summary>
    public override void OnUse()
    {
        if (Instance == null || Instance.amount <= 0) return;

        // Bepaal positie voor het blok in de wereld (bijv. 1 meter voor de speler)
        Vector3 placePosition = transform.position + new Vector3(1f, 0f, 0f);

        Debug.Log($"🧱 [{Data.itemName}] Blok geplaatst op positie {placePosition}!");

        // Als er een prefab is voor in de wereld, spawn die
        if (worldBlockPrefab != null)
        {
            Instantiate(worldBlockPrefab, placePosition, Quaternion.identity);
        }

        // Verminder het aantal in de stack van dit specifieke exemplaar
        Instance.amount--;
        Debug.Log($"Resterend aantal blokken: {Instance.amount}");

        // Als alle blokken op zijn, maak de hand/hoofd-placeholder leeg
        if (Instance.amount <= 0)
        {
            if (Holder != null)
            {
                Holder.Clear();
            }
        }
    }

    /// <summary>
    /// Rechtermuisklik: optionele actie, bijv. blok roteren voor het plaatsen
    /// </summary>
    public override void OnSecondaryUse()
    {
        Debug.Log($"🔄 [{Data.itemName}] Blok geroteerd!");
        transform.Rotate(0, 0, 90f);
    }
}
