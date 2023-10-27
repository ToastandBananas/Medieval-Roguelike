using InventorySystem;
using UnityEngine;

namespace ActionSystem
{
    public class ItemActionBarSlot : ActionBarSlot
    {
        public ItemData itemData { get; private set; }

        public void SetupAction(ItemData itemData)
        {
            this.itemData = itemData;
            if (itemData == null || itemData.Item == null)
            {
                ResetButton();
                return;
            }

            iconImage.sprite = itemData.Item.InventorySprite(itemData);
            iconImage.enabled = true;

            if (itemData.Item is Equipment)
            {
                if (itemData.Item is Ammunition && playerActionHandler.unit.UnitEquipment.QuiverEquipped() && playerActionHandler.unit.QuiverInventoryManager.Contains(itemData))
                {
                    actionType = playerActionHandler.GetAction<ReloadAction>().actionType;
                    action = actionType.GetAction(playerActionHandler.unit);

                    button.onClick.RemoveAllListeners();
                    button.onClick.AddListener(() =>
                    {
                        if (playerActionHandler.queuedActions.Count == 0)
                        {
                            ReloadAction reloadAction = action as ReloadAction;
                            reloadAction.QueueAction(/*itemData*/);
                            if (itemData.CurrentStackSize == 1)
                                ResetButton();
                        }
                    });
                }
                else
                {
                    actionType = playerActionHandler.GetAction<EquipAction>().actionType;
                    action = actionType.GetAction(playerActionHandler.unit);

                    button.onClick.RemoveAllListeners();
                    button.onClick.AddListener(() =>
                    {
                        if (playerActionHandler.queuedActions.Count == 0)
                        {
                            EquipAction equipAction = action as EquipAction;
                            equipAction.QueueAction(itemData, itemData.Item.Equipment.EquipSlot, null);
                            ResetButton();
                        }
                    });
                }
            }
        }

        public override void ResetButton()
        {
            base.ResetButton();
            itemData = null;
        }
    }
}
