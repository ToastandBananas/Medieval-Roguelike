using UnityEngine;

public enum EquipSlot { LeftHeldItem, RightHeldItem, Helm, BodyArmor, LegArmor, Gloves, Boots, Backpack }

public class EquipmentSlot : Slot
{
    [Header("Equipment")]
    [SerializeField] CharacterEquipment myCharacterEquipment;
    [SerializeField] EquipSlot equipSlot = global::EquipSlot.RightHeldItem;

    void Awake()
    {
        parentSlot = this;
        inventoryItem.SetMyCharacterEquipment(myCharacterEquipment);
    }

    void Start()
    {
        myCharacterEquipment.slots.Add(this);
    }

    public override bool IsFull() => inventoryItem.itemData != null && inventoryItem.itemData.Item() != null;

    public override void ClearItem()
    {
        // Hide the item's sprite
        HideSlotImage();

        // Setup the empty slot sprite
        SetEmptySlotSprite();

        // Clear the stack size text
        inventoryItem.ClearStackSizeText();

        // Clear out the slot's item data
        inventoryItem.SetItemData(null);
    }

    public override void HighlightSlots()
    {
        bool validSlot = false;
        Item draggedItem = InventoryUI.Instance.DraggedItem().itemData.Item();
        InventoryUI.Instance.DraggedItem_OverlappingMultipleItems();

        if (draggedItem.IsEquipment())
        {
            if (draggedItem.Equipment().EquipSlot() == equipSlot)
                validSlot = true;
            else if ((draggedItem.IsWeapon() || draggedItem.IsShield()) && (equipSlot == global::EquipSlot.LeftHeldItem || equipSlot == global::EquipSlot.RightHeldItem))
                validSlot = true;
        }

        InventoryUI.Instance.SetValidDragPosition(validSlot);

        SetEmptySlotSprite();

        if (validSlot)
            image.color = Color.green;
        else
            image.color = Color.red;
    }

    public override void RemoveSlotHighlights()
    {
        if (IsFull() && InventoryUI.Instance.DraggedItem().itemData != inventoryItem.itemData)
            SetFullSlotSprite();

        image.color = Color.white;
    }

    public override void SetupEmptySlotSprites() => SetEmptySlotSprite(); 

    public override ItemData GetItemData() => inventoryItem.itemData;

    public CharacterEquipment MyCharacterEquipment() => myCharacterEquipment;

    public EquipSlot EquipSlot() => equipSlot;
}
