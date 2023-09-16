using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class ContainerUI : MonoBehaviour
{
    [SerializeField] TextMeshProUGUI titleText;
    [SerializeField] RectTransform rectTransform;
    [SerializeField] HorizontalLayoutGroup horizontalLayoutGroup;

    [Header("Container Slot Groups")]
    [SerializeField] ContainerSlotGroup mainContainerSlotGroup;
    [SerializeField] ContainerSlotGroup[] subContainerSlotGroups;

    [Header("Section RectTransforms")]
    [SerializeField] RectTransform leftSectionRectTransform;
    [SerializeField] RectTransform middleSectionRectTransform;
    [SerializeField] RectTransform rightSectionRectTransform;

    public ContainerInventoryManager containerInventoryManager { get; private set; }

    public void ShowContainerInventory(ContainerInventory mainContainerInventory, Item containerItem)
    {
        if (containerItem != null)
            titleText.text = containerItem.name;

        containerInventoryManager = mainContainerInventory.ParentInventory.containerInventoryManager;
        containerInventoryManager.ParentInventory.SetupSlots(mainContainerSlotGroup);
        for (int i = 0; i < containerInventoryManager.SubInventories.Length; i++)
        {
            containerInventoryManager.SubInventories[i].SetupSlots(subContainerSlotGroups[i]);
        }

        gameObject.SetActive(true);
    }

    public void CloseContainerInventory()
    {
        if (gameObject.activeSelf == false)
            return;

        containerInventoryManager.ParentInventory.RemoveSlots();
        for (int i = 0; i < containerInventoryManager.SubInventories.Length; i++)
        {
            containerInventoryManager.SubInventories[i].RemoveSlots();
        }

        containerInventoryManager = null;
        gameObject.SetActive(false);
    }

    public void SetupRectTransform(ContainerInventory mainContainerInventory)
    {
        // Left section
        int leftWidth = 0;
        int leftHeight = 0;
        if (mainContainerInventory.ParentInventory.SubInventories.Length >= 1 && mainContainerInventory.ParentInventory.SubInventories[0].InventoryLayout.AmountOfSlots > 0)
        {
            leftHeight = mainContainerInventory.ParentInventory.SubInventories[0].MaxSlotsPerColumn * InventoryItem.slotSize;
            leftWidth = mainContainerInventory.ParentInventory.SubInventories[0].InventoryLayout.MaxSlotsPerRow * InventoryItem.slotSize;
            subContainerSlotGroups[0].gameObject.SetActive(true);
        }
        else
            subContainerSlotGroups[0].gameObject.SetActive(false);

        if (mainContainerInventory.ParentInventory.SubInventories.Length >= 3 && mainContainerInventory.ParentInventory.SubInventories[2].InventoryLayout.AmountOfSlots > 0)
        {
            leftHeight += mainContainerInventory.ParentInventory.SubInventories[2].MaxSlotsPerColumn * InventoryItem.slotSize;
            int widthToCheck = mainContainerInventory.ParentInventory.SubInventories[2].InventoryLayout.MaxSlotsPerRow * InventoryItem.slotSize;
            if (widthToCheck > leftWidth)
                leftWidth = widthToCheck;
            subContainerSlotGroups[2].gameObject.SetActive(true);
        }
        else
            subContainerSlotGroups[2].gameObject.SetActive(false);

        // Middle section
        int middleWidth = mainContainerInventory.ParentInventory.InventoryLayout.MaxSlotsPerRow * InventoryItem.slotSize;
        int middleHeight = mainContainerInventory.ParentInventory.MaxSlotsPerColumn * InventoryItem.slotSize;
        if (mainContainerInventory.ParentInventory.SubInventories.Length >= 5 && mainContainerInventory.ParentInventory.SubInventories[4].InventoryLayout.AmountOfSlots > 0)
        {
            middleHeight += mainContainerInventory.ParentInventory.SubInventories[4].MaxSlotsPerColumn * InventoryItem.slotSize;
            int widthToCheck = mainContainerInventory.ParentInventory.SubInventories[4].InventoryLayout.MaxSlotsPerRow * InventoryItem.slotSize;
            if (widthToCheck > middleWidth)
                middleWidth = widthToCheck;
            subContainerSlotGroups[4].gameObject.SetActive(true);
        }
        else
            subContainerSlotGroups[4].gameObject.SetActive(false);

        // Right section
        int rightWidth = 0;
        int rightHeight = 0;
        if (mainContainerInventory.ParentInventory.SubInventories.Length >= 2 && mainContainerInventory.ParentInventory.SubInventories[1].InventoryLayout.AmountOfSlots > 0)
        {
            rightHeight = mainContainerInventory.ParentInventory.SubInventories[1].MaxSlotsPerColumn * InventoryItem.slotSize;
            rightWidth = mainContainerInventory.ParentInventory.SubInventories[1].InventoryLayout.MaxSlotsPerRow * InventoryItem.slotSize;
            subContainerSlotGroups[1].gameObject.SetActive(true);
        }
        else
            subContainerSlotGroups[1].gameObject.SetActive(false);

        if (mainContainerInventory.ParentInventory.SubInventories.Length >= 4 && mainContainerInventory.ParentInventory.SubInventories[3].InventoryLayout.AmountOfSlots > 0)
        {
            rightHeight += mainContainerInventory.ParentInventory.SubInventories[3].MaxSlotsPerColumn * InventoryItem.slotSize;
            int widthToCheck = mainContainerInventory.ParentInventory.SubInventories[3].InventoryLayout.MaxSlotsPerRow * InventoryItem.slotSize;
            if (widthToCheck > rightWidth)
                rightWidth = widthToCheck;
            subContainerSlotGroups[3].gameObject.SetActive(true);
        }
        else
            subContainerSlotGroups[3].gameObject.SetActive(false);

        int newWidth = leftWidth + middleWidth + rightWidth;
        newWidth += horizontalLayoutGroup.padding.left + horizontalLayoutGroup.padding.right;

        if (leftWidth > 0)
            newWidth += (int)horizontalLayoutGroup.spacing;

        if (rightWidth > 0)
            newWidth += (int)horizontalLayoutGroup.spacing;

        int newHeight = middleHeight;
        if (leftHeight > newHeight)
            newHeight = leftHeight;

        if (rightHeight > newHeight)
            newHeight = rightHeight;

        newHeight += horizontalLayoutGroup.padding.top + horizontalLayoutGroup.padding.bottom;

        if ((subContainerSlotGroups[0] != null && subContainerSlotGroups[0].gameObject.activeSelf && subContainerSlotGroups[2] != null && subContainerSlotGroups[2].gameObject.activeSelf)
            || (subContainerSlotGroups[1] != null && subContainerSlotGroups[1].gameObject.activeSelf && subContainerSlotGroups[3] != null && subContainerSlotGroups[3].gameObject.activeSelf)
            || (subContainerSlotGroups[4] != null && subContainerSlotGroups[4].gameObject.activeSelf))
        {
            newHeight += (int)horizontalLayoutGroup.spacing;
        }

        rectTransform.sizeDelta = new Vector2(newWidth, newHeight);

        if (leftWidth > 0)
        {
            leftSectionRectTransform.sizeDelta = new Vector2(leftWidth, leftSectionRectTransform.sizeDelta.y);
            leftSectionRectTransform.gameObject.SetActive(true);
        }
        else
            leftSectionRectTransform.gameObject.SetActive(false);

        middleSectionRectTransform.sizeDelta = new Vector2(middleWidth, middleSectionRectTransform.sizeDelta.y);

        if (rightWidth > 0)
        {
            rightSectionRectTransform.sizeDelta = new Vector2(rightWidth, rightSectionRectTransform.sizeDelta.y);
            rightSectionRectTransform.gameObject.SetActive(true);
        }
        else
            rightSectionRectTransform.gameObject.SetActive(false);
    }

    public void TakeAll()
    {
        Debug.Log("Taking all from " + name);
    }
}
