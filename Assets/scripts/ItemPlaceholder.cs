using UnityEngine;
using UnityEngine.InputSystem;
using Unity.Netcode;

/// <summary>
/// DE ITEM PLACEHOLDER (HAND- EN HOOFD-SLOT VAN DE SPELER).
/// 
/// Dit component hangt als kind onder de Player.
/// In plaats van enkel een sprite, spawnt dit component nu het ECHTE GameObject (de prefab)
/// met al jouw eigen logica (bijv. SwordItem, BuildingItem).
/// 
/// Ook zorgt het voor de juiste positie:
/// - Hand-items (zwaarden, gereedschap) -> naast de speler
/// - Bouwblokken / zware items -> netjes BOVEN HET HOOFD van de speler
/// </summary>
public class ItemPlaceholder : NetworkBehaviour
{
    [Header("Posities op de Speler")]
    [Tooltip("Lokale positie voor items die in de hand worden gehouden (bijv. zwaarden).")]
    public Vector3 handOffset = new Vector3(0.35f, 0.1f, 0f);

    [Tooltip("Lokale positie voor items die boven het hoofd worden gehouden (bijv. bouwbare blokken).")]
    public Vector3 overheadOffset = new Vector3(0f, 0.85f, 0f);

    [Header("Visuele Fallback (als een item geen prefab heeft)")]
    [SerializeField] private SpriteRenderer fallbackSpriteRenderer;

    [Header("Huidig Spel-Object (Read-Only)")]
    [SerializeField] private GameObject currentSpawnedObject;
    [SerializeField] private string currentItemId;

    public ItemInstance CurrentInstance { get; private set; }
    public ItemData CurrentData => !string.IsNullOrEmpty(currentItemId) ? ItemDatabase.GetItem(currentItemId) : null;
    public ItemBase CurrentItemLogic { get; private set; }
    public bool HasItem => CurrentInstance != null && !CurrentInstance.IsEmpty;

    private Inventory playerInventory;

    private void Awake()
    {
        // Zoek fallback SpriteRenderer
        if (fallbackSpriteRenderer == null)
            fallbackSpriteRenderer = GetComponent<SpriteRenderer>();

        // Zoek de Inventory op de speler
        playerInventory = GetComponentInParent<Inventory>();
    }

    /// <summary>
    /// Wordt aangeroepen door de Inventory wanneer je een ander slot kiest of een item oppakt.
    /// </summary>
    public void SetItem(ItemInstance instance)
    {
        // 1. Verwijder eerst het vorige fysieke object
        Clear();

        if (instance == null || instance.IsEmpty)
        {
            return;
        }

        CurrentInstance = instance;
        currentItemId = instance.itemId;

        ItemData data = CurrentData;
        if (data == null)
        {
            Debug.LogWarning($"[ItemPlaceholder] Geen ItemData gevonden voor id: '{instance.itemId}'!");
            return;
        }

        // 2. Bepaal de positie: Hand of Boven het hoofd
        if (data.holdPosition == HoldPosition.OverHead)
        {
            transform.localPosition = overheadOffset;
        }
        else
        {
            transform.localPosition = handOffset;
        }

        // 3. Spawnen van het fysieke GameObject of Fallback Sprite
        if (data.itemPrefab != null)
        {
            // Verberg de fallback sprite
            if (fallbackSpriteRenderer != null) fallbackSpriteRenderer.enabled = false;

            // Spawn het fysieke prefab-object als kind van deze placeholder
            currentSpawnedObject = Instantiate(data.itemPrefab, transform);
            currentSpawnedObject.transform.localPosition = Vector3.zero;
            currentSpawnedObject.transform.localRotation = Quaternion.identity;

            // Initialiseer het logica-script (SwordItem, BuildingItem, etc.)
            CurrentItemLogic = currentSpawnedObject.GetComponent<ItemBase>();
            if (CurrentItemLogic != null)
            {
                CurrentItemLogic.Initialize(CurrentInstance, data, playerInventory, this);
            }
        }
        else
        {
            // FALLBACK: Als je nog geen 3D/2D prefab hebt gemaakt voor dit item, toon dan de sprite!
            if (fallbackSpriteRenderer != null)
            {
                fallbackSpriteRenderer.sprite = data.holdItem != null ? data.holdItem : data.icon;
                fallbackSpriteRenderer.enabled = true;
            }
        }
    }

    /// <summary>
    /// Maakt de hand/hoofd leeg en verwijdert het gespawnde object.
    /// </summary>
    public void Clear()
    {
        if (currentSpawnedObject != null)
        {
            Destroy(currentSpawnedObject);
            currentSpawnedObject = null;
        }

        CurrentItemLogic = null;
        CurrentInstance = null;
        currentItemId = "";

        if (fallbackSpriteRenderer != null)
        {
            fallbackSpriteRenderer.sprite = null;
            fallbackSpriteRenderer.enabled = false;
        }
    }

    private void Update()
    {
        // Alleen de eigenaar van deze speler mag de actieknoppen indrukken
        if (!IsOwner) return;
        if (CurrentItemLogic == null) return;

        // 1. Linkermuisklik: Gebruik item (slaan, bouwen, eten)
        bool leftClicked = false;
        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame) leftClicked = true;
        if (!leftClicked && Input.GetMouseButtonDown(0)) leftClicked = true;

        if (leftClicked)
        {
            CurrentItemLogic.OnUse();
        }

        // 2. Rechtermuisklik: Secundaire actie (blokkeren, roteren)
        bool rightClicked = false;
        if (Mouse.current != null && Mouse.current.rightButton.wasPressedThisFrame) rightClicked = true;
        if (!rightClicked && Input.GetMouseButtonDown(1)) rightClicked = true;

        if (rightClicked)
        {
            CurrentItemLogic.OnSecondaryUse();
        }
    }

    // Handige hulpmethodes voor UI / HUD weergave
    public string GetItemName() => CurrentData != null ? CurrentData.itemName : "Empty";
    public int GetDurability() => CurrentInstance != null ? CurrentInstance.durability : 0;
    public int GetMaxDurability() => CurrentData != null ? CurrentData.maxDurability : 0;
    public float GetDurabilityPercentage() => (GetMaxDurability() > 0) ? (float)GetDurability() / GetMaxDurability() : 1f;
}
