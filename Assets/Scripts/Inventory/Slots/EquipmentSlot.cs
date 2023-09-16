using UnityEngine;

public class EquipmentSlot : Slot
{
    [Header("Equipment")]
    [SerializeField] EquipSlot equipSlot = global::EquipSlot.RightHeldItem1;
    CharacterEquipment myCharacterEquipment;

    void Awake()
    {
        inventoryItem.SetMyCharacterEquipment(myCharacterEquipment);
    }

    public override bool IsFull() 
    {
        if (inventoryItem.itemData != null && inventoryItem.itemData.Item != null)
            return true;

        if (equipSlot == global::EquipSlot.RightHeldItem1 && GetOppositeWeaponSlot().inventoryItem.itemData != null && GetOppositeWeaponSlot().inventoryItem.itemData.Item != null && GetOppositeWeaponSlot().inventoryItem.itemData.Item.IsWeapon() && GetOppositeWeaponSlot().inventoryItem.itemData.Item.Weapon().isTwoHanded)
            return true;
        return false;
    }

    public override void ClearItem()
    {
        // Hide the item's sprite
        HideSlotImage();

        // Setup the empty slot sprite
        SetEmptySlotSprite();

        if (IsFull() && inventoryItem.itemData.Item.IsWeapon() && inventoryItem.itemData.Item.Weapon().isTwoHanded)
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
        if (equipSlot == global::EquipSlot.RightHeldItem1)
            return myCharacterEquipment.GetEquipmentSlot(global::EquipSlot.LeftHeldItem1);
        else if (equipSlot == global::EquipSlot.LeftHeldItem1)
            return myCharacterEquipment.GetEquipmentSlot(global::EquipSlot.RightHeldItem1);
        else if (equipSlot == global::EquipSlot.RightHeldItem2)
            return myCharacterEquipment.GetEquipmentSlot(global::EquipSlot.LeftHeldItem2);
        else if (equipSlot == global::EquipSlot.LeftHeldItem2)
            return myCharacterEquipment.GetEquipmentSlot(global::EquipSlot.RightHeldItem2);

        Debug.LogWarning($"{equipSlot} is not a weapon slot...");
        return null;
    }

    public override void ShowSlotImage()
    {
        if (inventoryItem.itemData == null || inventoryItem.itemData.Item == null)
        {
            Debug.LogWarning("There is no item in this slot...");
            return;
        }

        if (inventoryItem.itemData.Item.inventorySprite == null)
        {
            Debug.LogError($"Sprite for {inventoryItem.itemData.Item.name} is not yet set in the item's ScriptableObject");
            return;
        }

        if (IsHeldItemSlot())
        {
            if (inventoryItem.itemData.Item.IsWeapon() && inventoryItem.itemData.Item.Weapon().isTwoHanded)
            {
                EquipmentSlot oppositeWeaponSlot = GetOppositeWeaponSlot();
                if (equipSlot == global::EquipSlot.LeftHeldItem1 || equipSlot == global::EquipSlot.LeftHeldItem2)
                {
                    inventoryItem.SetupIconSprite(true);
                    oppositeWeaponSlot.inventoryItem.SetupIconSprite(false);
                }
                else
                {
                    inventoryItem.SetupIconSprite(false);
                    oppositeWeaponSlot.inventoryItem.SetupIconSprite(true);
                }
            }
            else
                inventoryItem.SetupIconSprite(true);
        }
        else
            inventoryItem.SetupIconSprite(true);
    }

    public bool IsHeldItemSlot() => equipSlot == global::EquipSlot.LeftHeldItem1 || equipSlot == global::EquipSlot.RightHeldItem1 || equipSlot == global::EquipSlot.LeftHeldItem2 || equipSlot == global::EquipSlot.RightHeldItem2;

    public override void HighlightSlots()
    {
        bool validSlot = false;
        Item draggedItem = InventoryUI.Instance.DraggedItem.itemData.Item;
        InventoryUI.Instance.DraggedItem_OverlappingMultipleItems();

        if (draggedItem.IsEquipment())
        {
            if (draggedItem.Equipment().EquipSlot() == equipSlot)
                validSlot = true;
            else if ((draggedItem.IsWeapon() || draggedItem.IsShield()) && (equipSlot == global::EquipSlot.LeftHeldItem1 || equipSlot == global::EquipSlot.RightHeldItem1 || equipSlot == global::EquipSlot.LeftHeldItem2 || equipSlot == global::EquipSlot.RightHeldItem2))
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
        if (IsFull() && InventoryUI.Instance.DraggedItem.itemData != inventoryItem.itemData)
            SetFullSlotSprite();

        image.color = Color.white;
    }

    public override void SetupEmptySlotSprites()
    {
        SetEmptySlotSprite();
        if (IsHeldItemSlot() && IsFull())
        { 
            if (inventoryItem.itemData != null && inventoryItem.itemData.Item != null && inventoryItem.itemData.Item.IsWeapon() && inventoryItem.itemData.Item.Weapon().isTwoHanded)
                GetOppositeWeaponSlot().SetEmptySlotSprite();
            else
            {
                EquipmentSlot oppositeWeaponSlot = GetOppositeWeaponSlot();
                if (oppositeWeaponSlot.inventoryItem.itemData != null && oppositeWeaponSlot.inventoryItem.itemData.Item != null && oppositeWeaponSlot.inventoryItem.itemData.Item.IsWeapon() && oppositeWeaponSlot.inventoryItem.itemData.Item.Weapon().isTwoHanded)
                    oppositeWeaponSlot.SetEmptySlotSprite();
            }
        }
    }

    public void SetMyCharacterEquipment(CharacterEquipment characterEquipment) => myCharacterEquipment = characterEquipment;

    public override ItemData GetItemData() => inventoryItem.itemData;

    public CharacterEquipment CharacterEquipment => myCharacterEquipment;

    public EquipSlot EquipSlot => equipSlot;

    public override Slot GetParentSlot() => this;
}
