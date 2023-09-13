using System.Collections.Generic;
using UnityEngine;

public class ContainerSlotGroup : MonoBehaviour
{
    [SerializeField] RectTransform rectTransform;

    List<InventorySlot> slots = new List<InventorySlot>();

    public void SetupRectTransform(InventoryLayout inventoryLayout)
    {
        int newWidth = inventoryLayout.MaxSlotsPerRow * InventoryItem.slotSize;
        int newHeight = Mathf.CeilToInt(inventoryLayout.AmountOfSlots / inventoryLayout.MaxSlotsPerRow) * InventoryItem.slotSize;

        rectTransform.sizeDelta = new Vector2(newWidth, newHeight);
    }

    public List<InventorySlot> Slots => slots;
}
