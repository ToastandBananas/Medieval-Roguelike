using InventorySystem;
using UnitSystem.ActionSystem.UI;
using System.Collections.Generic;
using UnityEngine;

namespace UnitSystem.ActionSystem.Actions
{
    public class Action_Consume : Action_BaseInventory
    {
        readonly List<ItemData> itemsToConsume = new();

        readonly int consumeAPCostPerPart = 360; // About 6 seconds per part (depends on the Unit's max AP though of course)

        public void QueueAction(ItemData itemData)
        {
            if (itemData.Item is Item_Consumable == false)
            {
                Debug.LogWarning($"{itemData.Item.Name} is not a consumable, but you're trying to queue a ConsumeAction...");
                return;
            }

            itemsToConsume.Add(itemData);
            QueueAction();
        }

        public override void TakeAction()
        {
            ItemData itemToConsume = itemsToConsume[0];
            ItemChangeThreshold currentItemChangeThreshold = ItemChangeThreshold.GetCurrentItemChangeThreshold(itemToConsume, itemToConsume.Item.Consumable.ItemChangeThresholds);
            itemToConsume.Use(1);

            if (itemToConsume.RemainingUses <= 0)
            {
                if (itemToConsume.MyInventory != null)
                    itemToConsume.MyInventory.RemoveItem(itemToConsume, true);
            }
            else
            {
                if (ItemChangeThreshold.ThresholdReached(itemToConsume, true, currentItemChangeThreshold, itemToConsume.Item.Consumable.ItemChangeThresholds, out ItemChangeThreshold newThreshold))
                {
                    if (itemToConsume.MyInventory != null)
                    {
                        SlotCoordinate itemsSlotCoordinate = itemToConsume.MyInventory.GetSlotCoordinateFromItemData(itemToConsume);
                        if (newThreshold.NewItem == currentItemChangeThreshold.NewItem)
                        {
                            if (itemsSlotCoordinate.myInventory.SlotVisualsCreated)
                                itemsSlotCoordinate.myInventory.GetSlotFromCoordinate(itemsSlotCoordinate).InventoryItem.SetupIconSprite(true);
                        }
                        else
                        {
                            Item oldItem = itemToConsume.Item;
                            itemToConsume.MyInventory.RemoveItem(itemToConsume, false);
                            itemToConsume.SetItem(newThreshold.NewItem);
                            itemToConsume.MyInventory.TryAddItemAt(itemToConsume.MyInventory.GetSlotCoordinate(itemsSlotCoordinate.coordinate.x + itemToConsume.Item.Width - oldItem.Width, itemsSlotCoordinate.coordinate.y + itemToConsume.Item.Height - oldItem.Height), itemToConsume, Unit);
                        }

                        if (ActionSystemUI.ItemActionBarAlreadyHasItem(itemToConsume))
                            ActionSystemUI.GetItemActionBarSlot(itemToConsume).SetupIconSprite();
                    }
                }
            }

            itemsToConsume.Remove(itemToConsume);

            // Debug.Log(unit.name + " consumed 1 part of " + itemToConsume.Item.Name); 
            CompleteAction();
        }

        public override int ActionPointsCost()
        {
            return Mathf.RoundToInt(consumeAPCostPerPart * itemsToConsume[0].Item.Consumable.ConsumeAPCostMultiplier);
        }

        public override bool IsInterruptable() => true;

        public override bool CanBeClearedFromActionQueue() => true;

        public override bool IsValidAction() => true;

        public override string TooltipDescription() => "";
    }
}
