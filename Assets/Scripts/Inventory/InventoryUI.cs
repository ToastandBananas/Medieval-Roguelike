using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using Controls;
using ContextMenu = GeneralUI.ContextMenu;
using UnitSystem;
using ActionSystem;
using GeneralUI;

namespace InventorySystem
{
    public class InventoryUI : MonoBehaviour
    {
        public static InventoryUI Instance { get; private set; }

        [SerializeField] InventoryItem draggedItem;

        [Header("Prefab")]
        [SerializeField] InventorySlot inventorySlotPrefab;

        [Header("Inventory Parents")]
        [SerializeField] GameObject playerInventoryUIParent;
        [SerializeField] GameObject npcInventoryUIParent;

        [Header("Pocket Inventory Parents")]
        [SerializeField] Transform playerPocketsParent;
        [SerializeField] Transform npcPocketsParent;
        public static List<InventorySlot> playerPocketsSlots { get; private set; }
        public static List<InventorySlot> npcPocketsSlots { get; private set; }

        [Header("Equipment Parents")]
        [SerializeField] Transform playerEquipmentParent;
        [SerializeField] Transform npcEquipmentParent;
        public static List<EquipmentSlot> playerEquipmentSlots { get; private set; }
        public static List<EquipmentSlot> npcEquipmentSlots { get; private set; }

        [Header("Container UI")]
        [SerializeField] ContainerUI[] containerUIs;

        public static Slot activeSlot { get; private set; }

        public static bool isDraggingItem { get; private set; }
        public static bool validDragPosition { get; private set; }
        public static Slot parentSlotDraggedFrom { get; private set; }

        public static bool playerInventoryActive { get; private set; }
        public static bool npcInventoryActive { get; private set; }

        public static Inventory lastInventoryInteractedWith { get; private set; }

        RectTransform rectTransform;

        static WaitForSeconds stopDraggingDelay = new WaitForSeconds(0.05f);

        void Awake()
        {
            if (Instance != null)
            {
                Debug.LogError("There's more than one InventoryUI! " + transform + " - " + Instance);
                Destroy(gameObject);
                return;
            }
            Instance = this;

            playerPocketsSlots = new List<InventorySlot>();
            npcPocketsSlots = new List<InventorySlot>();

            playerEquipmentSlots = playerEquipmentParent.gameObject.GetComponentsInChildren<EquipmentSlot>().ToList();
            npcEquipmentSlots = npcEquipmentParent.gameObject.GetComponentsInChildren<EquipmentSlot>().ToList();

            rectTransform = GetComponent<RectTransform>();

            draggedItem.DisableIconImage();

            if (Instance.playerInventoryUIParent.activeSelf)
                TogglePlayerInventory();

            if (Instance.npcInventoryUIParent.activeSelf)
                ToggleNPCInventory();
        }

        void Update()
        {
            if (GameControls.gamePlayActions.toggleInventory.WasPressed)
                TogglePlayerInventory();
            
            // If we're not already dragging an item
            if (isDraggingItem == false)
            {
                // Don't allow drag/drop inventory/equipment actions while an action is already queued
                if (UnitManager.player.unitActionHandler.queuedActions.Count > 0)
                    return;

                // If we select an item
                if (GameControls.gamePlayActions.menuSelect.WasPressed)
                {
                    if (activeSlot == null || activeSlot.IsFull() == false)
                        return;

                    // "Pickup" the item by hiding the item's sprite and showing that same sprite on the draggedItem object
                    if (activeSlot is InventorySlot)
                    {
                        InventorySlot activeInventorySlot = activeSlot as InventorySlot;
                        SetupDraggedItem(activeInventorySlot.slotCoordinate.parentSlotCoordinate.itemData, activeInventorySlot.ParentSlot(), activeInventorySlot.myInventory);
                        lastInventoryInteractedWith = activeInventorySlot.myInventory;
                    }
                    else
                    {
                        EquipmentSlot activeEquipmentSlot = activeSlot as EquipmentSlot;
                        if ((activeEquipmentSlot.EquipSlot == EquipSlot.RightHeldItem1 || activeEquipmentSlot.EquipSlot == EquipSlot.RightHeldItem2) && (activeEquipmentSlot.InventoryItem.itemData == null || activeEquipmentSlot.InventoryItem.itemData.Item == null))
                        {
                            EquipmentSlot oppositeWeaponSlot = activeEquipmentSlot.GetOppositeWeaponSlot();
                            if (oppositeWeaponSlot.InventoryItem.itemData.Item != null && oppositeWeaponSlot.InventoryItem.itemData.Item is Weapon && oppositeWeaponSlot.InventoryItem.itemData.Item.Weapon.IsTwoHanded)
                            {
                                SetupDraggedItem(oppositeWeaponSlot.InventoryItem.itemData, oppositeWeaponSlot, oppositeWeaponSlot.InventoryItem.myUnitEquipment);
                                oppositeWeaponSlot.InventoryItem.DisableIconImage();
                            }
                            else
                                SetupDraggedItem(activeEquipmentSlot.InventoryItem.itemData, activeSlot, activeSlot.InventoryItem.myUnitEquipment);
                        }
                        else
                        {
                            SetupDraggedItem(activeEquipmentSlot.InventoryItem.itemData, activeSlot, activeSlot.InventoryItem.myUnitEquipment);

                            if ((activeEquipmentSlot.EquipSlot == EquipSlot.LeftHeldItem1 || activeEquipmentSlot.EquipSlot == EquipSlot.LeftHeldItem2) && activeEquipmentSlot.InventoryItem.itemData.Item is Weapon && activeEquipmentSlot.InventoryItem.itemData.Item.Weapon.IsTwoHanded)
                                activeEquipmentSlot.GetOppositeWeaponSlot().InventoryItem.DisableIconImage();
                        }

                        lastInventoryInteractedWith = null;
                    }

                    activeSlot.ParentSlot().SetupEmptySlotSprites();
                    activeSlot.ParentSlot().InventoryItem.DisableIconImage();
                    activeSlot.ParentSlot().InventoryItem.ClearStackSizeText();

                    activeSlot.HighlightSlots();
                }
            }
            else // If we are dragging an item
            {
                // Don't allow drag/drop inventory/equipment actions while an action is already queued
                if (UnitManager.player.unitActionHandler.queuedActions.Count > 0)
                {
                    ReplaceDraggedItem();
                    return;
                }

                // The dragged item should follow the mouse position
                Vector2 offset = draggedItem.GetDraggedItemOffset();
                RectTransformUtility.ScreenPointToLocalPointInRectangle(rectTransform, Input.mousePosition, null, out Vector2 localMousePosition);
                draggedItem.RectTransform.localPosition = localMousePosition + offset;

                // If we try to place an item
                if (GameControls.gamePlayActions.menuSelect.WasPressed)
                {
                    // Try placing the item
                    if (activeSlot != null && validDragPosition && activeSlot != parentSlotDraggedFrom)
                    {
                        if (activeSlot is InventorySlot)
                        {
                            InventorySlot activeInventorySlot = activeSlot as InventorySlot;
                            activeInventorySlot.myInventory.TryAddItemAt(activeInventorySlot.slotCoordinate, draggedItem.itemData, UnitManager.player);
                            lastInventoryInteractedWith = activeInventorySlot.myInventory;
                        }
                        else if (activeSlot is EquipmentSlot)
                        {
                            // If drag/dropping an item onto an equipped backpack
                            if ((draggedItem.itemData.Item is Equipment == false || draggedItem.itemData.Item.Equipment.EquipSlot != EquipSlot.Back) && activeSlot.EquipmentSlot.EquipSlot == EquipSlot.Back && UnitManager.player.UnitEquipment.BackpackEquipped())
                            {
                                if (UnitManager.player.BackpackInventoryManager.TryAddItem(draggedItem.itemData, UnitManager.player))
                                {
                                    if (UnitManager.player.UnitEquipment.ItemDataEquipped(draggedItem.itemData))
                                        UnitManager.player.UnitEquipment.RemoveEquipment(draggedItem.itemData);
                                }
                                else
                                    ReplaceDraggedItem();
                            }
                            else if ((draggedItem.itemData.Item is Equipment == false || draggedItem.itemData.Item.Equipment.EquipSlot != EquipSlot.Belt) && activeSlot.EquipmentSlot.EquipSlot == EquipSlot.Belt && UnitManager.player.UnitEquipment.BeltBagEquipped())
                            {
                                if (UnitManager.player.BeltInventoryManager.TryAddItem(draggedItem.itemData, UnitManager.player))
                                {
                                    if (UnitManager.player.UnitEquipment.ItemDataEquipped(draggedItem.itemData))
                                        UnitManager.player.UnitEquipment.RemoveEquipment(draggedItem.itemData);
                                }
                                else
                                    ReplaceDraggedItem();
                            }
                            else
                            {
                                EquipmentSlot activeEquipmentSlot = activeSlot as EquipmentSlot;
                                UnitManager.player.unitActionHandler.GetAction<EquipAction>().QueueAction(draggedItem.itemData, activeEquipmentSlot.EquipSlot, parentSlotDraggedFrom != null && parentSlotDraggedFrom is ContainerEquipmentSlot ? parentSlotDraggedFrom.EquipmentSlot.ContainerEquipmentSlot.containerInventoryManager : null);
                                lastInventoryInteractedWith = null;
                            }

                            DisableDraggedItem();
                        }
                    }
                    else if (EventSystem.current.IsPointerOverGameObject() == false)
                    {
                        if (parentSlotDraggedFrom == null)
                        {
                            Unit unit = null;
                            if (draggedItem.myInventory != null)
                                unit = draggedItem.myInventory.MyUnit;

                            DropItemManager.DropItem(draggedItem.myInventory, unit, draggedItem.itemData);
                        }
                        else if (parentSlotDraggedFrom is EquipmentSlot)
                        {
                            EquipmentSlot equipmentSlotDraggedFrom = parentSlotDraggedFrom as EquipmentSlot;
                            DropItemManager.DropItem(equipmentSlotDraggedFrom.UnitEquipment, equipmentSlotDraggedFrom.EquipSlot);
                        }
                        else
                        {
                            InventorySlot inventorySlotDraggedFrom = parentSlotDraggedFrom as InventorySlot;
                            DropItemManager.DropItem(inventorySlotDraggedFrom.myInventory, inventorySlotDraggedFrom.myInventory.MyUnit, draggedItem.itemData);
                        }
                    }
                    else
                        ReplaceDraggedItem();
                }
            }
        }

        public static bool OverlappingMultipleItems(SlotCoordinate focusedSlotCoordinate, ItemData itemData, out SlotCoordinate overlappedItemsParentSlotCoordinate, out int overlappedItemCount)
        {
            overlappedItemsParentSlotCoordinate = null;
            overlappedItemCount = 0;

            if (focusedSlotCoordinate.myInventory.InventoryLayout.HasStandardSlotSize() == false)
            {
                overlappedItemsParentSlotCoordinate = focusedSlotCoordinate.myInventory.GetSlotCoordinate(focusedSlotCoordinate.coordinate.x, focusedSlotCoordinate.coordinate.y);
                if (overlappedItemsParentSlotCoordinate.isFull && overlappedItemsParentSlotCoordinate.itemData != itemData)
                    overlappedItemCount++;
                return false;
            }

            ItemData overlappedItemData = null;
            for (int x = 0; x < itemData.Item.Width; x++)
            {
                for (int y = 0; y < itemData.Item.Height; y++)
                {
                    SlotCoordinate slotCoordinateToCheck;
                    if (focusedSlotCoordinate.myInventory != null)
                    {
                        slotCoordinateToCheck = focusedSlotCoordinate.myInventory.GetSlotCoordinate(focusedSlotCoordinate.coordinate.x - x, focusedSlotCoordinate.coordinate.y - y);
                    }
                    else
                        slotCoordinateToCheck = focusedSlotCoordinate;

                    if (slotCoordinateToCheck == null)
                        continue;

                    if (slotCoordinateToCheck.isFull)
                    {
                        if (slotCoordinateToCheck.parentSlotCoordinate.itemData == itemData)
                            continue;

                        if (overlappedItemData == null)
                        {
                            if (slotCoordinateToCheck.myInventory != null)
                            {
                                overlappedItemsParentSlotCoordinate = slotCoordinateToCheck.parentSlotCoordinate;
                                overlappedItemData = slotCoordinateToCheck.parentSlotCoordinate.itemData;
                                overlappedItemCount++;
                            }
                            else
                            {
                                overlappedItemsParentSlotCoordinate = slotCoordinateToCheck;
                                overlappedItemCount++;
                                return false;
                            }
                        }
                        else if (overlappedItemData != slotCoordinateToCheck.parentSlotCoordinate.itemData)
                        {
                            overlappedItemCount++;
                            return true;
                        }
                    }
                }
            }
            return false;
        }

        public static void ReplaceDraggedItem()
        {
            // No need to setup the ItemData since it hasn't changed, so just show the item's sprite and change the slot's color/remove highlighting
            if (activeSlot != null)
                activeSlot.RemoveSlotHighlights();

            // If replacing a split stack
            if (parentSlotDraggedFrom != null && parentSlotDraggedFrom.GetItemData() != Instance.draggedItem.itemData && Instance.draggedItem.itemData.IsEqual(parentSlotDraggedFrom.GetItemData()))
            {
                if (parentSlotDraggedFrom is InventorySlot)
                {
                    if (parentSlotDraggedFrom.InventoryItem.myInventory.TryAddItemAt(parentSlotDraggedFrom.InventoryItem.myInventory.GetSlotCoordinateFromItemData(parentSlotDraggedFrom.GetItemData()), Instance.draggedItem.itemData, UnitManager.player) == false)
                    {
                        if (parentSlotDraggedFrom.InventoryItem.myInventory.MyUnit.UnitInventoryManager.TryAddItemToInventories(Instance.draggedItem.itemData) == false)
                            DropItemManager.DropItem(Instance.draggedItem.myInventory, UnitManager.player, Instance.draggedItem.itemData);
                    }
                }
                else
                {
                    EquipmentSlot equipmentSlot = parentSlotDraggedFrom as EquipmentSlot;
                    if (equipmentSlot.UnitEquipment.TryAddItemAt(equipmentSlot.EquipSlot, Instance.draggedItem.itemData) == false)
                    {
                        if (equipmentSlot.UnitEquipment.MyUnit.UnitInventoryManager.TryAddItemToInventories(Instance.draggedItem.itemData) == false)
                            DropItemManager.DropItem(Instance.draggedItem.myInventory, UnitManager.player, Instance.draggedItem.itemData);
                    }
                }
            }
            // If replacing the item normally is possible
            else if (parentSlotDraggedFrom != null && parentSlotDraggedFrom.GetItemData() == Instance.draggedItem.itemData)
            {
                parentSlotDraggedFrom.ShowSlotImage();

                if (parentSlotDraggedFrom is InventorySlot)
                {
                    InventorySlot parentInventorySlotDraggedFrom = parentSlotDraggedFrom as InventorySlot;
                    parentInventorySlotDraggedFrom.SetupFullSlotSprites();
                }
                else
                {
                    EquipmentSlot parentEquipmentSlotDraggedFrom = parentSlotDraggedFrom as EquipmentSlot;
                    parentEquipmentSlotDraggedFrom.SetFullSlotSprite();

                    if (parentEquipmentSlotDraggedFrom.IsHeldItemSlot() && parentEquipmentSlotDraggedFrom.InventoryItem.itemData.Item is Weapon && parentEquipmentSlotDraggedFrom.InventoryItem.itemData.Item.Weapon.IsTwoHanded)
                    {
                        EquipmentSlot oppositeWeaponSlot = parentEquipmentSlotDraggedFrom.GetOppositeWeaponSlot();
                        oppositeWeaponSlot.SetFullSlotSprite();
                    }
                    else if (parentEquipmentSlotDraggedFrom.EquipSlot == EquipSlot.Quiver && Instance.draggedItem.itemData.Item is Quiver)
                        parentEquipmentSlotDraggedFrom.InventoryItem.QuiverInventoryItem.UpdateQuiverSprites();
                }

                parentSlotDraggedFrom.InventoryItem.UpdateStackSizeVisuals();
            }
            else // Otherwise just try to add it to one of the Unit's inventories
            {
                if (UnitManager.player.UnitInventoryManager.TryAddItemToInventories(Instance.draggedItem.itemData) == false)
                    DropItemManager.DropItem(Instance.draggedItem.myInventory, UnitManager.player, Instance.draggedItem.itemData);
            }

            DisableDraggedItem();
        }

        public static void SetupDraggedItem(ItemData newItemData, Slot theParentSlotDraggedFrom, Inventory inventoryDraggedFrom)
        {
            SplitStack.Instance.Close();
            ContextMenu.DisableContextMenu(true);
            TooltipManager.ClearInventoryTooltips();

            Cursor.visible = false;
            isDraggingItem = true;

            parentSlotDraggedFrom = theParentSlotDraggedFrom;
            if (theParentSlotDraggedFrom == null)
                newItemData.SetInventorySlotCoordinate(null);

            Instance.draggedItem.SetMyInventory(inventoryDraggedFrom);
            Instance.draggedItem.SetMyUnitEquipment(null);
            Instance.draggedItem.SetItemData(newItemData);
            Instance.draggedItem.UpdateStackSizeVisuals();
            Instance.draggedItem.SetupDraggedSprite();
        }

        public void SetupDraggedItem(ItemData newItemData, Slot theParentSlotDraggedFrom, UnitEquipment unitEquipmentDraggedFrom)
        {
            SplitStack.Instance.Close();
            ContextMenu.DisableContextMenu(true);
            TooltipManager.ClearInventoryTooltips();

            Cursor.visible = false;
            isDraggingItem = true;

            parentSlotDraggedFrom = theParentSlotDraggedFrom;

            if (theParentSlotDraggedFrom is ContainerEquipmentSlot)
            {
                ContainerEquipmentSlot containerEquipmentSlot = theParentSlotDraggedFrom as ContainerEquipmentSlot;
                if (containerEquipmentSlot.containerInventoryManager.ParentInventory.slotVisualsCreated)
                    GetContainerUI(containerEquipmentSlot.containerInventoryManager).CloseContainerInventory();

                if (containerEquipmentSlot.EquipSlot == EquipSlot.Quiver)
                    unitEquipmentDraggedFrom.GetEquipmentSlot(EquipSlot.Quiver).InventoryItem.QuiverInventoryItem.HideQuiverSprites();
            }

            draggedItem.SetMyInventory(null);
            draggedItem.SetMyUnitEquipment(unitEquipmentDraggedFrom);
            draggedItem.SetItemData(newItemData);
            draggedItem.UpdateStackSizeVisuals();
            draggedItem.SetupDraggedSprite();
        }

        public static void DisableDraggedItem()
        {
            if (isDraggingItem == false)
                return;

            if (activeSlot != null)
                activeSlot.RemoveSlotHighlights();

            Cursor.visible = true;
            isDraggingItem = false;
            parentSlotDraggedFrom = null;
            Instance.draggedItem.SetItemData(null);
            Instance.draggedItem.SetMyInventory(null);

            Instance.StartCoroutine(DelayStopDraggingItem());
            Instance.draggedItem.DisableIconImage();
            Instance.draggedItem.ClearStackSizeText();
        }

        static IEnumerator DelayStopDraggingItem()
        {
            yield return stopDraggingDelay;
            Instance.draggedItem.SetItemData(null);
        }

        public static void TogglePlayerInventory()
        {
            if (isDraggingItem)
                ReplaceDraggedItem();

            Instance.playerInventoryUIParent.SetActive(!Instance.playerInventoryUIParent.activeSelf);
            playerInventoryActive = Instance.playerInventoryUIParent.activeSelf;

            if (playerInventoryActive == false)
            {
                activeSlot = null;
                lastInventoryInteractedWith = null;
                ContextMenu.DisableContextMenu();
                SplitStack.Instance.Close();
                TooltipManager.ClearInventoryTooltips();
                CloseAllContainerUI();

                if (npcInventoryActive)
                    ToggleNPCInventory();
            }
        }

        public static void ClearNPCInventorySlots()
        {
            if (npcInventoryActive == false)
                return;

            if (isDraggingItem)
                ReplaceDraggedItem(); 
            
            if (npcEquipmentSlots.Count > 0)
                npcEquipmentSlots[0].UnitEquipment.OnCloseNPCInventory();

            if (npcPocketsSlots.Count > 0)
                npcPocketsSlots[0].myInventory.OnCloseNPCInventory();
        }

        public static void ToggleNPCInventory()
        {
            ClearNPCInventorySlots();

            Instance.npcInventoryUIParent.SetActive(!Instance.npcInventoryUIParent.activeSelf);
            npcInventoryActive = Instance.npcInventoryUIParent.activeSelf;

            if (npcInventoryActive && playerInventoryActive == false)
                TogglePlayerInventory();
            else if (npcInventoryActive == false)
            {
                activeSlot = null;
                ContextMenu.DisableContextMenu();
                SplitStack.Instance.Close();
                TooltipManager.ClearInventoryTooltips();
                CloseAllContainerUI();
            }
        }

        public static void ShowContainerUI(ContainerInventoryManager containerInventoryManager, Item containerItem)
        {
            for (int i = 0; i < Instance.containerUIs.Length; i++)
            {
                if (Instance.containerUIs[i].containerInventoryManager == containerInventoryManager)
                    return;
            }

            if (playerInventoryActive == false)
                TogglePlayerInventory();

            GetNextAvailableContainerUI().ShowContainerInventory(containerInventoryManager.ParentInventory, containerItem);
        }

        public static void CloseAllContainerUI()
        {
            if (isDraggingItem)
                ReplaceDraggedItem();

            for (int i = 0; i < Instance.containerUIs.Length; i++)
            {
                Instance.containerUIs[i].CloseContainerInventory();
            }
        }

        static ContainerUI GetNextAvailableContainerUI()
        {
            for (int i = 0; i < Instance.containerUIs.Length; i++)
            {
                if (Instance.containerUIs[i].gameObject.activeSelf == false)
                    return Instance.containerUIs[i];
            }

            Instance.containerUIs[1].CloseContainerInventory();
            return Instance.containerUIs[1];
        }

        public static ContainerUI GetContainerUI(ContainerInventoryManager containerInventoryManager)
        {
            for (int i = 0; i < Instance.containerUIs.Length; i++)
            {
                if (Instance.containerUIs[i].containerInventoryManager == containerInventoryManager)
                    return Instance.containerUIs[i];
            }
            return null;
        }

        public static void SetValidDragPosition(bool valid) => validDragPosition = valid;

        public static void SetActiveSlot(Slot slot) => activeSlot = slot;

        public static void SetLastInventoryInteractedWith(Inventory inventory) => lastInventoryInteractedWith = inventory;

        public static InventoryItem DraggedItem => Instance.draggedItem;
        public static InventorySlot InventorySlotPrefab => Instance.inventorySlotPrefab;

        public static Transform PlayerPocketsParent => Instance.playerPocketsParent;
        public static Transform NPCPocketsParent => Instance.npcPocketsParent;
    }
}
