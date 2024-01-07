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
            if (inventoryItem.ItemData != null && inventoryItem.ItemData.Item != null)
                return true;

            if (equipSlot == EquipSlot.RightHeldItem1 || equipSlot == EquipSlot.RightHeldItem2)
            {
                EquipmentSlot oppositeWeaponSlot = GetOppositeWeaponSlot();
                if (oppositeWeaponSlot.inventoryItem.ItemData != null && oppositeWeaponSlot.inventoryItem.ItemData.Item != null && oppositeWeaponSlot.inventoryItem.ItemData.Item is Item_Weapon && oppositeWeaponSlot.inventoryItem.ItemData.Item.Weapon.IsTwoHanded)
                    return true;
            }

            return false;
        }

        public override void ClearSlotVisuals()
        {
            // Hide the item's sprite
            HideItemIcon();

            // Setup the empty slot sprite
            SetEmptySlotSprite();
            SetupImageColor();

            if (IsFull())
            {
                if (inventoryItem.ItemData.Item is Item_Weapon && inventoryItem.ItemData.Item.Weapon.IsTwoHanded)
                {
                    EquipmentSlot oppositeWeaponSlot = GetOppositeWeaponSlot();
                    oppositeWeaponSlot.HideItemIcon();
                    oppositeWeaponSlot.SetEmptySlotSprite();
                }
                else if (inventoryItem.ItemData.Item is Item_Quiver)
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
            if (inventoryItem.ItemData == null || inventoryItem.ItemData.Item == null)
            {
                Debug.LogWarning("There is no item in this slot...");
                return;
            }

            if (inventoryItem.ItemData.Item.InventorySprite(inventoryItem.ItemData) == null)
            {
                Debug.LogError($"Sprite for {inventoryItem.ItemData.Item.name} is not yet set in the item's ScriptableObject");
                return;
            }

            if (IsHeldItemSlot)
            {
                if (inventoryItem.ItemData.Item is Item_Weapon && inventoryItem.ItemData.Item.Weapon.IsTwoHanded)
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

                    oppositeWeaponSlot.EnableSlotImage();
                }
                else
                    inventoryItem.SetupIconSprite(true);
            }
            else
                inventoryItem.SetupIconSprite(true);

            EnableSlotImage();
        }

        public bool IsHeldItemSlot => equipSlot == EquipSlot.LeftHeldItem1 || equipSlot == EquipSlot.RightHeldItem1 || equipSlot == EquipSlot.LeftHeldItem2 || equipSlot == EquipSlot.RightHeldItem2;

        //public bool IsRingSlot => equipSlot == EquipSlot.Ring1 || equipSlot == EquipSlot.Ring2;

        public override void EnableSlotImage()
        {
            base.EnableSlotImage();
            SetupImageColor();
        }

        public override void HighlightSlots()
        {
            bool validSlot = false;
            Item draggedItem = InventoryUI.DraggedItem.ItemData.Item;

            if ((InventoryUI.DraggedItem.ItemData.IsBroken && (InventoryUI.ParentSlotDraggedFrom == null || InventoryUI.ParentSlotDraggedFrom != this)) 
                || (IsHeldItemSlot && !myUnitEquipment.CapableOfEquippingHeldItem(InventoryUI.DraggedItem.ItemData, equipSlot, false)) 
                || (myUnitEquipment.MyUnit.HealthSystem.IsDead && (InventoryUI.ParentSlotDraggedFrom == null || InventoryUI.ParentSlotDraggedFrom != this)))
                validSlot = false;
            else if (equipSlot == EquipSlot.Back && myUnitEquipment.HumanoidEquipment.BackpackEquipped)
                validSlot = true;
            else if (equipSlot == EquipSlot.Belt && myUnitEquipment.HumanoidEquipment.BeltBagEquipped)
                validSlot = true;
            else if (draggedItem is Item_Equipment)
            {
                if (draggedItem.Equipment.EquipSlot == equipSlot)
                    validSlot = true;
                else if ((draggedItem is Item_Weapon || draggedItem is Item_Shield) && IsHeldItemSlot)
                    validSlot = true;
                //else if (draggedItem is Ring && IsRingSlot())
                    //validSlot = true;
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
            if (IsFull() && InventoryUI.DraggedItem.ItemData != inventoryItem.ItemData)
                SetFullSlotSprite();

            SetupImageColor();
        }

        public void SetupImageColor()
        {
            if (((equipSlot == EquipSlot.LeftHeldItem1 || equipSlot == EquipSlot.LeftHeldItem2) && !myUnitEquipment.MyUnit.HealthSystem.ArmCanHoldItem(UnitSystem.BodyPartSide.Left))
                || ((equipSlot == EquipSlot.RightHeldItem1 || equipSlot == EquipSlot.RightHeldItem2) && !myUnitEquipment.MyUnit.HealthSystem.ArmCanHoldItem(UnitSystem.BodyPartSide.Right)))
                image.color = Color.red;
            else
                image.color = Color.white;
        }

        public override void SetupEmptySlotSprites()
        {
            SetEmptySlotSprite();
            if (IsHeldItemSlot && IsFull())
            {
                if (inventoryItem.ItemData != null && inventoryItem.ItemData.Item != null && inventoryItem.ItemData.Item is Item_Weapon && inventoryItem.ItemData.Item.Weapon.IsTwoHanded)
                    GetOppositeWeaponSlot().SetEmptySlotSprite();
                else
                {
                    EquipmentSlot oppositeWeaponSlot = GetOppositeWeaponSlot();
                    if (oppositeWeaponSlot.inventoryItem.ItemData != null && oppositeWeaponSlot.inventoryItem.ItemData.Item != null && oppositeWeaponSlot.inventoryItem.ItemData.Item is Item_Weapon && oppositeWeaponSlot.inventoryItem.ItemData.Item.Weapon.IsTwoHanded)
                        oppositeWeaponSlot.SetEmptySlotSprite();
                }
            }
        }

        public void SetMyCharacterEquipment(UnitEquipment unitEquipment) => myUnitEquipment = unitEquipment;

        public ContainerEquipmentSlot ContainerEquipmentSlot => this as ContainerEquipmentSlot;

        public override ItemData GetItemData() => inventoryItem.ItemData;

        public UnitEquipment UnitEquipment => myUnitEquipment;

        public Image PlaceholderImage => placeholderImage;

        public EquipSlot EquipSlot => equipSlot;

        public override Slot ParentSlot() => this;
    }
}
