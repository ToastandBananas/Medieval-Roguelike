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

            SetupIconSprite();
            ShowSlot();

            if (itemData.Item is Equipment)
            {
                if (itemData.Item is Ammunition)
                {
                    // We just need to assign any basic action, since the Player might not have a Reload Action at the time of assigning the item to a hotbar slot
                    actionType = playerActionHandler.GetAction<InventoryAction>().actionType;
                    action = actionType.GetAction(playerActionHandler.unit);

                    button.onClick.RemoveAllListeners();
                    button.onClick.AddListener(() =>
                    {
                        if (playerActionHandler.queuedActions.Count == 0)
                        {
                            // Only do something if the Player has a valid ranged weapon equipped
                            if (playerActionHandler.unit.UnitEquipment.RangedWeaponEquipped() && playerActionHandler.unit.unitMeshManager.GetHeldRangedWeapon().itemData.Item.RangedWeapon.ProjectileType == itemData.Item.Ammunition.ProjectileType)
                            {
                                if (itemData.CurrentStackSize == 1)
                                {
                                    playerActionHandler.GetAction<ReloadAction>().QueueAction(itemData);
                                    ResetButton();
                                }
                                else
                                    playerActionHandler.GetAction<ReloadAction>().QueueAction(itemData);
                            }
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
                            if (playerActionHandler.unit.UnitEquipment.CanEquipItem(itemData) == false)
                                return;

                            EquipAction equipAction = action as EquipAction;
                            equipAction.QueueAction(itemData, itemData.Item.Equipment.EquipSlot, null);
                            ResetButton();
                        }
                    });
                }
            }
            else if (itemData.Item is Consumable)
            {
                actionType = playerActionHandler.GetAction<ConsumeAction>().actionType;
                action = actionType.GetAction(playerActionHandler.unit);

                button.onClick.RemoveAllListeners();
                button.onClick.AddListener(() =>
                {
                    if (playerActionHandler.queuedActions.Count == 0)
                    {
                        if ((itemData.Item.MaxStackSize > 1 && itemData.CurrentStackSize == 1) || (itemData.Item.MaxUses > 1 && itemData.RemainingUses == 1))
                        {
                            ConsumeAction consumeAction = action as ConsumeAction;
                            consumeAction.QueueAction(itemData);
                            ResetButton();
                        }
                        else
                        {
                            ConsumeAction consumeAction = action as ConsumeAction;
                            consumeAction.QueueAction(itemData);
                        }
                    }
                });
            }
        }

        public void SetupIconSprite()
        {
            if (itemData.Item.HotbarSprite != null)
                iconImage.sprite = itemData.Item.HotbarSprite;
            else
                iconImage.sprite = itemData.Item.InventorySprite(itemData);
        }

        public override void ShowSlot()
        {
            iconImage.enabled = true;
            ActivateButton();
        }

        public override void ResetButton()
        {
            base.ResetButton();
            itemData = null;
        }
    }
}
