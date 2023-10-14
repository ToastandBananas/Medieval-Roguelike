using InventorySystem;
using System.Collections;
using System.Collections.Specialized;
using System.Linq;

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

        public override int GetActionPointsCost()
        {
            DictionaryEntry dictionaryEntry = itemDatasToEquip.Cast<DictionaryEntry>().LastOrDefault();
            EquipSlot targetEquipSlot = (EquipSlot)dictionaryEntry.Value;
            ItemData itemDataToEquip = (ItemData)dictionaryEntry.Key;
            int cost = CalculateItemsActionPointCost(itemDataToEquip);

            // Account for having to unequip any items
            if (unit.UnitEquipment.EquipSlotHasItem(targetEquipSlot))
                cost += CalculateItemsActionPointCost(unit.UnitEquipment.EquippedItemDatas[(int)targetEquipSlot]);

            if (unit.UnitEquipment.IsHeldItemEquipSlot(targetEquipSlot))
            {
                ItemData oppositeItemData = unit.UnitEquipment.EquippedItemDatas[(int)unit.UnitEquipment.GetOppositeWeaponEquipSlot(targetEquipSlot)];
                if (oppositeItemData != null && oppositeItemData.Item != null)
                {
                    if ((itemDataToEquip.Item is Weapon && itemDataToEquip.Item.Weapon.IsTwoHanded) || (oppositeItemData.Item is Weapon && oppositeItemData.Item.Weapon.IsTwoHanded))
                        cost += CalculateItemsActionPointCost(oppositeItemData);
                }
            }

            return cost;
        }

        public override bool IsValidAction() => unit.UnitEquipment != null;
    }
}
