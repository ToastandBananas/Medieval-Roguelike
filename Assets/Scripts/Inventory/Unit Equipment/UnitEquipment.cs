using System.Collections.Generic;
using UnitSystem;
using UnitSystem.ActionSystem;
using UnitSystem.ActionSystem.Actions;
using UnitSystem.ActionSystem.UI;
using UnityEngine;

namespace InventorySystem
{
    public abstract class UnitEquipment : MonoBehaviour
    {
        [SerializeField] protected Unit myUnit;

        [SerializeField] protected ItemData[] equippedItemDatas = new ItemData[1];

        public List<EquipmentSlot> Slots { get; protected set; }

        public bool SlotVisualsCreated { get; protected set; }

        public static readonly float equippedWeightFactor = 0.5f; // The weight reduction of an item when it's equipped or inside a bag that's equipped

        public ItemData GetEquippedItemData(EquipSlot equipSlot) => equippedItemDatas[(int)equipSlot];

        public bool CanEquipItem(ItemData newItemData) => CanEquipItemAt(newItemData, GetTargetEquipSlot(newItemData));

        public abstract bool CanEquipItemAt(ItemData newItemData, EquipSlot targetEquipSlot);

        public virtual bool CapableOfEquippingHeldItem(ItemData itemData, EquipSlot targetHeldItemEquipSlot, bool checkOppositeHeldItemSlot) => false;

        public bool TryEquipItem(ItemData newItemData) => TryAddItemAt(GetTargetEquipSlot(newItemData), newItemData);

        public virtual bool TryAddItemAt(EquipSlot targetEquipSlot, ItemData newItemData)
        {
            Equip(newItemData, targetEquipSlot);
            return true;
        }

        protected virtual void Equip(ItemData newItemData, EquipSlot targetEquipSlot)
        {
            if (IsHeldItemEquipSlot(targetEquipSlot))
            {
                EquipSlot oppositeWeaponSlot = HumanoidEquipment.GetOppositeHeldItemEquipSlot(targetEquipSlot);

                // If we're equipping a two-handed weapon and there's already a weapon/shield in the opposite weapon slot or if the opposite weapon slot has a two-handed weapon in it
                if (EquipSlotHasItem(oppositeWeaponSlot) && ((newItemData.Item is Item_Weapon && newItemData.Item.Weapon.IsTwoHanded) || (equippedItemDatas[(int)oppositeWeaponSlot].Item is Item_Weapon && equippedItemDatas[(int)oppositeWeaponSlot].Item.Weapon.IsTwoHanded)))
                {
                    myUnit.UnitActionHandler.GetAction<Action_Inventory>().QueueAction(equippedItemDatas[(int)oppositeWeaponSlot], equippedItemDatas[(int)oppositeWeaponSlot].CurrentStackSize, null, InventoryActionType.Unequip);
                    UnequipItem(oppositeWeaponSlot);
                }
            }

            // Unequip any item already in the target equip slot
            UnequipFromTargetEquipSlot(targetEquipSlot);
            
            // If the new item is coming from an NPC's Container Equipment Slot or a Loose Container Item, transfer the inventories
            if (IsWearableContainerEquipSlot(targetEquipSlot))
                SwapContainerInventories(targetEquipSlot, newItemData);

            // Clear out the item from it's original slot
            RemoveItemFromOrigin(newItemData);
            newItemData.SetInventorySlotCoordinate(null);

            // Assign the data
            equippedItemDatas[(int)targetEquipSlot] = newItemData;

            if (targetEquipSlot == EquipSlot.RightHeldItem1 && newItemData.Item is Item_Weapon && newItemData.Item.Weapon.IsTwoHanded)
                equippedItemDatas[(int)EquipSlot.RightHeldItem1] = null;
            else if (targetEquipSlot == EquipSlot.RightHeldItem2 && newItemData.Item is Item_Weapon && newItemData.Item.Weapon.IsTwoHanded)
                equippedItemDatas[(int)EquipSlot.RightHeldItem2] = null;

            if (IsWearableContainerEquipSlot(targetEquipSlot))
                InitializeInventories(targetEquipSlot, newItemData);

            // Setup the target slot's item data/sprites and mesh if necessary
            SetupNewItemIcon(GetEquipmentSlot(targetEquipSlot), newItemData);
            SetupEquipmentMesh(targetEquipSlot, newItemData);

            // Set the size of the opportunity attack trigger
            if (IsHeldItemEquipSlot(targetEquipSlot))
                myUnit.OpportunityAttackTrigger.UpdateColliderRadius();

            if (myUnit != null)
                myUnit.Stats.UpdateCarryWeight();
        }

        protected abstract void UnequipFromTargetEquipSlot(EquipSlot targetEquipSlot);

        protected abstract void SwapContainerInventories(EquipSlot targetEquipSlot, ItemData newItemData);

        protected virtual EquipSlot GetTargetEquipSlot(ItemData newItemData) => newItemData.Item.Equipment.EquipSlot;

        public void RemoveEquipment(ItemData itemData)
        {
            int targetEquipSlotIndex = -1;
            for (int i = 0; i < equippedItemDatas.Length; i++)
            {
                if (equippedItemDatas[i] == itemData)
                {
                    targetEquipSlotIndex = i;
                    break;
                }
            }

            if (targetEquipSlotIndex != -1)
            {
                if (SlotVisualsCreated)
                    GetEquipmentSlotFromIndex(targetEquipSlotIndex).ClearItem();

                RemoveActions(itemData.Item.Equipment, (EquipSlot)targetEquipSlotIndex);
                RemoveEquipmentMesh((EquipSlot)targetEquipSlotIndex);
                equippedItemDatas[targetEquipSlotIndex] = null;

                myUnit.Stats.UpdateCarryWeight();

                // Set the size of the opportunity attack trigger
                if (IsHeldItemEquipSlot((EquipSlot)targetEquipSlotIndex))
                    myUnit.OpportunityAttackTrigger.UpdateColliderRadius();

                ActionSystemUI.UpdateActionVisuals();
            }
        }

        protected abstract void InitializeInventories(EquipSlot targetEquipSlot, ItemData newItemData);

        protected abstract void RemoveItemFromOrigin(ItemData itemDataToRemove);

        public abstract void UnequipItem(EquipSlot equipSlot);

        public abstract void CreateSlotVisuals();

        protected abstract void SetupItems();

        protected void AddActions(Item_Equipment equipment)
        {
            if (equipment.ActionTypes.Length == 0)
                return;

            for (int i = 0; i < equipment.ActionTypes.Length; i++)
            {
                if (myUnit.UnitActionHandler.AvailableActionTypes.Contains(equipment.ActionTypes[i]))
                    continue;

                myUnit.UnitActionHandler.AvailableActionTypes.Add(equipment.ActionTypes[i]);
                equipment.ActionTypes[i].GetAction(myUnit);

                if (myUnit.IsPlayer)
                    ActionSystemUI.AddButton(equipment.ActionTypes[i]);
            }

            if (myUnit.IsPlayer)
                ActionSystemUI.UpdateActionVisuals();
        }

        public virtual void RemoveActions(Item_Equipment equipment, EquipSlot equipSlot)
        {
            for (int i = 0; i < equipment.ActionTypes.Length; i++)
            {
                if (!myUnit.UnitActionHandler.AvailableActionTypes.Contains(equipment.ActionTypes[i]))
                    continue;

                Pool_Actions.ReturnToPool(equipment.ActionTypes[i].GetAction(myUnit));
                myUnit.UnitActionHandler.AvailableActionTypes.Remove(equipment.ActionTypes[i]);
            }
        }

        public virtual bool EquipSlotIsFull(EquipSlot equipSlot) => EquipSlotHasItem(equipSlot);

        public bool EquipSlotHasItem(int equipSlotIndex) => equippedItemDatas[equipSlotIndex] != null && equippedItemDatas[equipSlotIndex].Item != null;

        public bool EquipSlotHasItem(EquipSlot equipSlot) => EquipSlotHasItem((int)equipSlot);

        EquipmentSlot GetEquipmentSlotFromIndex(int index)
        {
            for (int i = 0; i < Slots.Count; i++)
            {
                if (index == (int)Slots[i].EquipSlot)
                    return Slots[i];
            }
            return null;
        }

        /// <summary>Setup the target slot's item data and sprites.</summary>
        protected virtual void SetupNewItemIcon(EquipmentSlot targetSlot, ItemData newItemData)
        {
            if (!SlotVisualsCreated)
                return;

            newItemData.SetInventorySlotCoordinate(null);
            targetSlot.InventoryItem.SetItemData(newItemData);
            targetSlot.SetFullSlotSprite();
            targetSlot.ShowSlotImage();
            targetSlot.InventoryItem.UpdateStackSizeVisuals();
        }

        protected abstract void SetupEquipmentMesh(EquipSlot equipSlot, ItemData itemData);

        public abstract void RemoveEquipmentMesh(EquipSlot equipSlot);

        public EquipSlot GetEquipSlotFromItemData(ItemData itemData)
        {
            for (int i = 0; i < equippedItemDatas.Length; i++)
            {
                if (itemData == equippedItemDatas[i])
                    return (EquipSlot)i;
            }
            return EquipSlot.LeftHeldItem1;
        }

        public static bool IsHeldItemEquipSlot(EquipSlot equipSlot) => equipSlot == EquipSlot.LeftHeldItem1 || equipSlot == EquipSlot.RightHeldItem1 || equipSlot == EquipSlot.LeftHeldItem2 || equipSlot == EquipSlot.RightHeldItem2;

        public static bool IsWearableContainerEquipSlot(EquipSlot equipSlot) => equipSlot == EquipSlot.Back || equipSlot == EquipSlot.Belt || equipSlot == EquipSlot.Quiver;

        //public static bool IsRingEquipSlot(EquipSlot equipSlot) => equipSlot == EquipSlot.Ring1 || equipSlot == EquipSlot.Ring2;

        public virtual bool IsDualWielding => false;
        public virtual bool MeleeWeaponEquipped => false;
        public virtual bool RangedWeaponEquipped => false;
        public virtual bool ShieldEquipped => false;
        public virtual bool IsUnarmed => true;

        public float GetTotalEquipmentWeight()
        {
            float weight = 0f;
            for (int i = 0; i < equippedItemDatas.Length; i++)
            {
                if (EquipSlotHasItem(i))
                    weight += equippedItemDatas[i].Weight();
            }

            return weight * equippedWeightFactor;
        }

        public EquipmentSlot GetEquipmentSlot(EquipSlot equipSlot)
        {
            for (int i = 0; i < Slots.Count; i++)
            {
                if (Slots[i].EquipSlot == equipSlot)
                    return Slots[i];
            }
            return null;
        }

        public bool ItemDataEquipped(ItemData itemData)
        {
            if (itemData == null || itemData.Item == null)
                return false;

            for (int i = 0; i < equippedItemDatas.Length; i++)
            {
                if (equippedItemDatas[i] == itemData)
                    return true;
            }
            return false;
        }

        public ItemData[] EquippedItemDatas => equippedItemDatas;

        public Unit MyUnit => myUnit;

        public void OnCloseNPCInventory()
        {
            SlotVisualsCreated = false;

            if (Slots.Count > 0)
            {
                // Clear out any slots already in the list, so we can start from scratch
                for (int i = 0; i < Slots.Count; i++)
                {
                    Slots[i].RemoveSlotHighlights();
                    Slots[i].ClearItem();
                    Slots[i].SetMyCharacterEquipment(null);
                    Slots[i].InventoryItem.SetMyUnitEquipment(null);
                }
            }
        }

        public UnitEquipment_Humanoid HumanoidEquipment => this as UnitEquipment_Humanoid;
    }
}
