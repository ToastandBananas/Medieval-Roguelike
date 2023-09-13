using UnityEngine;
using TMPro;

public class ContainerUI : MonoBehaviour
{
    [SerializeField] TextMeshProUGUI titleText;
    [SerializeField] RectTransform rectTransform;

    [Header("Container Slot Groups")]
    [SerializeField] ContainerSlotGroup mainContainerSlotGroup;
    [SerializeField] ContainerSlotGroup[] subContainerSlotGroups;

    [Header("Section RectTransforms")]
    [SerializeField] RectTransform section1RectTransform;
    [SerializeField] RectTransform section2RectTransform;

    public void ShowContainerInventory(ContainerInventory mainContainerInventory, Item containerItem)
    {
        if (containerItem != null)
            titleText.text = containerItem.name;

        mainContainerInventory.SetupSlots(mainContainerSlotGroup);
        for (int i = 0; i < mainContainerInventory.SubInventories.Length; i++)
        {
            mainContainerInventory.SubInventories[i].SetupSlots(subContainerSlotGroups[i]);
        }

        gameObject.SetActive(true);
    }

    public void SetupRectTransform(ContainerInventory mainContainerInventory)
    {
        int newWidth = (mainContainerInventory.ParentInventory.InventoryLayout.MaxSlotsPerRow * InventoryItem.slotSize) + InventoryItem.slotSize;

        int leftWidth = 0;
        int rightWidth = 0;

        int mainHeight = (Mathf.CeilToInt(mainContainerInventory.ParentInventory.InventoryLayout.AmountOfSlots / mainContainerInventory.ParentInventory.InventoryLayout.MaxSlotsPerRow) * InventoryItem.slotSize) + InventoryItem.slotSize;

        int leftHeight = 0;
        if (mainContainerInventory.ParentInventory.SubInventories.Length >= 1 && mainContainerInventory.ParentInventory.SubInventories[0].InventoryLayout.AmountOfSlots > 0)
        {
            leftHeight = (Mathf.CeilToInt(mainContainerInventory.ParentInventory.SubInventories[0].InventoryLayout.AmountOfSlots / mainContainerInventory.SubInventories[0].InventoryLayout.MaxSlotsPerRow) * InventoryItem.slotSize) + InventoryItem.slotSize;
            leftWidth = (mainContainerInventory.ParentInventory.SubInventories[0].InventoryLayout.MaxSlotsPerRow * InventoryItem.slotSize) + InventoryItem.slotSize;
        }

        if (mainContainerInventory.ParentInventory.SubInventories.Length >= 3 && mainContainerInventory.ParentInventory.SubInventories[2].InventoryLayout.AmountOfSlots > 0)
        {
            leftHeight += (Mathf.CeilToInt(mainContainerInventory.ParentInventory.SubInventories[2].InventoryLayout.AmountOfSlots / mainContainerInventory.ParentInventory.SubInventories[2].InventoryLayout.MaxSlotsPerRow) * InventoryItem.slotSize) + InventoryItem.slotSize;
            int widthToCheck = (mainContainerInventory.ParentInventory.SubInventories[2].InventoryLayout.MaxSlotsPerRow * InventoryItem.slotSize) + InventoryItem.slotSize;
            if (widthToCheck > leftWidth)
                leftWidth = widthToCheck;
        }

        int rightHeight = 0;
        if (mainContainerInventory.ParentInventory.SubInventories.Length >= 2 && mainContainerInventory.ParentInventory.SubInventories[1].InventoryLayout.AmountOfSlots > 0)
        {
            rightHeight = (Mathf.CeilToInt(mainContainerInventory.ParentInventory.SubInventories[1].InventoryLayout.AmountOfSlots / mainContainerInventory.ParentInventory.SubInventories[1].InventoryLayout.MaxSlotsPerRow) * InventoryItem.slotSize) + InventoryItem.slotSize;
            rightWidth = (mainContainerInventory.ParentInventory.SubInventories[1].InventoryLayout.MaxSlotsPerRow * InventoryItem.slotSize) + InventoryItem.slotSize;
        }

        if (mainContainerInventory.ParentInventory.SubInventories.Length >= 4 && mainContainerInventory.ParentInventory.SubInventories[3].InventoryLayout.AmountOfSlots > 0)
        {
            rightHeight += (Mathf.CeilToInt(mainContainerInventory.ParentInventory.SubInventories[3].InventoryLayout.AmountOfSlots / mainContainerInventory.ParentInventory.SubInventories[3].InventoryLayout.MaxSlotsPerRow) * InventoryItem.slotSize) + InventoryItem.slotSize;
            int widthToCheck = (mainContainerInventory.ParentInventory.SubInventories[3].InventoryLayout.MaxSlotsPerRow * InventoryItem.slotSize) + InventoryItem.slotSize;
            if (widthToCheck > rightWidth)
                rightWidth = widthToCheck;
        }

        newWidth += leftWidth + rightWidth - (InventoryItem.slotSize / 2);

        int newHeight = mainHeight;
        if (leftHeight > newHeight)
            newHeight = leftHeight;

        if (rightHeight > newHeight)
            newHeight = rightHeight;

        if (newHeight != mainHeight)
            newHeight -= InventoryItem.slotSize;

        rectTransform.sizeDelta = new Vector2(newWidth, newHeight);

        section1RectTransform.sizeDelta = new Vector2(leftWidth + (InventoryItem.slotSize / 4), section1RectTransform.sizeDelta.y);
        section2RectTransform.sizeDelta = new Vector2(rightWidth + (InventoryItem.slotSize / 4), section1RectTransform.sizeDelta.y);
    }

    public void TakeAll()
    {
        Debug.Log("Taking all from " + name);
    }
}
