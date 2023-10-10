using UnityEngine;
using UnityEngine.UI;

namespace InventorySystem
{
    public class EquipmentSlot : Slot
    {
        [Header("Equipment")]
        [SerializeField] EquipSlot equipSlot = EquipSlot.RightHeldItem1;

        [Header("Placeholder Icon")]
        [SerializeField] Image placeholderImage;

        UnitEquipment myUnitEquipment;

        public override bool IsFull()
        {
            if (inventoryItem.itemData != null && inventoryItem.itemData.Item != null)
                return true;

            if (equipSlot == EquipSlot.RightHeldItem1 && GetOppositeWeaponSlot().inventoryItem.itemData != null && GetOppositeWeaponSlot().inventoryItem.itemData.Item != null && GetOppositeWeaponSlot().inventoryItem.itemData.Item is Weapon && GetOppositeWeaponSlot().inventoryItem.itemData.Item.Weapon.IsTwoHanded)
                return true;
            return false;
        }

        public override void ClearSlotVisuals()
        {
            // Hide the item's sprite
            HideItemIcon();

            // Setup the empty slot sprite
            SetEmptySlotSprite();

            if (IsFull())
            {
                if (inventoryItem.itemData.Item is Weapon && inventoryItem.itemData.Item.Weapon.IsTwoHanded)
                {
                    EquipmentSlot oppositeWeaponSlot = GetOppositeWeaponSlot();
                    oppositeWeaponSlot.HideItemIcon();
                    oppositeWeaponSlot.SetEmptySlotSprite();
                }
                else if (inventoryItem.itemData.Item is Quiver)
                    inventoryItem.QuiverInventoryItem.HideQuiverSprites();
            }

            // Clear the stack size text
            inventoryItem.ClearStackSizeText();

            // Clear out the slot's item data
            inventoryItem.SetItemData(null);
        }

        public override void ClearItem() => ClearSlotVisuals();

        public EquipmentSlot GetOppositeWeaponSlot()
        {
            if (equipSlot == EquipSlot.RightHeldItem1)
                return myUnitEquipment.GetEquipmentSlot(EquipSlot.LeftHeldItem1);
            else if (equipSlot == EquipSlot.LeftHeldItem1)
                return myUnitEquipment.GetEquipmentSlot(EquipSlot.RightHeldItem1);
            else if (equipSlot == EquipSlot.RightHeldItem2)
                return myUnitEquipment.GetEquipmentSlot(EquipSlot.LeftHeldItem2);
            else if (equipSlot == EquipSlot.LeftHeldItem2)
                return myUnitEquipment.GetEquipmentSlot(EquipSlot.RightHeldItem2);

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

            if (inventoryItem.itemData.Item.InventorySprite(inventoryItem.itemData) == null)
            {
                Debug.LogError($"Sprite for {inventoryItem.itemData.Item.name} is not yet set in the item's ScriptableObject");
                return;
            }

            if (IsHeldItemSlot())
            {
                if (inventoryItem.itemData.Item is Weapon && inventoryItem.itemData.Item.Weapon.IsTwoHanded)
                {
                    EquipmentSlot oppositeWeaponSlot = GetOppositeWeaponSlot();
                    if (equipSlot == EquipSlot.LeftHeldItem1 || equipSlot == EquipSlot.LeftHeldItem2)
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

        public bool IsHeldItemSlot() => equipSlot == EquipSlot.LeftHeldItem1 || equipSlot == EquipSlot.RightHeldItem1 || equipSlot == EquipSlot.LeftHeldItem2 || equipSlot == EquipSlot.RightHeldItem2;

        public override void HighlightSlots()
        {
            bool validSlot = false;
            Item draggedItem = InventoryUI.DraggedItem.itemData.Item;

            if (myUnitEquipment.MyUnit.health.IsDead() && (InventoryUI.parentSlotDraggedFrom == null || InventoryUI.parentSlotDraggedFrom != this))
                validSlot = false;
            else if (draggedItem is Equipment)
            {
                if (draggedItem.Equipment.EquipSlot == equipSlot)
                    validSlot = true;
                else if ((draggedItem is Weapon || draggedItem is Shield) && IsHeldItemSlot())
                    validSlot = true;
            }

            InventoryUI.SetValidDragPosition(validSlot);

            SetEmptySlotSprite();

            if (validSlot)
                image.color = Color.green;
            else
                image.color = Color.red;
        }

        public override void RemoveSlotHighlights()
        {
            if (IsFull() && InventoryUI.DraggedItem.itemData != inventoryItem.itemData)
                SetFullSlotSprite();

            image.color = Color.white;
        }

        public override void SetupEmptySlotSprites()
        {
            SetEmptySlotSprite();
            if (IsHeldItemSlot() && IsFull())
            {
                if (inventoryItem.itemData != null && inventoryItem.itemData.Item != null && inventoryItem.itemData.Item is Weapon && inventoryItem.itemData.Item.Weapon.IsTwoHanded)
                    GetOppositeWeaponSlot().SetEmptySlotSprite();
                else
                {
                    EquipmentSlot oppositeWeaponSlot = GetOppositeWeaponSlot();
                    if (oppositeWeaponSlot.inventoryItem.itemData != null && oppositeWeaponSlot.inventoryItem.itemData.Item != null && oppositeWeaponSlot.inventoryItem.itemData.Item is Weapon && oppositeWeaponSlot.inventoryItem.itemData.Item.Weapon.IsTwoHanded)
                        oppositeWeaponSlot.SetEmptySlotSprite();
                }
            }
        }

        public void SetMyCharacterEquipment(UnitEquipment unitEquipment) => myUnitEquipment = unitEquipment;

        public override ItemData GetItemData() => inventoryItem.itemData;

        public UnitEquipment UnitEquipment => myUnitEquipment;

        public Image PlaceholderImage => placeholderImage;

        public EquipSlot EquipSlot => equipSlot;

        public override Slot ParentSlot() => this;
    }
}
