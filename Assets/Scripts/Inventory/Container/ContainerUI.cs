using UnityEngine;
using TMPro;
using UnityEngine.UI;
using UnitSystem;

namespace InventorySystem
{
    public class ContainerUI : MonoBehaviour
    {
        [SerializeField] TextMeshProUGUI titleText;
        [SerializeField] TextMeshProUGUI takeAllText;
        [SerializeField] RectTransform rectTransform;
        [SerializeField] HorizontalLayoutGroup horizontalLayoutGroup;
        [SerializeField] ContainerUIDragHandle dragHandle;

        [Header("Container Slot Groups")]
        [SerializeField] ContainerSlotGroup mainContainerSlotGroup;
        [SerializeField] ContainerSlotGroup[] subContainerSlotGroups;

        [Header("Section RectTransforms")]
        [SerializeField] RectTransform leftSectionRectTransform;
        [SerializeField] RectTransform middleSectionRectTransform;
        [SerializeField] RectTransform rightSectionRectTransform;

        public InventoryManager_Container containerInventoryManager { get; private set; }

        readonly int minWidth = 260;

        public void ShowContainerInventory(ContainerInventory mainContainerInventory, Item containerItem)
        {
            SetTitleText(mainContainerInventory, containerItem);
            
            containerInventoryManager = mainContainerInventory.ParentInventory.containerInventoryManager;
            containerInventoryManager.ParentInventory.SetupSlots(mainContainerSlotGroup);
            for (int i = 0; i < containerInventoryManager.SubInventories.Length; i++)
            {
                containerInventoryManager.SubInventories[i].SetupSlots(subContainerSlotGroups[i]);
            }

            SetTakeAllText();
            SetupRectTransform(mainContainerInventory);
            gameObject.SetActive(true);
        }

        void SetTitleText(ContainerInventory mainContainerInventory, Item containerItem)
        {
            if (containerItem != null)
            {
                if (mainContainerInventory.MyUnit != null)
                {
                    if (mainContainerInventory.MyUnit.IsPlayer)
                        titleText.text = $"Your {containerItem.Name}";
                    else
                        titleText.text = $"{mainContainerInventory.MyUnit.name}'s {containerItem.Name}";
                }
                else
                    titleText.text = containerItem.Name;
            }
        }

        void SetTakeAllText()
        {
            if (containerInventoryManager.IsEquippedByPlayer())
                takeAllText.text = "Remove All";
            else
                takeAllText.text = "Take All";
        }

        public void CloseContainerInventory()
        {
            if (gameObject.activeSelf == false || containerInventoryManager == null)
                return;

            containerInventoryManager.ParentInventory.RemoveSlots();
            for (int i = 0; i < containerInventoryManager.SubInventories.Length; i++)
            {
                containerInventoryManager.SubInventories[i].RemoveSlots();
            }

            dragHandle.Reset();
            containerInventoryManager = null;
            gameObject.SetActive(false);
        }

        void SetupRectTransform(ContainerInventory mainContainerInventory)
        {
            // Left section
            int leftWidth = 0;
            int leftHeight = 0;
            if (mainContainerInventory.ParentInventory.SubInventories.Length >= 1 && mainContainerInventory.ParentInventory.SubInventories[0].InventoryLayout.AmountOfSlots > 0)
            {
                leftHeight = mainContainerInventory.ParentInventory.SubInventories[0].MaxSlotsPerColumn * mainContainerInventory.ParentInventory.SubInventories[0].InventoryLayout.SlotHeight * InventoryItem.slotSize;
                leftWidth = mainContainerInventory.ParentInventory.SubInventories[0].InventoryLayout.MaxSlotsPerRow * mainContainerInventory.ParentInventory.SubInventories[0].InventoryLayout.SlotWidth * InventoryItem.slotSize;
                subContainerSlotGroups[0].gameObject.SetActive(true);
            }
            else
                subContainerSlotGroups[0].gameObject.SetActive(false);

            if (mainContainerInventory.ParentInventory.SubInventories.Length >= 3 && mainContainerInventory.ParentInventory.SubInventories[2].InventoryLayout.AmountOfSlots > 0)
            {
                leftHeight += mainContainerInventory.ParentInventory.SubInventories[2].MaxSlotsPerColumn * mainContainerInventory.ParentInventory.SubInventories[2].InventoryLayout.SlotHeight * InventoryItem.slotSize;
                int widthToCheck = mainContainerInventory.ParentInventory.SubInventories[2].InventoryLayout.MaxSlotsPerRow * mainContainerInventory.ParentInventory.SubInventories[2].InventoryLayout.SlotWidth * InventoryItem.slotSize;
                if (widthToCheck > leftWidth)
                    leftWidth = widthToCheck;
                subContainerSlotGroups[2].gameObject.SetActive(true);
            }
            else
                subContainerSlotGroups[2].gameObject.SetActive(false);

            // Middle section
            int middleWidth = mainContainerInventory.ParentInventory.InventoryLayout.MaxSlotsPerRow * mainContainerInventory.ParentInventory.InventoryLayout.SlotWidth * InventoryItem.slotSize;
            int middleHeight = mainContainerInventory.ParentInventory.MaxSlotsPerColumn * mainContainerInventory.ParentInventory.InventoryLayout.SlotHeight * InventoryItem.slotSize;
            if (mainContainerInventory.ParentInventory.SubInventories.Length >= 5 && mainContainerInventory.ParentInventory.SubInventories[4].InventoryLayout.AmountOfSlots > 0)
            {
                middleHeight += mainContainerInventory.ParentInventory.SubInventories[4].MaxSlotsPerColumn * mainContainerInventory.ParentInventory.SubInventories[4].InventoryLayout.SlotHeight * InventoryItem.slotSize;
                int widthToCheck = mainContainerInventory.ParentInventory.SubInventories[4].InventoryLayout.MaxSlotsPerRow * mainContainerInventory.ParentInventory.SubInventories[4].InventoryLayout.SlotWidth * InventoryItem.slotSize;
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
                rightHeight = mainContainerInventory.ParentInventory.SubInventories[1].MaxSlotsPerColumn * mainContainerInventory.ParentInventory.SubInventories[1].InventoryLayout.SlotHeight * InventoryItem.slotSize;
                rightWidth = mainContainerInventory.ParentInventory.SubInventories[1].InventoryLayout.MaxSlotsPerRow * mainContainerInventory.ParentInventory.SubInventories[1].InventoryLayout.SlotWidth * InventoryItem.slotSize;
                subContainerSlotGroups[1].gameObject.SetActive(true);
            }
            else
                subContainerSlotGroups[1].gameObject.SetActive(false);

            if (mainContainerInventory.ParentInventory.SubInventories.Length >= 4 && mainContainerInventory.ParentInventory.SubInventories[3].InventoryLayout.AmountOfSlots > 0)
            {
                rightHeight += mainContainerInventory.ParentInventory.SubInventories[3].MaxSlotsPerColumn * mainContainerInventory.ParentInventory.SubInventories[3].InventoryLayout.SlotHeight * InventoryItem.slotSize;
                int widthToCheck = mainContainerInventory.ParentInventory.SubInventories[3].InventoryLayout.MaxSlotsPerRow * mainContainerInventory.ParentInventory.SubInventories[3].InventoryLayout.SlotWidth * InventoryItem.slotSize;
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

            if (newWidth < minWidth)
                newWidth = minWidth;

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
            if (containerInventoryManager.IsEquippedByPlayer())
            {
                RemoveAll();
                return;
            }

            for (int i = containerInventoryManager.ParentInventory.ItemDatas.Count - 1; i >= 0; i--)
            {
                UnitManager.player.UnitInventoryManager.TryAddItemToInventories(containerInventoryManager.ParentInventory.ItemDatas[i]);
            }

            for (int i = 0; i < containerInventoryManager.SubInventories.Length; i++)
            {
                for (int j = containerInventoryManager.SubInventories[i].ItemDatas.Count - 1; j >= 0; j--)
                {
                    UnitManager.player.UnitInventoryManager.TryAddItemToInventories(containerInventoryManager.SubInventories[i].ItemDatas[j]);
                }
            }

            if (containerInventoryManager.ContainsAnyItems() == false)
                CloseContainerInventory();
        }

        void RemoveAll()
        {
            for (int i = containerInventoryManager.ParentInventory.ItemDatas.Count - 1; i >= 0; i--)
            {
                if (UnitManager.player.UnitInventoryManager.MainInventory.TryAddItem(containerInventoryManager.ParentInventory.ItemDatas[i], UnitManager.player) == false)
                    DropItemManager.DropItem(containerInventoryManager.ParentInventory, UnitManager.player, containerInventoryManager.ParentInventory.ItemDatas[i]);
            }

            for (int i = 0; i < containerInventoryManager.SubInventories.Length; i++)
            {
                for (int j = containerInventoryManager.SubInventories[i].ItemDatas.Count - 1; j >= 0; j--)
                {
                    if (UnitManager.player.UnitInventoryManager.MainInventory.TryAddItem(containerInventoryManager.SubInventories[i].ItemDatas[j], UnitManager.player) == false)
                        DropItemManager.DropItem(containerInventoryManager.SubInventories[i], UnitManager.player, containerInventoryManager.SubInventories[i].ItemDatas[j]);
                }
            }

            if (containerInventoryManager.ContainsAnyItems() == false)
                CloseContainerInventory();
        }

        public void DropAll()
        {
            for (int i = containerInventoryManager.ParentInventory.ItemDatas.Count - 1; i >= 0; i--)
            {
                DropItemManager.DropItem(containerInventoryManager.ParentInventory,UnitManager.player, containerInventoryManager.ParentInventory.ItemDatas[i]);
            }

            for (int i = 0; i < containerInventoryManager.SubInventories.Length; i++)
            {
                for (int j = containerInventoryManager.SubInventories[i].ItemDatas.Count - 1; j >= 0; j--)
                {
                    DropItemManager.DropItem(containerInventoryManager.SubInventories[i],UnitManager.player, containerInventoryManager.SubInventories[i].ItemDatas[j]);
                }
            }

            if (containerInventoryManager.ContainsAnyItems() == false)
                CloseContainerInventory();
        }
    }
}
