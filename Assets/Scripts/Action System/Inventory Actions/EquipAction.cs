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
                            costMultiplier = 1.4f;
                            break;
                        case EquipSlot.Quiver:
                            costMultiplier = 1.2f;
                            break;
                    }
                }
            }
            
            // Debug.Log($"Equip Cost of {itemData.Item.Name}: {Mathf.RoundToInt(GetItemsActionPointCost(itemData, stackSize, itemsContainerInventoryManager) * (float)costMultiplier)}");
            return Mathf.RoundToInt(GetItemsActionPointCost(itemData, stackSize, itemsContainerInventoryManager) * (float)costMultiplier);
        }

        public override int GetActionPointsCost()
        {
            ItemData itemDataToEquip = (ItemData)itemDatasToEquip.Cast<DictionaryEntry>().LastOrDefault().Key;
            int cost = GetItemsEquipActionPointCost(itemDataToEquip, itemDataToEquip.CurrentStackSize, itemsContainerInventoryManager);

            itemsContainerInventoryManager = null;
            return cost;
        }

        public override bool IsValidAction() => unit.UnitEquipment != null;

        public override bool IsInterruptable() => false;

        public override bool CanBeClearedFromActionQueue() => false;

        public override string TooltipDescription() => "";
    }
}
