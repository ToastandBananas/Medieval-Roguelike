using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using Controls;
using ContextMenu = GeneralUI.ContextMenu;
using UnitSystem;
using GeneralUI;
using TMPro;
using UnitSystem.ActionSystem.Actions;

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
        public static List<InventorySlot> PlayerPocketsSlots { get; private set; }
        public static List<InventorySlot> NpcPocketsSlots { get; private set; }

        [Header("Equipment Parents")]
        [SerializeField] Transform playerEquipmentParent;
        [SerializeField] Transform npcEquipmentParent;
        public static List<EquipmentSlot> PlayerEquipmentSlots { get; private set; }
        public static List<EquipmentSlot> NpcEquipmentSlots { get; private set; }

        [Header("Container UI")]
        [SerializeField] ContainerUI[] containerUIs;

        [Header("Other UI")]
        [SerializeField] TextMeshProUGUI weightText;

        public static Slot activeSlot { get; private set; }

        public static bool IsDraggingItem { get; private set; }
        public static bool ValidDragPosition { get; private set; }
        public static Slot ParentSlotDraggedFrom { get; private set; }

        public static bool PlayerInventoryActive { get; private set; }
        public static bool NpcInventoryActive { get; private set; }

        public static Inventory LastInventoryInteractedWith { get; private set; }

        RectTransform rectTransform;

        static readonly WaitForSeconds stopDraggingDelay = new(0.05f);

        void Awake()
        {
            if (Instance != null)
            {
                Debug.LogError("There's more than one InventoryUI! " + transform + " - " + Instance);
                Destroy(gameObject);
                return;
            }
            Instance = this;

            PlayerPocketsSlots = new List<InventorySlot>();
            NpcPocketsSlots = new List<InventorySlot>();

            PlayerEquipmentSlots = playerEquipmentParent.gameObject.GetComponentsInChildren<EquipmentSlot>().ToList();
            NpcEquipmentSlots = npcEquipmentParent.gameObject.GetComponentsInChildren<EquipmentSlot>().ToList();

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
            if (!IsDraggingItem)
            {
                // Don't allow drag/drop inventory/equipment actions while an action is already queued
                if (!UnitManager.player.IsMyTurn || UnitManager.player.UnitActionHandler.QueuedActions.Count > 0)
                    return;

                // If we select an item
                if (GameControls.gamePlayActions.menuSelect.WasPressed)
                {
                    if (activeSlot == null || !activeSlot.IsFull())
                        return;

                    // "Pickup" the item by hiding the item's sprite and showing that same sprite on the draggedItem object
                    if (activeSlot is InventorySlot)
                    {
                        InventorySlot activeInventorySlot = activeSlot as InventorySlot;
                        SetupDraggedItem(activeInventorySlot.slotCoordinate.ParentSlotCoordinate.ItemData, activeInventorySlot.ParentSlot(), activeInventorySlot.myInventory);
                        LastInventoryInteractedWith = activeInventorySlot.myInventory;
                    }
                    else
                    {
                        EquipmentSlot activeEquipmentSlot = activeSlot as EquipmentSlot;
                        if ((activeEquipmentSlot.EquipSlot == EquipSlot.RightHeldItem1 || activeEquipmentSlot.EquipSlot == EquipSlot.RightHeldItem2) && (activeEquipmentSlot.InventoryItem.ItemData == null || activeEquipmentSlot.InventoryItem.ItemData.Item == null))
                        {
                            EquipmentSlot oppositeWeaponSlot = activeEquipmentSlot.GetOppositeWeaponSlot();
                            if (oppositeWeaponSlot.InventoryItem.ItemData.Item != null && oppositeWeaponSlot.InventoryItem.ItemData.Item is Item_Weapon && oppositeWeaponSlot.InventoryItem.ItemData.Item.Weapon.IsTwoHanded)
                            {
                                SetupDraggedItem(oppositeWeaponSlot.InventoryItem.ItemData, oppositeWeaponSlot, oppositeWeaponSlot.InventoryItem.MyUnitEquipment);
                                oppositeWeaponSlot.InventoryItem.DisableIconImage();
                            }
                            else
                                SetupDraggedItem(activeEquipmentSlot.InventoryItem.ItemData, activeSlot, activeSlot.InventoryItem.MyUnitEquipment);
                        }
                        else
                        {
                            SetupDraggedItem(activeEquipmentSlot.InventoryItem.ItemData, activeSlot, activeSlot.InventoryItem.MyUnitEquipment);

                            if ((activeEquipmentSlot.EquipSlot == EquipSlot.LeftHeldItem1 || activeEquipmentSlot.EquipSlot == EquipSlot.LeftHeldItem2) && activeEquipmentSlot.InventoryItem.ItemData.Item is Item_Weapon && activeEquipmentSlot.InventoryItem.ItemData.Item.Weapon.IsTwoHanded)
                                activeEquipmentSlot.GetOppositeWeaponSlot().InventoryItem.DisableIconImage();
                        }

                        LastInventoryInteractedWith = null;
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
                if (UnitManager.player.UnitActionHandler.QueuedActions.Count > 0)
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
                    if (activeSlot != null && ValidDragPosition && activeSlot != ParentSlotDraggedFrom)
                    {
                        if (activeSlot is InventorySlot)
                        {
                            InventorySlot activeInventorySlot = activeSlot as InventorySlot;
                            activeInventorySlot.myInventory.TryAddItemAt(activeInventorySlot.slotCoordinate, draggedItem.ItemData, UnitManager.player);
                            LastInventoryInteractedWith = activeInventorySlot.myInventory;
                        }
                        else if (activeSlot is EquipmentSlot)
                        {
                            // If drag/dropping an item onto an equipped backpack
                            if ((draggedItem.ItemData.Item is Item_Equipment == false || draggedItem.ItemData.Item.Equipment.EquipSlot != EquipSlot.Back) && activeSlot.EquipmentSlot.EquipSlot == EquipSlot.Back && UnitManager.player.UnitEquipment.HumanoidEquipment.BackpackEquipped)
                            {
                                if (UnitManager.player.BackpackInventoryManager.TryAddItem(draggedItem.ItemData, UnitManager.player))
                                {
                                    if (UnitManager.player.UnitEquipment.ItemDataEquipped(draggedItem.ItemData))
                                        UnitManager.player.UnitEquipment.RemoveEquipment(draggedItem.ItemData);
                                }
                                else
                                    ReplaceDraggedItem();
                            }
                            else if ((draggedItem.ItemData.Item is Item_Equipment == false || draggedItem.ItemData.Item.Equipment.EquipSlot != EquipSlot.Belt) && activeSlot.EquipmentSlot.EquipSlot == EquipSlot.Belt && UnitManager.player.UnitEquipment.HumanoidEquipment.BeltBagEquipped)
                            {
                                if (UnitManager.player.BeltInventoryManager.TryAddItem(draggedItem.ItemData, UnitManager.player))
                                {
                                    if (UnitManager.player.UnitEquipment.ItemDataEquipped(draggedItem.ItemData))
                                        UnitManager.player.UnitEquipment.RemoveEquipment(draggedItem.ItemData);
                                }
                                else
                                    ReplaceDraggedItem();
                            }
                            else
                            {
                                EquipmentSlot activeEquipmentSlot = activeSlot as EquipmentSlot;
                                UnitManager.player.UnitActionHandler.GetAction<Action_Equip>().QueueAction(draggedItem.ItemData, activeEquipmentSlot.EquipSlot, ParentSlotDraggedFrom != null && ParentSlotDraggedFrom is ContainerEquipmentSlot ? ParentSlotDraggedFrom.EquipmentSlot.ContainerEquipmentSlot.containerInventoryManager : null);
                                LastInventoryInteractedWith = null;
                            }

                            DisableDraggedItem();
                        }
                    }
                    else if (!EventSystem.current.IsPointerOverGameObject())
                    {
                        if (ParentSlotDraggedFrom == null)
                        {
                            Unit unit = null;
                            if (draggedItem.MyInventory != null)
                                unit = draggedItem.MyInventory.MyUnit;

                            DropItemManager.DropItem(draggedItem.MyInventory, unit, draggedItem.ItemData);
                        }
                        else if (ParentSlotDraggedFrom is EquipmentSlot)
                        {
                            EquipmentSlot equipmentSlotDraggedFrom = ParentSlotDraggedFrom as EquipmentSlot;
                            DropItemManager.DropItem(equipmentSlotDraggedFrom.UnitEquipment, equipmentSlotDraggedFrom.EquipSlot);
                        }
                        else
                        {
                            InventorySlot inventorySlotDraggedFrom = ParentSlotDraggedFrom as InventorySlot;
                            DropItemManager.DropItem(inventorySlotDraggedFrom.myInventory, inventorySlotDraggedFrom.myInventory.MyUnit, draggedItem.ItemData);
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

            if (!focusedSlotCoordinate.MyInventory.InventoryLayout.HasStandardSlotSize)
            {
                overlappedItemsParentSlotCoordinate = focusedSlotCoordinate.MyInventory.GetSlotCoordinate(focusedSlotCoordinate.Coordinate.x, focusedSlotCoordinate.Coordinate.y);
                if (overlappedItemsParentSlotCoordinate.IsFull && overlappedItemsParentSlotCoordinate.ItemData != itemData)
                    overlappedItemCount++;
                return false;
            }

            ItemData overlappedItemData = null;
            for (int x = 0; x < itemData.Item.Width; x++)
            {
                for (int y = 0; y < itemData.Item.Height; y++)
                {
                    SlotCoordinate slotCoordinateToCheck;
                    if (focusedSlotCoordinate.MyInventory != null)
                    {
                        slotCoordinateToCheck = focusedSlotCoordinate.MyInventory.GetSlotCoordinate(focusedSlotCoordinate.Coordinate.x - x, focusedSlotCoordinate.Coordinate.y - y);
                    }
                    else
                        slotCoordinateToCheck = focusedSlotCoordinate;

                    if (slotCoordinateToCheck == null)
                        continue;

                    if (slotCoordinateToCheck.IsFull)
                    {
                        if (slotCoordinateToCheck.ParentSlotCoordinate.ItemData == itemData)
                            continue;

                        if (overlappedItemData == null)
                        {
                            if (slotCoordinateToCheck.MyInventory != null)
                            {
                                overlappedItemsParentSlotCoordinate = slotCoordinateToCheck.ParentSlotCoordinate;
                                overlappedItemData = slotCoordinateToCheck.ParentSlotCoordinate.ItemData;
                                overlappedItemCount++;
                            }
                            else
                            {
                                overlappedItemsParentSlotCoordinate = slotCoordinateToCheck;
                                overlappedItemCount++;
                                return false;
                            }
                        }
                        else if (overlappedItemData != slotCoordinateToCheck.ParentSlotCoordinate.ItemData)
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
            if (ParentSlotDraggedFrom != null && ParentSlotDraggedFrom.GetItemData() != Instance.draggedItem.ItemData && Instance.draggedItem.ItemData.IsEqual(ParentSlotDraggedFrom.GetItemData()))
            {
                if (ParentSlotDraggedFrom is InventorySlot)
                {
                    if (!ParentSlotDraggedFrom.InventoryItem.MyInventory.TryAddItemAt(ParentSlotDraggedFrom.InventoryItem.MyInventory.GetSlotCoordinateFromItemData(ParentSlotDraggedFrom.GetItemData()), Instance.draggedItem.ItemData, UnitManager.player))
                    {
                        if (!ParentSlotDraggedFrom.InventoryItem.MyInventory.MyUnit.UnitInventoryManager.TryAddItemToInventories(Instance.draggedItem.ItemData))
                            DropItemManager.DropItem(Instance.draggedItem.MyInventory, UnitManager.player, Instance.draggedItem.ItemData);
                    }
                }
                else
                {
                    EquipmentSlot equipmentSlot = ParentSlotDraggedFrom as EquipmentSlot;
                    if (!equipmentSlot.UnitEquipment.TryAddItemAt(equipmentSlot.EquipSlot, Instance.draggedItem.ItemData))
                    {
                        if (!equipmentSlot.UnitEquipment.MyUnit.UnitInventoryManager.TryAddItemToInventories(Instance.draggedItem.ItemData))
                            DropItemManager.DropItem(Instance.draggedItem.MyInventory, UnitManager.player, Instance.draggedItem.ItemData);
                    }
                }
            }
            // If replacing the item normally is possible
            else if (ParentSlotDraggedFrom != null && ParentSlotDraggedFrom.GetItemData() == Instance.draggedItem.ItemData)
            {
                ParentSlotDraggedFrom.ShowSlotImage();

                if (ParentSlotDraggedFrom is InventorySlot)
                {
                    InventorySlot parentInventorySlotDraggedFrom = ParentSlotDraggedFrom as InventorySlot;
                    parentInventorySlotDraggedFrom.SetupFullSlotSprites();
                }
                else
                {
                    EquipmentSlot parentEquipmentSlotDraggedFrom = ParentSlotDraggedFrom as EquipmentSlot;
                    parentEquipmentSlotDraggedFrom.SetFullSlotSprite();

                    if (parentEquipmentSlotDraggedFrom.IsHeldItemSlot && parentEquipmentSlotDraggedFrom.InventoryItem.ItemData.Item is Item_Weapon && parentEquipmentSlotDraggedFrom.InventoryItem.ItemData.Item.Weapon.IsTwoHanded)
                    {
                        EquipmentSlot oppositeWeaponSlot = parentEquipmentSlotDraggedFrom.GetOppositeWeaponSlot();
                        oppositeWeaponSlot.SetFullSlotSprite();
                    }
                    else if (parentEquipmentSlotDraggedFrom.EquipSlot == EquipSlot.Quiver && Instance.draggedItem.ItemData.Item is Item_Quiver)
                        parentEquipmentSlotDraggedFrom.InventoryItem.QuiverInventoryItem.UpdateQuiverSprites();
                }

                ParentSlotDraggedFrom.InventoryItem.UpdateStackSizeVisuals();
            }
            else // Otherwise just try to add it to one of the Unit's inventories
            {
                if (!UnitManager.player.UnitInventoryManager.TryAddItemToInventories(Instance.draggedItem.ItemData))
                    DropItemManager.DropItem(Instance.draggedItem.MyInventory, UnitManager.player, Instance.draggedItem.ItemData);
            }

            DisableDraggedItem();
        }

        public static void SetupDraggedItem(ItemData newItemData, Slot theParentSlotDraggedFrom, Inventory inventoryDraggedFrom)
        {
            SplitStack.Instance.Close();
            ContextMenu.DisableContextMenu(true);
            TooltipManager.ClearInventoryTooltips();

            Cursor.visible = false;
            IsDraggingItem = true;

            ParentSlotDraggedFrom = theParentSlotDraggedFrom;
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
            IsDraggingItem = true;

            ParentSlotDraggedFrom = theParentSlotDraggedFrom;

            if (theParentSlotDraggedFrom is ContainerEquipmentSlot)
            {
                ContainerEquipmentSlot containerEquipmentSlot = theParentSlotDraggedFrom as ContainerEquipmentSlot;
                if (containerEquipmentSlot.containerInventoryManager.ParentInventory.SlotVisualsCreated)
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
            if (!IsDraggingItem)
                return;

            if (activeSlot != null)
                activeSlot.RemoveSlotHighlights();

            Cursor.visible = true;
            IsDraggingItem = false;
            ParentSlotDraggedFrom = null;
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
            if (IsDraggingItem)
                ReplaceDraggedItem();

            Instance.playerInventoryUIParent.SetActive(!Instance.playerInventoryUIParent.activeSelf);
            PlayerInventoryActive = Instance.playerInventoryUIParent.activeSelf;

            if (!PlayerInventoryActive)
            {
                activeSlot = null;
                LastInventoryInteractedWith = null;
                ContextMenu.DisableContextMenu();
                SplitStack.Instance.Close();
                TooltipManager.ClearInventoryTooltips();
                CloseAllContainerUI();

                if (NpcInventoryActive)
                    ToggleNPCInventory();
            }
        }

        public static void ClearNPCInventorySlots()
        {
            if (!NpcInventoryActive)
                return;

            if (IsDraggingItem)
                ReplaceDraggedItem(); 
            
            if (NpcEquipmentSlots.Count > 0)
                NpcEquipmentSlots[0].UnitEquipment.OnCloseNPCInventory();

            if (NpcPocketsSlots.Count > 0)
                NpcPocketsSlots[0].myInventory.OnCloseNPCInventory();
        }

        public static void ToggleNPCInventory()
        {
            ClearNPCInventorySlots();

            Instance.npcInventoryUIParent.SetActive(!Instance.npcInventoryUIParent.activeSelf);
            NpcInventoryActive = Instance.npcInventoryUIParent.activeSelf;

            if (NpcInventoryActive && !PlayerInventoryActive)
                TogglePlayerInventory();
            else if (!NpcInventoryActive)
            {
                activeSlot = null;
                ContextMenu.DisableContextMenu();
                SplitStack.Instance.Close();
                TooltipManager.ClearInventoryTooltips();
                CloseAllContainerUI();
            }
        }

        public static void ShowContainerUI(InventoryManager_Container containerInventoryManager, Item containerItem)
        {
            for (int i = 0; i < Instance.containerUIs.Length; i++)
            {
                if (Instance.containerUIs[i].containerInventoryManager == containerInventoryManager)
                    return;
            }

            if (!PlayerInventoryActive)
                TogglePlayerInventory();

            GetNextAvailableContainerUI().ShowContainerInventory(containerInventoryManager.ParentInventory, containerItem);
        }

        public static void CloseAllContainerUI()
        {
            if (IsDraggingItem)
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
                if (!Instance.containerUIs[i].gameObject.activeSelf)
                    return Instance.containerUIs[i];
            }

            Instance.containerUIs[1].CloseContainerInventory();
            return Instance.containerUIs[1];
        }

        public static ContainerUI GetContainerUI(InventoryManager_Container containerInventoryManager)
        {
            for (int i = 0; i < Instance.containerUIs.Length; i++)
            {
                if (Instance.containerUIs[i].containerInventoryManager == containerInventoryManager)
                    return Instance.containerUIs[i];
            }
            return null;
        }

        public static void UpdatePlayerCarryWeightText() => Instance.weightText.text = $"{UnitManager.player.Stats.CurrentCarryWeight} / {UnitManager.player.Stats.MaxCarryWeight} lbs";

        public static void SetValidDragPosition(bool valid) => ValidDragPosition = valid;

        public static void SetActiveSlot(Slot slot) => activeSlot = slot;

        public static void SetLastInventoryInteractedWith(Inventory inventory) => LastInventoryInteractedWith = inventory;

        public static InventoryItem DraggedItem => Instance.draggedItem;
        public static InventorySlot InventorySlotPrefab => Instance.inventorySlotPrefab;

        public static Transform PlayerPocketsParent => Instance.playerPocketsParent;
        public static Transform NPCPocketsParent => Instance.npcPocketsParent;
    }
}
