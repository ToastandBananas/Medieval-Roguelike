using InventorySystem;
using System.Collections.Generic;
using UnityEngine;

namespace ActionSystem
{
    public class ConsumeAction : BaseInventoryAction
    {
        List<ItemData> itemsToConsume = new List<ItemData>();

        readonly int consumeAPCostPerPart = 360; // About 6 seconds per part (depends on the Unit's max AP though of course)

        public void QueueAction(ItemData itemData)
        {
            if (itemData.Item is Consumable == false)
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
            itemToConsume.Use(1);

            if (itemToConsume.RemainingUses <= 0)
            {
                if (itemToConsume.MyInventory != null)
                    itemToConsume.MyInventory.RemoveItem(itemToConsume);
            }

            itemsToConsume.Remove(itemToConsume);

            Debug.Log(unit.name + " consumed 1 part of " + itemToConsume.Item.Name); 
            CompleteAction();
        }

        public override int GetActionPointsCost()
        {
            return Mathf.RoundToInt(consumeAPCostPerPart * GetItemSizeMultiplier(itemsToConsume[0].Item.ItemSize) * itemsToConsume[0].Item.Consumable.ConsumeAPCostMultiplier);
        }

        public override bool IsInterruptable() => true;

        public override bool CanBeClearedFromActionQueue() => true;

        public override bool IsValidAction() => true;
    }
}
