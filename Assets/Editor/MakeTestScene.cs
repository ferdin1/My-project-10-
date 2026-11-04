using UnityEngine;
using UnityEditor;
using System.IO;
using System.Collections.Generic;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// EDITOR SCRIPT VOOR HET MAKEN VAN EEN TEST SCENE MET ITEM PREFABS EN DATABASE.
/// 
/// Bereikbaar in Unity via het menu:
/// Tools -> Maak Test Scene (Inventory & Database)
/// </summary>
public static class MakeTestScene
{
    [MenuItem("Tools/Maak Test Scene (Inventory & Database)")]
    public static void CreateInventoryTestScene()
    {
        // 1. Zorg dat de benodigde mappen bestaan
        string dataFolder = "Assets/Resources/ItemData";
        string prefabFolder = "Assets/Resources/Prefabs";

        if (!Directory.Exists(dataFolder)) Directory.CreateDirectory(dataFolder);
        if (!Directory.Exists(prefabFolder)) Directory.CreateDirectory(prefabFolder);
        AssetDatabase.Refresh();

        // 2. Maak de fysieke Prefabs voor in de hand / boven het hoofd (indien ze nog niet bestaan)
        GameObject swordPrefab = GetOrCreateSwordPrefab(prefabFolder);
        GameObject blockPrefab = GetOrCreateBlockPrefab(prefabFolder);

        // 3. Maak of laad ItemData ScriptableObjects
        List<ItemData> createdItems = new List<ItemData>();

        // Zwaard: Wordt in de HAND gehouden en heeft SwordItem logica
        createdItems.Add(GetOrCreateItemData(
            "iron_sword", 
            "Iron Sword", 
            "Een scherp zwaard in de hand. Klik links om aan te vallen!", 
            100, 
            false, 
            1, 
            HoldPosition.Hand, 
            swordPrefab, 
            new Color(0.8f, 0.85f, 0.95f)));

        // Bouwblok: Wordt BOVEN HET HOOFD gehouden en heeft BuildingItem logica
        createdItems.Add(GetOrCreateItemData(
            "stone_block", 
            "Stone Block", 
            "Een zwaar bouwblok dat boven het hoofd wordt gedragen. Klik links om te plaatsen!", 
            0, 
            true, 
            64, 
            HoldPosition.OverHead, 
            blockPrefab, 
            new Color(0.6f, 0.6f, 0.65f)));

        // Diamant: Normaal verzamelbaar item (fallback sprite)
        createdItems.Add(GetOrCreateItemData(
            "diamond", 
            "Diamond", 
            "Zeldzame edelsteen, stapelbaar tot 64.", 
            0, 
            true, 
            64, 
            HoldPosition.Hand, 
            null, 
            new Color(0.2f, 0.8f, 1f)));

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        // 4. Maak of zoek de ItemDatabase in de huidige scene
        ItemDatabase db = Object.FindFirstObjectByType<ItemDatabase>();
        if (db == null)
        {
            GameObject dbObj = new GameObject("ItemDatabase");
            db = dbObj.AddComponent<ItemDatabase>();
            Undo.RegisterCreatedObjectUndo(dbObj, "Create ItemDatabase");
        }

        db.items = new List<ItemData>(createdItems);
        db.RebuildLookup();
        EditorUtility.SetDirty(db);

        // 5. Verwijder per ongeluk achtergebleven dummy scene-players
        // De echte speler spawnt via Netcode (Square 1.prefab)!
        GameObject dummyPlayer = GameObject.Find("Player");
        if (dummyPlayer != null && !dummyPlayer.name.Contains("(Clone)"))
        {
            Undo.DestroyObjectImmediate(dummyPlayer);
            Debug.Log("[MakeTestScene] Dummy scene-speler opgeruimd. De speler spawnt via Netcode!");
        }

        // 6. Maak of update de Pickup test objecten in de scene
        CreatePickupObject("Pickup_IronSword", new Vector3(-2.5f, 1.5f, 0f), "iron_sword", 1, 100, createdItems[0].icon);
        CreatePickupObject("Pickup_StoneBlock", new Vector3(0f, 2f, 0f), "stone_block", 5, 0, createdItems[1].icon);
        CreatePickupObject("Pickup_Diamond", new Vector3(2.5f, 1.5f, 0f), "diamond", 10, 0, createdItems[2].icon);

        // 7. Koppel de InventoryUI aan de scene
        CreateOrUpdateInventoryUI();

        Debug.Log("✅ Test Scene succesvol ingericht!");
        Debug.Log("📌 InventoryUI is gekoppeld en toont lokaal de hotbar.");
        Debug.Log("📌 'iron_sword' spawnt fysiek in de HAND met SwordItem (zwaait bij linkermuisklik).");
        Debug.Log("📌 'stone_block' spawnt fysiek BOVEN HET HOOFD met BuildingItem (plaatst bij linkermuisklik).");
    }

    private static void CreateOrUpdateInventoryUI()
    {
        // 1. Ruim eventueel oud leeg InventoryUI object op als het geen Canvas is
        GameObject oldUI = GameObject.Find("InventoryUI");
        if (oldUI != null && oldUI.GetComponent<Canvas>() == null)
        {
            Undo.DestroyObjectImmediate(oldUI);
        }

        // 2. Zoek of maak het InventoryCanvas
        GameObject canvasObj = GameObject.Find("InventoryCanvas");
        if (canvasObj != null)
        {
            Undo.DestroyObjectImmediate(canvasObj);
        }

        canvasObj = new GameObject("InventoryCanvas");
        Undo.RegisterCreatedObjectUndo(canvasObj, "Create InventoryCanvas");

        Canvas canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;

        CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;

        canvasObj.AddComponent<GraphicRaycaster>();

        // 3. Zorg dat er een EventSystem is
        if (Object.FindFirstObjectByType<UnityEngine.EventSystems.EventSystem>() == null)
        {
            GameObject esObj = new GameObject("EventSystem");
            esObj.AddComponent<UnityEngine.EventSystems.EventSystem>();
            esObj.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
            Undo.RegisterCreatedObjectUndo(esObj, "Create EventSystem");
        }

        // 4. Maak het Hotbar paneel onderaan het scherm
        GameObject hotbarObj = new GameObject("HotbarPanel");
        hotbarObj.transform.SetParent(canvasObj.transform, false);
        RectTransform hotbarRect = hotbarObj.AddComponent<RectTransform>();
        hotbarRect.anchorMin = new Vector2(0.5f, 0f);
        hotbarRect.anchorMax = new Vector2(0.5f, 0f);
        hotbarRect.pivot = new Vector2(0.5f, 0f);
        hotbarRect.anchoredPosition = new Vector2(0f, 18f);
        hotbarRect.sizeDelta = new Vector2(620f, 96f);

        Image hotbarBg = hotbarObj.AddComponent<Image>();
        hotbarBg.color = new Color(0.1f, 0.12f, 0.16f, 0.85f);

        // 5. Maak de SelectedItemText (boven de slots)
        GameObject textObj = new GameObject("SelectedItemText");
        textObj.transform.SetParent(hotbarObj.transform, false);
        RectTransform textRect = textObj.AddComponent<RectTransform>();
        textRect.anchorMin = new Vector2(0.5f, 1f);
        textRect.anchorMax = new Vector2(0.5f, 1f);
        textRect.pivot = new Vector2(0.5f, 1f);
        textRect.anchoredPosition = new Vector2(0f, -6f);
        textRect.sizeDelta = new Vector2(400f, 26f);

        TextMeshProUGUI tmp = textObj.AddComponent<TextMeshProUGUI>();
        tmp.text = "Slot 1 [Leeg]";
        tmp.fontSize = 18;
        tmp.fontStyle = FontStyles.Bold;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = new Color(1f, 0.9f, 0.2f);

        // 6. Maak de Slots Container met HorizontalLayoutGroup
        GameObject slotsContainerObj = new GameObject("SlotsContainer");
        slotsContainerObj.transform.SetParent(hotbarObj.transform, false);
        RectTransform slotsContainerRect = slotsContainerObj.AddComponent<RectTransform>();
        slotsContainerRect.anchorMin = new Vector2(0.5f, 0f);
        slotsContainerRect.anchorMax = new Vector2(0.5f, 0f);
        slotsContainerRect.pivot = new Vector2(0.5f, 0f);
        slotsContainerRect.anchoredPosition = new Vector2(0f, 10f);
        slotsContainerRect.sizeDelta = new Vector2(580f, 52f);

        HorizontalLayoutGroup layout = slotsContainerObj.AddComponent<HorizontalLayoutGroup>();
        layout.spacing = 6;
        layout.childAlignment = TextAnchor.MiddleCenter;
        layout.childControlWidth = false;
        layout.childControlHeight = false;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = false;

        // 7. Maak 10 Slots met InventorySlotUI
        InventorySlotUI[] slotUIs = new InventorySlotUI[10];
        for (int i = 0; i < 10; i++)
        {
            GameObject slotObj = new GameObject($"Slot_{i}");
            slotObj.transform.SetParent(slotsContainerObj.transform, false);

            RectTransform sRect = slotObj.AddComponent<RectTransform>();
            sRect.sizeDelta = new Vector2(50f, 50f);

            Image slotBg = slotObj.AddComponent<Image>();
            slotBg.color = new Color(0.18f, 0.20f, 0.25f, 0.95f);

            Button slotBtn = slotObj.AddComponent<Button>();
            ColorBlock cb = slotBtn.colors;
            cb.highlightedColor = new Color(0.3f, 0.35f, 0.45f);
            slotBtn.colors = cb;

            // Highlight border
            GameObject hlObj = new GameObject("HighlightBorder");
            hlObj.transform.SetParent(slotObj.transform, false);
            RectTransform hlRect = hlObj.AddComponent<RectTransform>();
            hlRect.anchorMin = Vector2.zero;
            hlRect.anchorMax = Vector2.one;
            hlRect.sizeDelta = Vector2.zero;
            Image hlImg = hlObj.AddComponent<Image>();
            hlImg.color = new Color(1f, 0.85f, 0.2f, 0.45f);
            hlObj.SetActive(i == 0);

            // Icon Image
            GameObject iconObj = new GameObject("Icon");
            iconObj.transform.SetParent(slotObj.transform, false);
            RectTransform iconRect = iconObj.AddComponent<RectTransform>();
            iconRect.anchorMin = new Vector2(0.5f, 0.5f);
            iconRect.anchorMax = new Vector2(0.5f, 0.5f);
            iconRect.sizeDelta = new Vector2(38f, 38f);
            Image iconImg = iconObj.AddComponent<Image>();
            iconImg.preserveAspect = true;
            iconObj.SetActive(false);

            // Amount Text
            GameObject amtObj = new GameObject("Amount");
            amtObj.transform.SetParent(slotObj.transform, false);
            RectTransform amtRect = amtObj.AddComponent<RectTransform>();
            amtRect.anchorMin = new Vector2(1f, 0f);
            amtRect.anchorMax = new Vector2(1f, 0f);
            amtRect.pivot = new Vector2(1f, 0f);
            amtRect.anchoredPosition = new Vector2(-4f, 4f);
            amtRect.sizeDelta = new Vector2(30f, 20f);
            TextMeshProUGUI amtTmp = amtObj.AddComponent<TextMeshProUGUI>();
            amtTmp.fontSize = 13;
            amtTmp.fontStyle = FontStyles.Bold;
            amtTmp.alignment = TextAlignmentOptions.BottomRight;
            amtTmp.color = Color.white;
            amtTmp.text = "";

            // Koppel InventorySlotUI component
            InventorySlotUI slotScript = slotObj.AddComponent<InventorySlotUI>();
            slotScript.iconImage = iconImg;
            slotScript.amountText = amtTmp;
            slotScript.highlightBorder = hlObj;
            slotScript.slotButton = slotBtn;

            slotUIs[i] = slotScript;
        }

        // 8. Koppel InventoryUI aan het Canvas
        InventoryUI invUI = canvasObj.AddComponent<InventoryUI>();
        invUI.selectedItemText = tmp;
        invUI.slots = slotUIs;

        EditorUtility.SetDirty(canvasObj);
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
            UnityEngine.SceneManagement.SceneManager.GetActiveScene()
        );
        Debug.Log("[MakeTestScene] Echte Unity UI Canvas aangemaakt met 10 slots!");
    }

    private static GameObject GetOrCreateSwordPrefab(string folder)
    {
        string path = $"{folder}/SwordPrefab.prefab";
        GameObject existing = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        if (existing != null) return existing;

        GameObject temp = new GameObject("SwordPrefab");
        SpriteRenderer sr = temp.AddComponent<SpriteRenderer>();
        sr.sprite = CreateSimpleSprite("sword_model", 20, 20, new Color(0.85f, 0.85f, 0.95f));
        sr.sortingOrder = 6;
        temp.AddComponent<SwordItem>();

        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(temp, path);
        Object.DestroyImmediate(temp);
        return prefab;
    }

    private static GameObject GetOrCreateBlockPrefab(string folder)
    {
        string path = $"{folder}/BlockPrefab.prefab";
        GameObject existing = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        if (existing != null) return existing;

        GameObject temp = new GameObject("BlockPrefab");
        SpriteRenderer sr = temp.AddComponent<SpriteRenderer>();
        sr.sprite = CreateSimpleSprite("block_model", 28, 28, new Color(0.55f, 0.55f, 0.6f));
        sr.sortingOrder = 6;
        temp.AddComponent<BuildingItem>();

        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(temp, path);
        Object.DestroyImmediate(temp);
        return prefab;
    }

    private static ItemData GetOrCreateItemData(
        string id, 
        string name, 
        string description, 
        int maxDurability, 
        bool stackable, 
        int maxStack, 
        HoldPosition holdPos, 
        GameObject prefab, 
        Color color)
    {
        string assetPath = $"Assets/Resources/ItemData/{id}.asset";
        ItemData data = AssetDatabase.LoadAssetAtPath<ItemData>(assetPath);

        if (data == null)
        {
            data = ScriptableObject.CreateInstance<ItemData>();
            data.id = id;
            data.itemName = name;
            data.description = description;
            data.maxDurability = maxDurability;
            data.stackable = stackable;
            data.maxStack = maxStack;
            data.holdPosition = holdPos;
            data.itemPrefab = prefab;
            data.icon = CreateSimpleSprite($"{id}_icon", 24, 24, color);
            data.holdItem = data.icon;

            AssetDatabase.CreateAsset(data, assetPath);
            Debug.Log($"[MakeTestScene] Nieuw ItemData asset aangemaakt: {assetPath}");
        }
        else
        {
            // Update prefab en holdPosition als ze gewijzigd zijn
            data.holdPosition = holdPos;
            if (prefab != null) data.itemPrefab = prefab;
            EditorUtility.SetDirty(data);
        }

        return data;
    }

    private static Sprite CreateSimpleSprite(string spriteName, int width, int height, Color color)
    {
        string spritePath = $"Assets/Resources/ItemData/{spriteName}.png";
        if (File.Exists(spritePath))
        {
            Sprite existing = AssetDatabase.LoadAssetAtPath<Sprite>(spritePath);
            if (existing != null) return existing;
        }

        Texture2D tex = new Texture2D(width, height);
        Color[] pixels = new Color[width * height];
        for (int i = 0; i < pixels.Length; i++)
        {
            pixels[i] = color;
        }
        tex.SetPixels(pixels);
        tex.Apply();

        byte[] bytes = tex.EncodeToPNG();
        File.WriteAllBytes(spritePath, bytes);
        AssetDatabase.ImportAsset(spritePath, ImportAssetOptions.ForceUpdate);

        TextureImporter importer = AssetImporter.GetAtPath(spritePath) as TextureImporter;
        if (importer != null)
        {
            importer.textureType = TextureImporterType.Sprite;
            importer.spritePixelsPerUnit = 32;
            importer.filterMode = FilterMode.Point;
            importer.SaveAndReimport();
        }

        return AssetDatabase.LoadAssetAtPath<Sprite>(spritePath);
    }

    private static GameObject CreatePickupObject(string objName, Vector3 pos, string itemId, int amount, int durability, Sprite icon)
    {
        GameObject pickup = GameObject.Find(objName);
        if (pickup == null)
        {
            pickup = new GameObject(objName);
            Undo.RegisterCreatedObjectUndo(pickup, $"Create {objName}");
        }

        pickup.transform.position = pos;

        SpriteRenderer sr = pickup.GetComponent<SpriteRenderer>();
        if (sr == null) sr = pickup.AddComponent<SpriteRenderer>();
        sr.sprite = icon;
        sr.sortingOrder = 1;

        CircleCollider2D col = pickup.GetComponent<CircleCollider2D>();
        if (col == null) col = pickup.AddComponent<CircleCollider2D>();
        col.isTrigger = true;
        col.radius = 0.5f;

        Unity.Netcode.NetworkObject netObj = pickup.GetComponent<Unity.Netcode.NetworkObject>();
        if (netObj == null) pickup.AddComponent<Unity.Netcode.NetworkObject>();

        oppak pickupScript = pickup.GetComponent<oppak>();
        if (pickupScript == null) pickupScript = pickup.AddComponent<oppak>();
        pickupScript.itemId = itemId;
        pickupScript.amount = amount;
        pickupScript.customDurability = durability;

        return pickup;
    }
}
