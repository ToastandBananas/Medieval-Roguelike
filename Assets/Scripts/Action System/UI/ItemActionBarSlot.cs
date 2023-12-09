using InventorySystem;
using UnitSystem.ActionSystem.Actions;

namespace UnitSystem.ActionSystem.UI
{
    public class ItemActionBarSlot : ActionBarSlot
    {
        public ItemData ItemData { get; private set; }

        public void SetupAction(ItemData itemData)
        {
            ItemData = itemData;
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
                    ActionType = playerActionHandler.GetAction<Action_Inventory>().ActionType;
                    Action = ActionType.GetAction(playerActionHandler.Unit);

                    button.onClick.RemoveAllListeners();
                    button.onClick.AddListener(() =>
                    {
                        if (playerActionHandler.QueuedActions.Count == 0)
                        {
                            // Only do something if the Player has a valid ranged weapon equipped that's not already loaded and valid ammunition
                            HeldRangedWeapon heldRangedWeapon = playerActionHandler.Unit.UnitMeshManager.GetHeldRangedWeapon();
                            if (heldRangedWeapon != null && heldRangedWeapon.IsLoaded == false && heldRangedWeapon.ItemData.Item.RangedWeapon.ProjectileType == itemData.Item.Ammunition.ProjectileType)
                            {
                                playerActionHandler.GetAction<Action_Reload>().QueueAction(itemData);
                                if (itemData.CurrentStackSize == 1)
                                    ResetButton();
                            }
                        }
                    });
                }
                else
                {
                    ActionType = playerActionHandler.GetAction<Action_Equip>().ActionType;
                    Action = ActionType.GetAction(playerActionHandler.Unit);

                    button.onClick.RemoveAllListeners();
                    button.onClick.AddListener(() =>
                    {
                        if (playerActionHandler.QueuedActions.Count == 0)
                        {
                            if (playerActionHandler.Unit.UnitEquipment.CanEquipItem(itemData) == false)
                                return;

                            Action_Equip equipAction = Action as Action_Equip;
                            equipAction.QueueAction(itemData, itemData.Item.Equipment.EquipSlot, null);
                            ResetButton();
                        }
                    });
                }
            }
            else if (itemData.Item is Consumable)
            {
                ActionType = playerActionHandler.GetAction<Action_Consume>().ActionType;
                Action = ActionType.GetAction(playerActionHandler.Unit);

                button.onClick.RemoveAllListeners();
                button.onClick.AddListener(() =>
                {
                    if (playerActionHandler.QueuedActions.Count == 0)
                    {
                        if ((itemData.Item.MaxStackSize > 1 && itemData.CurrentStackSize == 1) || (itemData.Item.MaxUses > 1 && itemData.RemainingUses == 1))
                        {
                            Action_Consume consumeAction = Action as Action_Consume;
                            consumeAction.QueueAction(itemData);
                            ResetButton();
                        }
                        else
                        {
                            Action_Consume consumeAction = Action as Action_Consume;
                            consumeAction.QueueAction(itemData);
                        }
                    }
                });
            }
        }

        public void SetupIconSprite() => iconImage.sprite = ItemData.Item.HotbarSprite(ItemData);

        public override void ShowSlot()
        {
            iconImage.enabled = true;
            ActivateButton();
        }

        public override void ResetButton()
        {
            base.ResetButton();
            ItemData = null;
        }
    }
}
