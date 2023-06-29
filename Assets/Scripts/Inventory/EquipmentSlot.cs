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

    public override bool IsFull()
    {
        if (inventoryItem.itemData != null && inventoryItem.itemData.Item() != null)
            return true;
        else if (IsWeaponSlot())
        {
            EquipmentSlot oppositeWeaponSlot = GetOppositeWeaponSlot();
            if (oppositeWeaponSlot.inventoryItem.itemData != null && oppositeWeaponSlot.inventoryItem.itemData.Item() != null && oppositeWeaponSlot.inventoryItem.itemData.Item().Weapon().isTwoHanded)
                return true;
        }
        return false;
    }

    public override void ClearItem()
    {
        // Hide the item's sprite
        HideSlotImage();

        // Setup the empty slot sprite
        SetEmptySlotSprite();

        if (IsFull() && inventoryItem.itemData.Item().IsWeapon() && inventoryItem.itemData.Item().Weapon().isTwoHanded)
        {
            EquipmentSlot oppositeWeaponSlot = GetOppositeWeaponSlot();
            oppositeWeaponSlot.HideSlotImage();
            oppositeWeaponSlot.SetEmptySlotSprite();
        }

        // Clear the stack size text
        inventoryItem.ClearStackSizeText();

        // Clear out the slot's item data
        inventoryItem.SetItemData(null);
    }

    public EquipmentSlot GetOppositeWeaponSlot()
    {
        if (equipSlot == global::EquipSlot.RightHeldItem)
            return myCharacterEquipment.GetEquipmentSlot(global::EquipSlot.LeftHeldItem);
        else if (equipSlot == global::EquipSlot.LeftHeldItem)
            return myCharacterEquipment.GetEquipmentSlot(global::EquipSlot.RightHeldItem);
        return null;
    }

    public override void ShowSlotImage()
    {
        if (inventoryItem.itemData == null || inventoryItem.itemData.Item() == null)
        {
            Debug.LogWarning("There is no item in this slot...");
            return;
        }

        if (inventoryItem.itemData.Item().inventorySprite == null)
        {
            Debug.LogError($"Sprite for {inventoryItem.itemData.Item().name} is not yet set in the item's ScriptableObject");
            return;
        }

        if (IsWeaponSlot())
        {
            if (inventoryItem.itemData.Item().Weapon().isTwoHanded)
            {
                EquipmentSlot oppositeWeaponSlot = GetOppositeWeaponSlot();
                if (EquipSlot() == global::EquipSlot.LeftHeldItem)
                {
                    inventoryItem.SetupSprite(true);
                    oppositeWeaponSlot.inventoryItem.SetupSprite(false);
                }
                else
                {
                    inventoryItem.SetupSprite(false);
                    oppositeWeaponSlot.inventoryItem.SetupSprite(true);
                }
            }
            else
                inventoryItem.SetupSprite(true);
        }
        else
            inventoryItem.SetupSprite(true);
    }

    public bool IsWeaponSlot() => equipSlot == global::EquipSlot.LeftHeldItem || equipSlot == global::EquipSlot.RightHeldItem;

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

    public override void SetupEmptySlotSprites()
    {
        SetEmptySlotSprite();
        if (IsWeaponSlot() && IsFull())
        { 
            if (inventoryItem.itemData != null && inventoryItem.itemData.Item() != null && inventoryItem.itemData.Item().Weapon().isTwoHanded)
                GetOppositeWeaponSlot().SetEmptySlotSprite();
            else
            {
                EquipmentSlot oppositeWeaponSlot = GetOppositeWeaponSlot();
                if (oppositeWeaponSlot.inventoryItem.itemData != null && oppositeWeaponSlot.inventoryItem.itemData.Item() != null && oppositeWeaponSlot.inventoryItem.itemData.Item().Weapon().isTwoHanded)
                    oppositeWeaponSlot.SetEmptySlotSprite();
            }
        }
    }

    public override ItemData GetItemData() => inventoryItem.itemData;

    public CharacterEquipment MyCharacterEquipment() => myCharacterEquipment;

    public EquipSlot EquipSlot() => equipSlot;
}
