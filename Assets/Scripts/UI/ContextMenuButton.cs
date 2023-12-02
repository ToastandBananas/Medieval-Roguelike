using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using InteractableObjects;
using GridSystem;
using UnitSystem.ActionSystem;
using UnitSystem.ActionSystem.UI;
using InventorySystem;
using UnitSystem;

namespace GeneralUI
{
    public class ContextMenuButton : MonoBehaviour
    {
        [SerializeField] Button button;
        [SerializeField] TextMeshProUGUI buttonText;
        [SerializeField] RectTransform rectTransform;

        StringBuilder stringBuilder = new();

        public void SetupReloadButton(ItemData projectileItemData) => SetupButton($"Reload with {projectileItemData.Item.Name}", delegate { ReloadProjectile(projectileItemData); });

        void ReloadProjectile(ItemData projectileItemData)
        {
            ReloadAction reloadAction = UnitManager.player.unitActionHandler.GetAction<ReloadAction>();
            if (reloadAction != null)
                reloadAction.QueueAction(projectileItemData);
            ContextMenu.DisableContextMenu();
        }

        public void SetupThrowWeaponButton(ItemData itemDataToThrow)
        {
            stringBuilder.Clear();
            stringBuilder.Append("Throw");
            if (UnitManager.player.UnitEquipment.ItemDataEquipped(itemDataToThrow))
                stringBuilder.Append(" Equipped");
            stringBuilder.Append($" {itemDataToThrow.Item.Name}");
            SetupButton(stringBuilder.ToString(), delegate { ReadyThrownWeapon(itemDataToThrow); });
        }

        void ReadyThrownWeapon(ItemData itemDataToThrow)
        {
            ThrowAction throwAction = UnitManager.player.unitActionHandler.GetAction<ThrowAction>();
            if (throwAction != null && itemDataToThrow != null)
            {
                UnitManager.player.unitActionHandler.PlayerActionHandler.SetSelectedActionType(throwAction.ActionType, false);
                throwAction.SetItemToThrow(itemDataToThrow);
                GridSystemVisual.UpdateAttackRangeGridVisual();
            }

            ContextMenu.DisableContextMenu();
        }

        public void SetupThrowItemButton() => SetupButton($"Throw", ReadyThrownItem);

        void ReadyThrownItem()
        {
            ThrowAction throwAction = UnitManager.player.unitActionHandler.GetAction<ThrowAction>();
            if (throwAction != null)
            {
                throwAction.SetItemToThrow(ContextMenu.TargetSlot.GetItemData());
                UnitManager.player.unitActionHandler.PlayerActionHandler.SetSelectedActionType(throwAction.ActionType, false);
                ActionSystemUI.SetSelectedActionSlot(ActionSystemUI.GetActionBarSlot(throwAction.ActionType));
                GridSystemVisual.UpdateAttackRangeGridVisual();
            }

            ContextMenu.DisableContextMenu();
        }

        public void SetupMoveToButton(GridPosition targetGridPosition) => SetupButton("Move To", delegate { MoveTo(targetGridPosition); });

        void MoveTo(GridPosition targetGridPosition)
        {
            UnitManager.player.unitActionHandler.MoveAction.QueueAction(targetGridPosition);
            ContextMenu.DisableContextMenu();
        }

        public void SetupAttackButton()
        {
            if (UnitManager.player.unitActionHandler.IsInAttackRange(ContextMenu.TargetUnit, true))
                SetupButton("Attack", delegate { Attack(true); });
            else
                SetupButton("Move to Attack", delegate { Attack(false); });
        }

        void Attack(bool isInAttackRange)
        {
            UnitManager.player.unitActionHandler.SetTargetEnemyUnit(ContextMenu.TargetUnit);

            if (isInAttackRange)
                UnitManager.player.unitActionHandler.PlayerActionHandler.AttackTarget();
            else
            {
                // If the player has a ranged weapon equipped, find the nearest possible Shoot Action attack position
                if (UnitManager.player.UnitEquipment.RangedWeaponEquipped && UnitManager.player.UnitEquipment.HasValidAmmunitionEquipped())
                    UnitManager.player.unitActionHandler.MoveAction.QueueAction(UnitManager.player.unitActionHandler.GetAction<ShootAction>().GetNearestAttackPosition(UnitManager.player.GridPosition, ContextMenu.TargetUnit));
                else // If the player has a melee weapon equipped or is unarmed, find the nearest possible Melee Action attack position
                    UnitManager.player.unitActionHandler.MoveAction.QueueAction(UnitManager.player.unitActionHandler.GetAction<MeleeAction>().GetNearestAttackPosition(UnitManager.player.GridPosition, ContextMenu.TargetUnit));
            }

            ContextMenu.DisableContextMenu();
        }

        public void SetupAddToBackpackButton(ItemData itemData) => SetupButton($"Put in Backpack", delegate { AddToBackpack(itemData); });

        void AddToBackpack(ItemData itemData)
        {
            if (UnitManager.player.BackpackInventoryManager.TryAddItem(itemData, UnitManager.player))
            {
                if (ContextMenu.TargetInteractable != null && ContextMenu.TargetInteractable is LooseItem)
                    LooseItemPool.ReturnToPool((LooseItem)ContextMenu.TargetInteractable);
            }
            else if (ContextMenu.TargetInteractable != null && ContextMenu.TargetInteractable is LooseItem)
            {
                LooseItem looseItem = ContextMenu.TargetInteractable as LooseItem;
                looseItem.JiggleItem();
            }

            ContextMenu.DisableContextMenu();
        }

        public void SetupAddToBeltBagButton(ItemData itemData) => SetupButton($"Put in Belt Pouch", delegate { AddToBeltBag(itemData); });

        void AddToBeltBag(ItemData itemData)
        {
            if (UnitManager.player.BeltInventoryManager.TryAddItem(itemData, UnitManager.player))
            {
                if (ContextMenu.TargetInteractable != null && ContextMenu.TargetInteractable is LooseItem)
                    LooseItemPool.ReturnToPool((LooseItem)ContextMenu.TargetInteractable);
            }
            else if (ContextMenu.TargetInteractable != null && ContextMenu.TargetInteractable is LooseItem)
            {
                LooseItem looseItem = ContextMenu.TargetInteractable as LooseItem;
                looseItem.JiggleItem();
            }

            ContextMenu.DisableContextMenu();
        }

        public void SetupTakeItemButton(ItemData itemData)
        {
            stringBuilder.Clear();
            stringBuilder.Append("Take");
            if (itemData.MyInventory != null && itemData.MyInventory is ContainerInventory)
            {
                ContainerInventory containerInventory = itemData.MyInventory as ContainerInventory;
                if (containerInventory.containerInventoryManager == UnitManager.player.BackpackInventoryManager || containerInventory.containerInventoryManager == UnitManager.player.QuiverInventoryManager)
                    stringBuilder.Append(" Out");
            }

            SetupButton(stringBuilder.ToString(), delegate { TakeItem(itemData); });
        }

        void TakeItem(ItemData itemData)
        {
            if (ContextMenu.TargetInteractable != null && ContextMenu.TargetInteractable is LooseItem)
            {
                UnitManager.player.unitActionHandler.InteractAction.QueueAction(ContextMenu.TargetInteractable);
            }
            else if (itemData.MyInventory != null && itemData.MyInventory is ContainerInventory)
            {
                ContainerInventory containerInventory = itemData.MyInventory as ContainerInventory;
                if (containerInventory.containerInventoryManager == UnitManager.player.BackpackInventoryManager || containerInventory.containerInventoryManager == UnitManager.player.BeltInventoryManager || containerInventory.containerInventoryManager == UnitManager.player.QuiverInventoryManager)
                    UnitManager.player.UnitInventoryManager.MainInventory.TryAddItem(itemData, UnitManager.player);
                else
                    UnitManager.player.UnitInventoryManager.TryAddItemToInventories(itemData);
            }
            else if (UnitManager.player.UnitInventoryManager.TryAddItemToInventories(itemData))
            {
                if (InventoryUI.npcEquipmentSlots[0].UnitEquipment != null && InventoryUI.npcEquipmentSlots[0].UnitEquipment.ItemDataEquipped(itemData))
                    InventoryUI.npcEquipmentSlots[0].UnitEquipment.RemoveEquipment(itemData);
                else if (ContextMenu.TargetInteractable != null && ContextMenu.TargetInteractable is LooseItem)
                    LooseItemPool.ReturnToPool((LooseItem)ContextMenu.TargetInteractable);
            }

            ContextMenu.DisableContextMenu();
        }

        public void SetupUseItemButton(ItemData itemData, int amountToUse = 1)
        {
            if (itemData.Item is Equipment)
            {
                if (UnitManager.player.UnitEquipment.ItemDataEquipped(itemData))
                {
                    stringBuilder.Clear();
                    stringBuilder.Append("Unequip");
                    if (ContextMenu.TargetSlot is ContainerEquipmentSlot)
                    {
                        ContainerEquipmentSlot containerSlot = ContextMenu.TargetSlot as ContainerEquipmentSlot;
                        if (containerSlot.containerInventoryManager.ContainsAnyItems()) // We always will drop equipped containers if they have items in them, as they can't go in the inventory
                            stringBuilder.Append(" & Drop");
                    }

                    SetupButton(stringBuilder.ToString(), UnequipItem);
                }
                else if (ContextMenu.TargetInteractable != null || (ContextMenu.TargetSlot != null && (ContextMenu.TargetSlot is EquipmentSlot == false || ContextMenu.TargetSlot.InventoryItem.myUnitEquipment != UnitManager.player.UnitEquipment)))
                {
                    if (itemData.Item is Ammunition && UnitManager.player.UnitEquipment.EquipSlotHasItem(EquipSlot.Quiver) && UnitManager.player.UnitEquipment.EquippedItemDatas[(int)EquipSlot.Quiver].Item is Quiver)
                    {
                        Quiver quiver = UnitManager.player.UnitEquipment.EquippedItemDatas[(int)EquipSlot.Quiver].Item as Quiver;
                        if (quiver.AllowedProjectileType == itemData.Item.Ammunition.ProjectileType)
                            SetupButton("Add to Quiver", delegate { UseItem(itemData); });
                        else
                            SetupButton("Equip", delegate { UseItem(itemData); });
                    }
                    else
                        SetupButton("Equip", delegate { UseItem(itemData); });
                }
            }
            else
            {
                stringBuilder.Clear();

                if (itemData.Item is Consumable)
                {
                    Consumable consumable = itemData.Item as Consumable;

                    if ((itemData.Item.MaxUses > 1 && amountToUse != itemData.RemainingUses) || (itemData.Item.MaxStackSize > 1 && amountToUse != itemData.CurrentStackSize))
                        stringBuilder.Append("    ");

                    if (consumable.ItemType == ItemType.Food)
                        stringBuilder.Append("Eat");
                    else
                        stringBuilder.Append("Drink");
                }
                else
                    stringBuilder.Append("Use");

                if (itemData.Item.MaxUses > 1)
                {
                    if (amountToUse == itemData.RemainingUses)
                    {
                        if (itemData.RemainingUses > 1)
                        {
                            if (itemData.RemainingUses < itemData.Item.MaxUses)
                                stringBuilder.Append(" Remaining");
                            else
                                stringBuilder.Append(" All");
                        }
                    }
                    else if (amountToUse > 1 && itemData.RemainingUses >= 4 && amountToUse == Mathf.CeilToInt(itemData.RemainingUses * 0.75f))
                        stringBuilder.Append($" Three Quarters ({Mathf.CeilToInt(itemData.RemainingUses * 0.75f)})");
                    else if (amountToUse > 1 && itemData.RemainingUses >= 4 && amountToUse == Mathf.CeilToInt(itemData.RemainingUses * 0.5f))
                        stringBuilder.Append($" Half ({Mathf.CeilToInt(itemData.RemainingUses * 0.5f)})");
                    else if (amountToUse > 1 && itemData.RemainingUses >= 4 && amountToUse == Mathf.CeilToInt(itemData.RemainingUses * 0.25f))
                        stringBuilder.Append($" a Quarter ({Mathf.CeilToInt(itemData.RemainingUses * 0.25f)})");
                    else if (amountToUse > 1 && itemData.RemainingUses >= 10 && amountToUse == Mathf.CeilToInt(itemData.RemainingUses * 0.1f))
                        stringBuilder.Append($" a Little Bit ({Mathf.CeilToInt(itemData.RemainingUses * 0.1f)})");
                    else if (amountToUse == 1)
                        stringBuilder.Append(" One Portion");
                }
                else if (itemData.Item.MaxStackSize > 1)
                {
                    if (amountToUse == itemData.CurrentStackSize)
                    {
                        if (itemData.CurrentStackSize > 1)
                        {
                            if (itemData.CurrentStackSize < itemData.Item.MaxStackSize)
                                stringBuilder.Append(" Remaining");
                            else
                                stringBuilder.Append(" All");
                        }
                    }
                    else if (amountToUse > 1 && itemData.CurrentStackSize >= 4 && amountToUse == Mathf.CeilToInt(itemData.CurrentStackSize * 0.75f))
                        stringBuilder.Append($" Three Quarters ({Mathf.CeilToInt(itemData.CurrentStackSize * 0.75f)})");
                    else if (amountToUse > 1 && itemData.CurrentStackSize >= 4 && amountToUse == Mathf.CeilToInt(itemData.CurrentStackSize * 0.5f))
                        stringBuilder.Append($" Half ({Mathf.CeilToInt(itemData.CurrentStackSize * 0.5f)})");
                    else if (amountToUse > 1 && itemData.CurrentStackSize >= 4 && amountToUse == Mathf.CeilToInt(itemData.CurrentStackSize * 0.25f))
                        stringBuilder.Append($" a Quarter ({Mathf.CeilToInt(itemData.CurrentStackSize * 0.25f)})");
                    else if (amountToUse > 1 && itemData.CurrentStackSize >= 10 && amountToUse == Mathf.CeilToInt(itemData.CurrentStackSize * 0.1f))
                        stringBuilder.Append($" a Little Bit ({Mathf.CeilToInt(itemData.CurrentStackSize * 0.1f)})");
                    else if (amountToUse == 1)
                        stringBuilder.Append(" One");
                }

                SetupButton(stringBuilder.ToString(), delegate { UseItem(itemData, amountToUse); });
            }
        }

        void UseItem(ItemData itemData, int amountToUse = 1)
        {
            if (itemData.Item.Use(UnitManager.player, itemData, ContextMenu.TargetSlot != null ? ContextMenu.TargetSlot : null, ContextMenu.TargetInteractable != null && ContextMenu.TargetInteractable is LooseContainerItem ? ContextMenu.TargetInteractable as LooseContainerItem : null, amountToUse))
            {
                if (ContextMenu.TargetInteractable != null && ContextMenu.TargetInteractable is LooseItem)
                    LooseItemPool.ReturnToPool((LooseItem)ContextMenu.TargetInteractable);
            }
            else if (ContextMenu.TargetInteractable != null && ContextMenu.TargetInteractable is LooseItem)
            {
                LooseItem looseItem = ContextMenu.TargetInteractable as LooseItem;
                looseItem.JiggleItem();
            }

            ContextMenu.DisableContextMenu();
        }

        void UnequipItem()
        {
            EquipmentSlot equipmentSlot = ContextMenu.TargetSlot as EquipmentSlot;
            UnitManager.player.unitActionHandler.GetAction<UnequipAction>().QueueAction(equipmentSlot.EquipSlot, equipmentSlot is ContainerEquipmentSlot ? equipmentSlot.ContainerEquipmentSlot.containerInventoryManager : null);
            ContextMenu.DisableContextMenu();
        }

        public void SetupOpenContainerButton() => SetupButton("Search", OpenContainer);

        void OpenContainer()
        {
            if (ContextMenu.TargetSlot != null)
            {
                ContainerEquipmentSlot containerEquipmentSlot = ContextMenu.TargetSlot as ContainerEquipmentSlot;
                InventoryUI.ShowContainerUI(containerEquipmentSlot.containerInventoryManager, containerEquipmentSlot.ParentSlot().GetItemData().Item);
            }
            else if (ContextMenu.TargetInteractable != null)
                UnitManager.player.unitActionHandler.InteractAction.QueueAction(ContextMenu.TargetInteractable);
            else if (ContextMenu.TargetUnit != null)
                UnitManager.player.unitActionHandler.InteractAction.QueueAction(ContextMenu.TargetUnit.unitInteractable);

            ContextMenu.DisableContextMenu();
        }

        public void SetupCloseContainerButton() => SetupButton("Close", CloseContainer);

        void CloseContainer()
        {
            if (ContextMenu.TargetSlot != null)
            {
                ContainerEquipmentSlot containerEquipmentSlot = ContextMenu.TargetSlot as ContainerEquipmentSlot;
                InventoryUI.GetContainerUI(containerEquipmentSlot.containerInventoryManager).CloseContainerInventory();
            }
            else if (ContextMenu.TargetInteractable != null)
            {
                LooseContainerItem looseContainerItem = ContextMenu.TargetInteractable as LooseContainerItem;
                InventoryUI.GetContainerUI(looseContainerItem.ContainerInventoryManager).CloseContainerInventory();
            }
            else if (ContextMenu.TargetUnit != null)
                InventoryUI.ToggleNPCInventory();

            ContextMenu.DisableContextMenu();
        }

        public void SetupSplitStackButton(ItemData itemData) => SetupButton("Split Stack", delegate { OpenSplitStackUI(itemData, ContextMenu.TargetSlot); });

        void OpenSplitStackUI(ItemData itemData, Slot targetSlot)
        {
            SplitStack.Instance.Open(itemData, targetSlot);
            ContextMenu.DisableContextMenu();
        }

        public void SetupDropItemButton() => SetupButton("Drop", DropItem);

        void DropItem()
        {
            if (ContextMenu.TargetSlot != null)
            {
                if (ContextMenu.TargetSlot.InventoryItem.myUnitEquipment != null)
                {
                    EquipmentSlot targetEquipmentSlot = ContextMenu.TargetSlot as EquipmentSlot;

                    // If the slot owner is dead, then it just means the Player is trying to drop a dead Unit's equipment
                    if (targetEquipmentSlot.InventoryItem.myUnitEquipment.MyUnit.health.IsDead)
                        UnitManager.player.unitActionHandler.GetAction<InventoryAction>().QueueAction(targetEquipmentSlot.GetItemData(), targetEquipmentSlot.GetItemData().CurrentStackSize, targetEquipmentSlot is ContainerEquipmentSlot ? targetEquipmentSlot.ContainerEquipmentSlot.containerInventoryManager : null, InventoryActionType.Unequip);
                    else
                        targetEquipmentSlot.InventoryItem.myUnitEquipment.MyUnit.unitActionHandler.GetAction<InventoryAction>().QueueAction(ContextMenu.TargetSlot.GetItemData(), targetEquipmentSlot.GetItemData().CurrentStackSize, targetEquipmentSlot is ContainerEquipmentSlot ? targetEquipmentSlot.ContainerEquipmentSlot.containerInventoryManager : null, InventoryActionType.Unequip);

                    DropItemManager.DropItem(targetEquipmentSlot.InventoryItem.myUnitEquipment, targetEquipmentSlot.EquipSlot);
                }
                else if (ContextMenu.TargetSlot.InventoryItem.myInventory != null)
                    DropItemManager.DropItem(ContextMenu.TargetSlot.InventoryItem.myInventory, ContextMenu.TargetSlot.InventoryItem.GetMyUnit(), ContextMenu.TargetSlot.GetItemData());
            }

            ContextMenu.DisableContextMenu();
        }

        public void SetupAddItemToHotbarButton(ItemActionBarSlot itemActionBarSlot) => SetupButton("Add to Hotbar", delegate { AddItemToHotbar(itemActionBarSlot); });

        void AddItemToHotbar(ItemActionBarSlot itemActionBarSlot)
        {
            if (ContextMenu.TargetSlot != null)
                itemActionBarSlot.SetupAction(ContextMenu.TargetSlot.GetItemData());

            ContextMenu.DisableContextMenu();
        }

        public void SetupRemoveFromHotbarButton(ItemActionBarSlot itemActionBarSlot) => SetupButton("Remove from Hotbar", delegate { RemoveItemFromHotbar(itemActionBarSlot); });

        void RemoveItemFromHotbar(ItemActionBarSlot itemActionBarSlot)
        {
            itemActionBarSlot.ResetButton();
            ContextMenu.DisableContextMenu();
        }

        void SetupButton(string buttonText, UnityAction action)
        {
            gameObject.name = buttonText;
            this.buttonText.text = buttonText;
            button.onClick.AddListener(action);
            gameObject.SetActive(true);
        }

        public void Disable()
        {
            button.onClick.RemoveAllListeners();
            gameObject.SetActive(false);
        }

        public TextMeshProUGUI ButtonText => buttonText;

        public RectTransform RectTransform => rectTransform;
    }
}
