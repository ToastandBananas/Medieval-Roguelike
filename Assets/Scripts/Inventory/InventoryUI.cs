using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;

public class InventoryUI : MonoBehaviour
{
    public static InventoryUI Instance { get; private set; }

    [SerializeField] InventoryItem draggedItem;

    [Header("Inventories")]
    [SerializeField] Transform playerPocketsParent;
    [SerializeField] Transform npcPocketsParent;
    public List<InventorySlot> playerPocketsSlots { get; private set; }
    public List<InventorySlot> npcPocketsSlots { get; private set; }

    [Header("Player Equipment")]
    [SerializeField] Transform playerEquipmentParent;
    public List<EquipmentSlot> playerEquipmentSlots { get; private set; }

    [Header("NPC Equipment")]
    [SerializeField] Transform npcEquipmentParent;
    public List<EquipmentSlot> npcEquipmentSlots { get; private set; }

    [Header("Container UI")]
    [SerializeField] ContainerUI[] containerUIs;

    [Header("Prefab")]
    [SerializeField] InventorySlot inventorySlotPrefab;

    public Slot activeSlot { get; private set; }

    public bool isDraggingItem { get; private set; }
    public bool validDragPosition { get; private set; }
    public int draggedItemOverlapCount { get; private set; }
    public Slot parentSlotDraggedFrom { get; private set; }
    public Slot overlappedItemsParentSlot { get; private set; }

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
        // npcEquipmentSlots = npcEquipmentParent.gameObject.GetComponentsInChildren<EquipmentSlot>().ToList();

        rectTransform = GetComponent<RectTransform>();

        draggedItem.DisableIconImage();
    }

    void Update()
    {
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
                    SetupDraggedItem(activeInventorySlot.slotCoordinate.parentSlotCoordinate.itemData, activeInventorySlot.GetParentSlot(), activeInventorySlot.myInventory);
                }
                else
                {
                    EquipmentSlot activeEquipmentSlot = activeSlot as EquipmentSlot;
                    if ((activeEquipmentSlot.EquipSlot == EquipSlot.RightHeldItem1 || activeEquipmentSlot.EquipSlot == EquipSlot.RightHeldItem2) && (activeEquipmentSlot.InventoryItem.itemData == null || activeEquipmentSlot.InventoryItem.itemData.Item == null))
                    {
                        EquipmentSlot oppositeWeaponSlot = activeEquipmentSlot.GetOppositeWeaponSlot();
                        if (oppositeWeaponSlot.InventoryItem.itemData.Item != null && oppositeWeaponSlot.InventoryItem.itemData.Item.IsWeapon() && oppositeWeaponSlot.InventoryItem.itemData.Item.Weapon().isTwoHanded)
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

                        if ((activeEquipmentSlot.EquipSlot == EquipSlot.LeftHeldItem1 || activeEquipmentSlot.EquipSlot == EquipSlot.LeftHeldItem2) && activeEquipmentSlot.InventoryItem.itemData.Item.IsWeapon() && activeEquipmentSlot.InventoryItem.itemData.Item.Weapon().isTwoHanded)
                            activeEquipmentSlot.GetOppositeWeaponSlot().InventoryItem.DisableIconImage();
                    }
                }

                activeSlot.GetParentSlot().SetupEmptySlotSprites();
                activeSlot.GetParentSlot().InventoryItem.DisableIconImage();
                activeSlot.GetParentSlot().InventoryItem.ClearStackSizeText();

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
                        activeInventorySlot.myInventory.TryAddDraggedItemAt(activeInventorySlot, draggedItem.itemData);
                    }
                    else
                    {
                        EquipmentSlot activeEquipmentSlot = activeSlot as EquipmentSlot;
                        activeEquipmentSlot.CharacterEquipment.TryAddItemAt(activeEquipmentSlot.EquipSlot, draggedItem.itemData);
                    }
                }
                else if (EventSystem.current.IsPointerOverGameObject() == false)
                {
                    if (parentSlotDraggedFrom is EquipmentSlot) 
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

    public bool DraggedItem_OverlappingMultipleItems()
    {
        int width = draggedItem.itemData.Item.width;
        int height = draggedItem.itemData.Item.height;
        ItemData overlappedItemData = null;
        draggedItemOverlapCount = 0;

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                Slot slotToCheck;
                if (activeSlot is InventorySlot)
                {
                    InventorySlot activeInventorySlot = activeSlot as InventorySlot;
                    slotToCheck = activeInventorySlot.myInventory.GetSlotFromCoordinate(activeInventorySlot.slotCoordinate.coordinate.x - x, activeInventorySlot.slotCoordinate.coordinate.y - y);
                }
                else
                    slotToCheck = activeSlot;

                if (slotToCheck == null)
                    continue;

                if (slotToCheck.IsFull())
                {
                    if (slotToCheck.GetItemData() == draggedItem.itemData)
                        continue;

                    if (overlappedItemData == null)
                    {
                        if (slotToCheck is InventorySlot)
                        {
                            overlappedItemsParentSlot = slotToCheck.GetParentSlot();
                            overlappedItemData = slotToCheck.GetItemData();
                            draggedItemOverlapCount++;
                        }
                        else
                        {
                            overlappedItemsParentSlot = slotToCheck;
                            draggedItemOverlapCount++;
                            return false;
                        }
                    }
                    else if (overlappedItemData != slotToCheck.GetItemData())
                    {
                        draggedItemOverlapCount++;
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

            if (parentEquipmentSlotDraggedFrom.IsHeldItemSlot() && parentEquipmentSlotDraggedFrom.InventoryItem.itemData.Item.IsWeapon() && parentEquipmentSlotDraggedFrom.InventoryItem.itemData.Item.Weapon().isTwoHanded)
            {
                EquipmentSlot oppositeWeaponSlot = parentEquipmentSlotDraggedFrom.GetOppositeWeaponSlot();
                oppositeWeaponSlot.SetFullSlotSprite();
            }
        }

        parentSlotDraggedFrom.InventoryItem.UpdateStackSizeText();

        // Hide the dragged item
        DisableDraggedItem();
    }

    public void SetupDraggedItem(ItemData newItemData, Slot parentSlotDraggedFrom, Inventory inventoryDraggedFrom)
    {
        Cursor.visible = false;
        isDraggingItem = true;

        this.parentSlotDraggedFrom = parentSlotDraggedFrom;

        draggedItem.SetMyInventory(inventoryDraggedFrom);
        draggedItem.SetMyCharacterEquipment(null);
        draggedItem.SetItemData(newItemData);
        draggedItem.UpdateStackSizeText();
        draggedItem.SetupDraggedSprite();
    }

    public void SetupDraggedItem(ItemData newItemData, Slot parentSlotDraggedFrom, CharacterEquipment characterEquipmentDraggedFrom)
    {
        Cursor.visible = false;
        isDraggingItem = true;

        this.parentSlotDraggedFrom = parentSlotDraggedFrom;

        draggedItem.SetMyInventory(null);
        draggedItem.SetMyCharacterEquipment(characterEquipmentDraggedFrom);
        draggedItem.SetItemData(newItemData);
        draggedItem.UpdateStackSizeText();
        draggedItem.SetupDraggedSprite();
    }

    public void DisableDraggedItem()
    {
        if (activeSlot != null)
            activeSlot.RemoveSlotHighlights();

        Cursor.visible = true;
        isDraggingItem = false;
        parentSlotDraggedFrom = null;
        draggedItemOverlapCount = 0;

        StartCoroutine(DelayStopDraggingItem());
        draggedItem.DisableIconImage();
        draggedItem.ClearStackSizeText();
    }

    IEnumerator DelayStopDraggingItem()
    {
        yield return stopDraggingDelay;
        draggedItem.SetItemData(null);
    }

    public void ShowContainerUI(ContainerInventory mainContainerInventory)
    {
        ContainerUI containerUI = GetNextAvailableContainerUI();
        containerUI.ShowContainerInventory(mainContainerInventory, null);
        containerUI.SetupRectTransform(mainContainerInventory);
    }

    public void ShowContainerUI(ContainerInventory mainContainerInventory, Item containerItem)
    {
        ContainerUI containerUI = GetNextAvailableContainerUI();
        containerUI.ShowContainerInventory(mainContainerInventory, containerItem);
        containerUI.SetupRectTransform(mainContainerInventory);
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

    public void CreateSlotVisuals(Inventory inventory, List<InventorySlot> slots, Transform slotsParent)
    {
        if (inventory.SlotVisualsCreated)
        {
            Debug.LogWarning($"Slot visuals for {name}, owned by {inventory.MyUnit.name}, has already been created...");
            return;
        }

        if (slots.Count > 0)
        {
            // Clear out any slots already in the list, so we can start from scratch
            for (int i = 0; i < slots.Count; i++)
            {
                slots[i].RemoveSlotHighlights();
                slots[i].ClearItem();
                slots[i].SetSlotCoordinate(null);
                slots[i].SetMyInventory(null);
                slots[i].gameObject.SetActive(false);
            }

            slots.Clear();
        }

        for (int i = 0; i < inventory.InventoryLayout.AmountOfSlots; i++)
        {
            InventorySlot newSlot = Instantiate(inventorySlotPrefab, slotsParent);
            newSlot.SetSlotCoordinate(inventory.GetSlotCoordinate((i % inventory.InventoryLayout.MaxSlotsPerRow) + 1, Mathf.FloorToInt((float)i / inventory.InventoryLayout.MaxSlotsPerRow) + 1));
            newSlot.name = $"Slot - {newSlot.slotCoordinate.name}";

            newSlot.SetMyInventory(inventory);
            newSlot.InventoryItem.SetMyInventory(inventory);
            slots.Add(newSlot);

            if (i == inventory.InventoryLayout.MaxSlots - 1)
                break;
        }

        inventory.SetSlotVisualsCreated(true);

        inventory.SetupItems();
    }

    public void SetActiveSlot(Slot slot) => activeSlot = slot;

    public InventoryItem DraggedItem => draggedItem;

    public InventorySlot InventorySlotPrefab => inventorySlotPrefab;

    public void SetValidDragPosition(bool valid) => validDragPosition = valid;

    public Transform PlayerPocketsParent => playerPocketsParent;

    public Transform NPCPocketsParent => npcPocketsParent;
}
