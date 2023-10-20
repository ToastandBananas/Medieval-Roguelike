using InventorySystem;
using System.Collections;
using System.Collections.Specialized;
using System.Linq;
using UnityEngine;

namespace ActionSystem
{
    public class EquipAction : BaseInventoryAction
    {
        OrderedDictionary itemDatasToEquip = new OrderedDictionary();

        public void QueueAction(ItemData itemDataToEquip, EquipSlot targetEquipSlot)
        {
            if (itemDatasToEquip.Contains(itemDataToEquip))
                itemDatasToEquip.Remove(itemDataToEquip);

            itemDatasToEquip.Add(itemDataToEquip, targetEquipSlot);

            unit.unitActionHandler.QueueAction(this);
        }

        public override void TakeAction()
        {
            DictionaryEntry dictionaryEntry = itemDatasToEquip.Cast<DictionaryEntry>().FirstOrDefault();
            unit.UnitEquipment.TryAddItemAt((EquipSlot)dictionaryEntry.Value, (ItemData)dictionaryEntry.Key);
            CompleteAction();
        }

        public override void CompleteAction()
        {
            base.CompleteAction();

            itemDatasToEquip.Remove((ItemData)itemDatasToEquip.Cast<DictionaryEntry>().FirstOrDefault().Key);
        }

        public static int GetItemsEquipActionPointCost(ItemData itemData, int stackSize)
        {
            float costMultiplier = 1f;
            if (itemData.Item is Equipment)
            {
                if (UnitEquipment.IsHeldItemEquipSlot(itemData.Item.Equipment.EquipSlot))
                    costMultiplier = 0.5f;
                else
                {
                    switch (itemData.Item.Equipment.EquipSlot)
                    {
                        case EquipSlot.Helm:
                            costMultiplier = 1.2f;
                            break;
                        case EquipSlot.BodyArmor:
                            costMultiplier = 5f;
                            break;
                        case EquipSlot.Shirt:
                            costMultiplier = 1.8f;
                            break;
                        case EquipSlot.Gloves:
                            costMultiplier = 2f;
                            break;
                        case EquipSlot.Boots:
                            costMultiplier = 3.5f;
                            break;
                        case EquipSlot.Back:
                            costMultiplier = 4f;
                            break;
                        case EquipSlot.Quiver:
                            costMultiplier = 2f;
                            break;
                    }
                }
            }

            // Debug.Log($"Equip Cost of {itemData.Item.Name}: {Mathf.RoundToInt(GetItemsActionPointCost(itemData, stackSize) * (float)costMultiplier)}");
            return Mathf.RoundToInt(GetItemsActionPointCost(itemData, stackSize) * (float)costMultiplier);
        }

        public override int GetActionPointsCost()
        {
            DictionaryEntry dictionaryEntry = itemDatasToEquip.Cast<DictionaryEntry>().LastOrDefault();
            EquipSlot targetEquipSlot = (EquipSlot)dictionaryEntry.Value;
            ItemData itemDataToEquip = (ItemData)dictionaryEntry.Key;
            int cost = GetItemsEquipActionPointCost(itemDataToEquip, itemDataToEquip.CurrentStackSize);

            // Account for having to unequip any items
            if (unit.UnitEquipment.EquipSlotHasItem(targetEquipSlot))
                cost += UnequipAction.GetItemsUnequipActionPointCost(unit.UnitEquipment.EquippedItemDatas[(int)targetEquipSlot], unit.UnitEquipment.EquippedItemDatas[(int)targetEquipSlot].CurrentStackSize);

            if (UnitEquipment.IsHeldItemEquipSlot(targetEquipSlot))
            {
                ItemData oppositeItemData = unit.UnitEquipment.EquippedItemDatas[(int)unit.UnitEquipment.GetOppositeWeaponEquipSlot(targetEquipSlot)];
                if (oppositeItemData != null && oppositeItemData.Item != null)
                {
                    if ((itemDataToEquip.Item is Weapon && itemDataToEquip.Item.Weapon.IsTwoHanded) || (oppositeItemData.Item is Weapon && oppositeItemData.Item.Weapon.IsTwoHanded))
                        cost += UnequipAction.GetItemsUnequipActionPointCost(oppositeItemData, oppositeItemData.CurrentStackSize);
                }
            }

            return cost;
        }

        public override bool IsValidAction() => unit.UnitEquipment != null;
    }
}
