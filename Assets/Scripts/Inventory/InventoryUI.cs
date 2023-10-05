using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;

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
    public List<InventorySlot> playerPocketsSlots { get; private set; }
    public List<InventorySlot> npcPocketsSlots { get; private set; }

    [Header("Equipment Parents")]
    [SerializeField] Transform playerEquipmentParent;
    [SerializeField] Transform npcEquipmentParent;
    public List<EquipmentSlot> playerEquipmentSlots { get; private set; }
    public List<EquipmentSlot> npcEquipmentSlots { get; private set; }

    [Header("Container UI")]
    [SerializeField] ContainerUI[] containerUIs;

    public Slot activeSlot { get; private set; }

    public bool isDraggingItem { get; private set; }
    public bool validDragPosition { get; private set; }
    public Slot parentSlotDraggedFrom { get; private set; }

    RectTransform rectTransform;

    WaitForSeconds stopDraggingDelay = new WaitForSeconds(0.05f);

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
    }

    void Update()
    {
        if (GameControls.gamePlayActions.toggleInventory.WasPressed)
            TogglePlayerInventory();

        // If we're not already dragging an item
        if (isDraggingItem == false)
        {
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
                }
                else
                {
                    EquipmentSlot activeEquipmentSlot = activeSlot as EquipmentSlot;
                    if ((activeEquipmentSlot.EquipSlot == EquipSlot.RightHeldItem1 || activeEquipmentSlot.EquipSlot == EquipSlot.RightHeldItem2) && (activeEquipmentSlot.InventoryItem.itemData == null || activeEquipmentSlot.InventoryItem.itemData.Item == null))
                    {
                        EquipmentSlot oppositeWeaponSlot = activeEquipmentSlot.GetOppositeWeaponSlot();
                        if (oppositeWeaponSlot.InventoryItem.itemData.Item != null && oppositeWeaponSlot.InventoryItem.itemData.Item is Weapon && oppositeWeaponSlot.InventoryItem.itemData.Item.Weapon.IsTwoHanded)
                        {
                            SetupDraggedItem(oppositeWeaponSlot.InventoryItem.itemData, oppositeWeaponSlot, oppositeWeaponSlot.InventoryItem.myCharacterEquipment);
                            oppositeWeaponSlot.InventoryItem.DisableIconImage();
                        }
                        else
                            SetupDraggedItem(activeEquipmentSlot.InventoryItem.itemData, activeSlot, activeSlot.InventoryItem.myCharacterEquipment);
                    }
                    else
                    {
                        SetupDraggedItem(activeEquipmentSlot.InventoryItem.itemData, activeSlot, activeSlot.InventoryItem.myCharacterEquipment);

                        if ((activeEquipmentSlot.EquipSlot == EquipSlot.LeftHeldItem1 || activeEquipmentSlot.EquipSlot == EquipSlot.LeftHeldItem2) && activeEquipmentSlot.InventoryItem.itemData.Item is Weapon && activeEquipmentSlot.InventoryItem.itemData.Item.Weapon.IsTwoHanded)
                            activeEquipmentSlot.GetOppositeWeaponSlot().InventoryItem.DisableIconImage();
                    }
                }

                activeSlot.ParentSlot().SetupEmptySlotSprites();
                activeSlot.ParentSlot().InventoryItem.DisableIconImage();
                activeSlot.ParentSlot().InventoryItem.ClearStackSizeText();

                activeSlot.HighlightSlots();
            }
        }
        else // If we are dragging an item
        {
            // The dragged item should follow the mouse position
            Vector2 offset = draggedItem.GetDraggedItemOffset();
            RectTransformUtility.ScreenPointToLocalPointInRectangle(rectTransform, Input.mousePosition, null, out Vector2 localMousePosition);
            draggedItem.RectTransform().localPosition = localMousePosition + offset;

            // If we try to place an item
            if (GameControls.gamePlayActions.menuSelect.WasPressed)
            {
                // Try placing the item
                if (activeSlot != null)
                {
                    if (activeSlot is InventorySlot)
                    {
                        InventorySlot activeInventorySlot = activeSlot as InventorySlot;
                        activeInventorySlot.myInventory.TryAddItemAt(activeInventorySlot.slotCoordinate, draggedItem.itemData);
                    }
                    else
                    {
                        EquipmentSlot activeEquipmentSlot = activeSlot as EquipmentSlot;
                        activeEquipmentSlot.CharacterEquipment.TryAddItemAt(activeEquipmentSlot.EquipSlot, draggedItem.itemData);
                    }
                }
                else if (EventSystem.current.IsPointerOverGameObject() == false)
                {
                    if (parentSlotDraggedFrom == null)
                    {
                        Unit unit = null;
                        if (draggedItem.myInventory != null)
                            unit = draggedItem.myInventory.MyUnit;

                        DropItemManager.DropItem(unit, draggedItem.myInventory, draggedItem.itemData);
                    }
                    else if (parentSlotDraggedFrom is EquipmentSlot) 
                    {
                        EquipmentSlot equipmentSlotDraggedFrom = parentSlotDraggedFrom as EquipmentSlot;
                        DropItemManager.DropItem(equipmentSlotDraggedFrom.CharacterEquipment, equipmentSlotDraggedFrom.EquipSlot);
                    }
                    else
                    {
                        InventorySlot inventorySlotDraggedFrom = parentSlotDraggedFrom as InventorySlot;
                        DropItemManager.DropItem(inventorySlotDraggedFrom.myInventory.MyUnit, inventorySlotDraggedFrom.myInventory, draggedItem.itemData);
                    }
                }
            }
        }
    }

    public bool OverlappingMultipleItems(SlotCoordinate focusedSlotCoordinate, ItemData itemData, out SlotCoordinate overlappedItemsParentSlotCoordinate, out int overlappedItemCount)
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

    public void ReplaceDraggedItem()
    {
        // No need to setup the ItemData since it hasn't changed, so just show the item's sprite and change the slot's color/remove highlighting
        if (activeSlot != null)
            activeSlot.RemoveSlotHighlights();

        // If replacing a split stack
        if (parentSlotDraggedFrom != null && parentSlotDraggedFrom.GetItemData() != draggedItem.itemData && draggedItem.itemData.IsEqual(parentSlotDraggedFrom.GetItemData()))
        {
            if (parentSlotDraggedFrom is InventorySlot)
            {
                if (parentSlotDraggedFrom.InventoryItem.myInventory.TryAddItemAt(parentSlotDraggedFrom.InventoryItem.myInventory.GetSlotCoordinateFromItemData(parentSlotDraggedFrom.GetItemData()), draggedItem.itemData) == false)
                {
                    if (parentSlotDraggedFrom.InventoryItem.myInventory.MyUnit.TryAddItemToInventories(draggedItem.itemData) == false)
                        DropItemManager.DropItem(UnitManager.Instance.player, draggedItem.myInventory, draggedItem.itemData);
                }
            }
            else
            {
                EquipmentSlot equipmentSlot = parentSlotDraggedFrom as EquipmentSlot;
                if (parentSlotDraggedFrom.InventoryItem.myCharacterEquipment.TryAddItemAt(equipmentSlot.EquipSlot, draggedItem.itemData) == false)
                {
                    if (parentSlotDraggedFrom.InventoryItem.myCharacterEquipment.MyUnit.TryAddItemToInventories(draggedItem.itemData) == false)
                        DropItemManager.DropItem(UnitManager.Instance.player, draggedItem.myInventory, draggedItem.itemData);
                }
            }
        }
        // If replacing the item normally is possible
        else if (parentSlotDraggedFrom != null && parentSlotDraggedFrom.GetItemData() == draggedItem.itemData)
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
                else if (parentEquipmentSlotDraggedFrom.EquipSlot == EquipSlot.Quiver && draggedItem.itemData.Item is Quiver)
                    parentEquipmentSlotDraggedFrom.InventoryItem.QuiverInventoryItem.UpdateQuiverSprites();
            }

            parentSlotDraggedFrom.InventoryItem.UpdateStackSizeVisuals();
        }
        else // Otherwise just try to add it to one of the Unit's inventories
        {
            if (UnitManager.Instance.player.TryAddItemToInventories(draggedItem.itemData) == false)
                DropItemManager.DropItem(UnitManager.Instance.player, draggedItem.myInventory, draggedItem.itemData);
        }

        DisableDraggedItem();
    }

    public void SetupDraggedItem(ItemData newItemData, Slot parentSlotDraggedFrom, Inventory inventoryDraggedFrom)
    {
        SplitStack.Instance.Close();
        ContextMenu.Instance.DisableContextMenu(true);

        Cursor.visible = false;
        isDraggingItem = true;

        this.parentSlotDraggedFrom = parentSlotDraggedFrom;
        if (parentSlotDraggedFrom == null)
            newItemData.SetInventorySlotCoordinate(null);

        draggedItem.SetMyInventory(inventoryDraggedFrom);
        draggedItem.SetMyCharacterEquipment(null);
        draggedItem.SetItemData(newItemData);
        draggedItem.UpdateStackSizeVisuals();
        draggedItem.SetupDraggedSprite();
    }

    public void SetupDraggedItem(ItemData newItemData, Slot parentSlotDraggedFrom, CharacterEquipment characterEquipmentDraggedFrom)
    {
        SplitStack.Instance.Close();
        ContextMenu.Instance.DisableContextMenu(true);

        Cursor.visible = false;
        isDraggingItem = true;

        this.parentSlotDraggedFrom = parentSlotDraggedFrom;

        if (parentSlotDraggedFrom is ContainerEquipmentSlot)
        {
            ContainerEquipmentSlot containerEquipmentSlot = parentSlotDraggedFrom as ContainerEquipmentSlot;
            if (containerEquipmentSlot.EquipSlot == EquipSlot.Back)
            {
                if (containerEquipmentSlot.containerInventoryManager.ParentInventory.SlotVisualsCreated)
                    GetContainerUI(containerEquipmentSlot.containerInventoryManager).CloseContainerInventory();
            }
            else if (containerEquipmentSlot.EquipSlot == EquipSlot.Quiver)
            {
                if (characterEquipmentDraggedFrom.MyUnit.QuiverInventoryManager.ParentInventory.SlotVisualsCreated)
                    GetContainerUI(characterEquipmentDraggedFrom.MyUnit.QuiverInventoryManager).CloseContainerInventory();

                characterEquipmentDraggedFrom.GetEquipmentSlot(EquipSlot.Quiver).InventoryItem.QuiverInventoryItem.HideQuiverSprites();
            }
        }

        draggedItem.SetMyInventory(null);
        draggedItem.SetMyCharacterEquipment(characterEquipmentDraggedFrom);
        draggedItem.SetItemData(newItemData);
        draggedItem.UpdateStackSizeVisuals();
        draggedItem.SetupDraggedSprite();
    }

    public void DisableDraggedItem()
    {
        if (isDraggingItem == false)
            return;

        if (activeSlot != null)
            activeSlot.RemoveSlotHighlights();

        Cursor.visible = true;
        isDraggingItem = false;
        parentSlotDraggedFrom = null;
        draggedItem.SetItemData(null);
        draggedItem.SetMyInventory(null);

        StartCoroutine(DelayStopDraggingItem());
        draggedItem.DisableIconImage();
        draggedItem.ClearStackSizeText();
    }

    IEnumerator DelayStopDraggingItem()
    {
        yield return stopDraggingDelay;
        draggedItem.SetItemData(null);
    }

    public void TogglePlayerInventory()
    {
        if (isDraggingItem)
            ReplaceDraggedItem();

        playerInventoryUIParent.SetActive(!playerInventoryUIParent.activeSelf);

        if (playerInventoryUIParent.activeSelf == false)
        {
            activeSlot = null;
            ContextMenu.Instance.DisableContextMenu();
            SplitStack.Instance.Close();
            CloseAllContainerUI();

            if (npcInventoryUIParent.activeSelf)
                ToggleNPCInventory();
        }
    }

    public void ToggleNPCInventory()
    {
        if (isDraggingItem)
            ReplaceDraggedItem();

        npcInventoryUIParent.SetActive(!npcInventoryUIParent.activeSelf);

        if (playerInventoryUIParent.activeSelf == false)
        {
            activeSlot = null;
            ContextMenu.Instance.DisableContextMenu();
            SplitStack.Instance.Close();
            CloseAllContainerUI();
        }
    }

    public void ShowContainerUI(ContainerInventoryManager containerInventoryManager, Item containerItem)
    {
        for (int i = 0; i < containerUIs.Length; i++)
        {
            if (containerUIs[i].containerInventoryManager == containerInventoryManager)
                return;
        }

        ContainerUI containerUI = GetNextAvailableContainerUI();
        containerUI.ShowContainerInventory(containerInventoryManager.ParentInventory, containerItem);
        containerUI.SetupRectTransform(containerInventoryManager.ParentInventory);
    }

    public void CloseAllContainerUI()
    {
        if (isDraggingItem)
            ReplaceDraggedItem();

        for (int i = 0; i < containerUIs.Length; i++)
        {
            containerUIs[i].CloseContainerInventory();
        }
    }

    ContainerUI GetNextAvailableContainerUI()
    {
        for (int i = 0; i < containerUIs.Length; i++)
        {
            if (containerUIs[i].gameObject.activeSelf == false)
                return containerUIs[i];
        }
        return containerUIs[1];
    }

    public ContainerUI GetContainerUI(ContainerInventoryManager containerInventoryManager)
    {
        for (int i = 0; i < containerUIs.Length; i++)
        {
            if (containerUIs[i].containerInventoryManager == containerInventoryManager)
                return containerUIs[i];
        }
        return null;
    }

    public void SetValidDragPosition(bool valid) => validDragPosition = valid;

    public void SetActiveSlot(Slot slot) => activeSlot = slot;

    public InventoryItem DraggedItem => draggedItem;
    public InventorySlot InventorySlotPrefab => inventorySlotPrefab;

    public Transform PlayerPocketsParent => playerPocketsParent;
    public Transform NPCPocketsParent => npcPocketsParent;
}
