using InventorySystem;
using System.Collections;
using System.Collections.Specialized;
using System.Linq;
using UnityEngine;

namespace UnitSystem.ActionSystem
{
    public class EquipAction : BaseInventoryAction
    {
        OrderedDictionary itemDatasToEquip = new OrderedDictionary();
        ContainerInventoryManager itemsContainerInventoryManager;

        public void QueueAction(ItemData itemDataToEquip, EquipSlot targetEquipSlot, ContainerInventoryManager itemsContainerInventoryManager)
        {
            if (itemDataToEquip.Item is Equipment == false)
            {
                Debug.LogWarning($"{itemDataToEquip.Item.Name} is not a type of Equipment, but you're trying to queue an EquipAction...");
                return;
            }

            this.itemsContainerInventoryManager = itemsContainerInventoryManager;

            if (itemDatasToEquip.Contains(itemDataToEquip))
                itemDatasToEquip.Remove(itemDataToEquip);

            itemDatasToEquip.Add(itemDataToEquip, targetEquipSlot);

            Unit.unitActionHandler.QueueAction(this);
        }

        /// <summary>For when it's too risky to queue an EquipAction, such as when equipping on pickup, there would be a chance that the action gets cancelled and the item disappears after picking it up, but before equipping it.
        /// In such a case, we should queue an InventoryAction instead of type Equip and then call this method.</summary>
        public void TakeActionImmediately(ItemData itemDataToEquip, EquipSlot targetEquipSlot, ContainerInventoryManager itemsContainerInventoryManager)
        {
            if (itemDataToEquip.Item is Equipment == false)
            {
                Debug.LogWarning($"{itemDataToEquip.Item.Name} is not a type of Equipment, but you're trying to queue an EquipAction...");
                return;
            }

            this.itemsContainerInventoryManager = itemsContainerInventoryManager;

            if (itemDatasToEquip.Contains(itemDataToEquip))
                itemDatasToEquip.Remove(itemDataToEquip);

            itemDatasToEquip.Insert(0, itemDataToEquip, targetEquipSlot);

            TakeAction();
        }

        public override void TakeAction()
        {
            DictionaryEntry dictionaryEntry = itemDatasToEquip.Cast<DictionaryEntry>().FirstOrDefault();
            EquipSlot targetEquipSlot = (EquipSlot)dictionaryEntry.Value;
            if (UnitEquipment.IsHeldItemEquipSlot(targetEquipSlot))
            {
                if (Unit.UnitEquipment.currentWeaponSet == WeaponSet.One)
                {
                    if (targetEquipSlot == EquipSlot.LeftHeldItem2)
                        targetEquipSlot = EquipSlot.LeftHeldItem1;
                    else if (targetEquipSlot == EquipSlot.RightHeldItem2)
                        targetEquipSlot = EquipSlot.RightHeldItem1;
                }
                else // Weapon Set 2
                {
                    if (targetEquipSlot == EquipSlot.LeftHeldItem1)
                        targetEquipSlot = EquipSlot.LeftHeldItem2;
                    else if (targetEquipSlot == EquipSlot.RightHeldItem1)
                        targetEquipSlot = EquipSlot.RightHeldItem2;
                }
            }

            Unit.UnitEquipment.TryAddItemAt(targetEquipSlot, (ItemData)dictionaryEntry.Key);
            CompleteAction();
        }

        public override void CompleteAction()
        {
            base.CompleteAction();

            if (itemDatasToEquip.Count > 0)
                itemDatasToEquip.Remove((ItemData)itemDatasToEquip.Cast<DictionaryEntry>().FirstOrDefault().Key);
        }

        public static int GetItemsEquipActionPointCost(ItemData itemData, int stackSize, ContainerInventoryManager itemsContainerInventoryManager)
        {
            float costMultiplier = 1f;
            if (itemData.Item is Equipment)
            {
                if (UnitEquipment.IsHeldItemEquipSlot(itemData.Item.Equipment.EquipSlot))
                {
                    if (itemData.Item is Weapon)
                        costMultiplier = 0.4f;
                    else
                        costMultiplier = 0.6f;
                }
                else
                {
                    switch (itemData.Item.Equipment.EquipSlot)
                    {
                        case EquipSlot.Helm:
                            costMultiplier = 2f;
                            break;
                        case EquipSlot.BodyArmor:
                            costMultiplier = 1f; // Body armor is generally quite heavy, so avoid making it cost too much
                            break;
                        case EquipSlot.Shirt:
                            costMultiplier = 1.8f;
                            break;
                        case EquipSlot.Gloves:
                            costMultiplier = 2.5f;
                            break;
                        case EquipSlot.Boots:
                            costMultiplier = 3.5f;
                            break;
                        case EquipSlot.Back:
                            costMultiplier = 1f;
                            break;
                        case EquipSlot.Quiver:
                            costMultiplier = 1.8f;
                            break;
                    }
                }
            }
            
            // Debug.Log($"Equip Cost of {itemData.Item.Name}: {Mathf.RoundToInt(GetItemsActionPointCost(itemData, stackSize, itemsContainerInventoryManager) * (float)costMultiplier)}");
            return Mathf.RoundToInt(GetItemsActionPointCost(itemData, stackSize, itemsContainerInventoryManager) * (float)costMultiplier);
        }

        public override int ActionPointsCost()
        {
            ItemData itemDataToEquip = (ItemData)itemDatasToEquip.Cast<DictionaryEntry>().LastOrDefault().Key;
            int cost = GetItemsEquipActionPointCost(itemDataToEquip, itemDataToEquip.CurrentStackSize, itemsContainerInventoryManager);

            itemsContainerInventoryManager = null;
            return cost;
        }

        public override bool IsValidAction() => Unit != null && Unit.UnitEquipment != null;

        public override bool IsInterruptable() => false;

        public override bool CanBeClearedFromActionQueue() => false;

        public override string TooltipDescription() => "";
    }
}
