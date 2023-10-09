using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace InventorySystem
{
    public class ContainerSlotGroup : MonoBehaviour
    {
        [SerializeField] RectTransform rectTransform;
        [SerializeField] GridLayoutGroup gridLayoutGroup;

        List<InventorySlot> slots = new List<InventorySlot>();

        public void SetupRectTransform(InventoryLayout inventoryLayout)
        {
            int newWidth = inventoryLayout.MaxSlotsPerRow * inventoryLayout.SlotWidth * InventoryItem.slotSize;
            int newHeight = Mathf.CeilToInt((float)inventoryLayout.AmountOfSlots / inventoryLayout.MaxSlotsPerRow) * inventoryLayout.SlotHeight * InventoryItem.slotSize;

            gridLayoutGroup.cellSize = new Vector2(inventoryLayout.SlotWidth * InventoryItem.slotSize, inventoryLayout.SlotHeight * InventoryItem.slotSize);
            rectTransform.sizeDelta = new Vector2(newWidth, newHeight);
        }

        public List<InventorySlot> Slots => slots;
    }
}
