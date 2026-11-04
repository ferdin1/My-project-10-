using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Beheert een individueel slot in de hotbar UI.
/// Gekoppeld aan echte Unity UI componenten (Image, TextMeshPro, Button).
/// </summary>
public class InventorySlotUI : MonoBehaviour
{
    [Header("UI Componenten")]
    public Image iconImage;
    public TMP_Text amountText;
    public GameObject highlightBorder;
    public Button slotButton;

    private int slotIndex;
    private Inventory targetInventory;

    public void Init(int index, Inventory inv)
    {
        slotIndex = index;
        targetInventory = inv;

        if (slotButton != null)
        {
            slotButton.onClick.RemoveAllListeners();
            slotButton.onClick.AddListener(OnSlotClicked);
        }
    }

    private void OnSlotClicked()
    {
        if (targetInventory != null)
        {
            targetInventory.SelectSlot(slotIndex);
        }
    }

    public void Set(ItemInstance item, bool isSelected)
    {
        if (highlightBorder != null)
        {
            highlightBorder.SetActive(isSelected);
        }

        if (item != null && !item.IsEmpty)
        {
            ItemData data = ItemDatabase.GetItem(item.itemId);
            if (data != null && data.icon != null && iconImage != null)
            {
                iconImage.sprite = data.icon;
                iconImage.color = Color.white;
                iconImage.gameObject.SetActive(true);
            }
            else if (iconImage != null)
            {
                iconImage.gameObject.SetActive(false);
            }

            if (amountText != null)
            {
                amountText.text = item.amount > 1 ? item.amount.ToString() : "";
            }
        }
        else
        {
            if (iconImage != null) iconImage.gameObject.SetActive(false);
            if (amountText != null) amountText.text = "";
        }
    }
}

