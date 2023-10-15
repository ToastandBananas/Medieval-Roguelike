using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using InteractableObjects;
using GridSystem;
using ActionSystem;
using InventorySystem;
using UnitSystem;

namespace GeneralUI
{
    public class ContextMenuButton : MonoBehaviour
    {
        [SerializeField] Button button;
        [SerializeField] TextMeshProUGUI buttonText;
        [SerializeField] RectTransform rectTransform;

        StringBuilder stringBuilder = new StringBuilder();

        public void SetupMoveToButton(GridPosition targetGridPosition) => SetupButton("Move To", delegate { MoveTo(targetGridPosition); });

        void MoveTo(GridPosition targetGridPosition)
        {
            UnitManager.player.unitActionHandler.GetAction<MoveAction>().QueueAction(targetGridPosition);
            ContextMenu.DisableContextMenu();
        }

        public void SetupAttackButton()
        {
            if (UnitManager.player.unitActionHandler.IsInAttackRange(ContextMenu.targetUnit, true))
                SetupButton("Attack", delegate { Attack(true); });
            else
                SetupButton("Move to Attack", delegate { Attack(false); });
        }

        void Attack(bool isInAttackRange)
        {
            UnitManager.player.unitActionHandler.SetTargetEnemyUnit(ContextMenu.targetUnit);

            if (isInAttackRange)
                UnitManager.player.unitActionHandler.AttackTarget();
            else
            {
                // If the player has a ranged weapon equipped, find the nearest possible Shoot Action attack position
                if (UnitManager.player.UnitEquipment.RangedWeaponEquipped() && UnitManager.player.UnitEquipment.HasValidAmmunitionEquipped())
                    UnitManager.player.unitActionHandler.GetAction<MoveAction>().QueueAction(UnitManager.player.unitActionHandler.GetAction<ShootAction>().GetNearestAttackPosition(UnitManager.player.GridPosition, ContextMenu.targetUnit));
                else // If the player has a melee weapon equipped or is unarmed, find the nearest possible Melee Action attack position
                    UnitManager.player.unitActionHandler.GetAction<MoveAction>().QueueAction(UnitManager.player.unitActionHandler.GetAction<MeleeAction>().GetNearestAttackPosition(UnitManager.player.GridPosition, ContextMenu.targetUnit));
            }

            ContextMenu.DisableContextMenu();
        }

        public void SetupAddToBackpackButton(ItemData itemData)
        {
            stringBuilder.Clear();
            stringBuilder.Append("Put in ");
            if (UnitManager.player.UnitEquipment.EquippedItemDatas[(int)EquipSlot.Back].Item is Backpack)
                stringBuilder.Append("Bag");

            SetupButton(stringBuilder.ToString(), delegate { AddToBackpack(itemData); });
        }

        void AddToBackpack(ItemData itemData)
        {
            if (UnitManager.player.BackpackInventoryManager.TryAddItem(itemData, UnitManager.player))
            {
                if (ContextMenu.targetInteractable != null && ContextMenu.targetInteractable is LooseItem)
                    LooseItemPool.ReturnToPool((LooseItem)ContextMenu.targetInteractable);
            }
            else if (ContextMenu.targetInteractable != null && ContextMenu.targetInteractable is LooseItem)
            {
                LooseItem looseItem = ContextMenu.targetInteractable as LooseItem;
                looseItem.FumbleItem();
            }

            ContextMenu.DisableContextMenu();
        }

        public void SetupTakeItemButton(ItemData itemData)
        {
            stringBuilder.Clear();
            stringBuilder.Append("Take");
            if (itemData.MyInventory() != null && itemData.MyInventory() is ContainerInventory)
            {
                ContainerInventory containerInventory = itemData.MyInventory() as ContainerInventory;
                if (containerInventory.containerInventoryManager == UnitManager.player.BackpackInventoryManager || containerInventory.containerInventoryManager == UnitManager.player.QuiverInventoryManager)
                    stringBuilder.Append(" Out");
            }

            SetupButton(stringBuilder.ToString(), delegate { TakeItem(itemData); });
        }

        void TakeItem(ItemData itemData)
        {
            if (itemData.MyInventory() != null && itemData.MyInventory() is ContainerInventory)
            {
                ContainerInventory containerInventory = itemData.MyInventory() as ContainerInventory;
                if (containerInventory.containerInventoryManager == UnitManager.player.BackpackInventoryManager || containerInventory.containerInventoryManager == UnitManager.player.QuiverInventoryManager)
                    UnitManager.player.MainInventory.TryAddItem(itemData, UnitManager.player);
            }
            else if (UnitManager.player.TryAddItemToInventories(itemData))
            {
                if (InventoryUI.npcEquipmentSlots[0].UnitEquipment != null && InventoryUI.npcEquipmentSlots[0].UnitEquipment.ItemDataEquipped(itemData))
                    InventoryUI.npcEquipmentSlots[0].UnitEquipment.RemoveEquipment(itemData);
                else if (ContextMenu.targetInteractable != null && ContextMenu.targetInteractable is LooseItem)
                    LooseItemPool.ReturnToPool((LooseItem)ContextMenu.targetInteractable);
            }
            else if (ContextMenu.targetInteractable != null && ContextMenu.targetInteractable is LooseItem)
            {
                LooseItem looseItem = ContextMenu.targetInteractable as LooseItem;
                looseItem.FumbleItem();
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
                    if (ContextMenu.targetSlot is ContainerEquipmentSlot)
                    {
                        ContainerEquipmentSlot containerSlot = ContextMenu.targetSlot as ContainerEquipmentSlot;
                        if ((containerSlot.EquipSlot != EquipSlot.Quiver || containerSlot.GetItemData().Item is Quiver) && containerSlot.containerInventoryManager.ContainsAnyItems()) // We always will drop equipped containers if they have items in them, as they can't go in the inventory
                            stringBuilder.Append(" & Drop");
                    }

                    SetupButton(stringBuilder.ToString(), UnequipItem);
                }
                else if (ContextMenu.targetInteractable != null || (ContextMenu.targetSlot != null && (ContextMenu.targetSlot is EquipmentSlot == false || ContextMenu.targetSlot.InventoryItem.myUnitEquipment != UnitManager.player.UnitEquipment)))
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
            if (itemData.Item.Use(UnitManager.player, itemData, amountToUse))
            {
                if (ContextMenu.targetInteractable != null && ContextMenu.targetInteractable is LooseContainerItem)
                {
                    LooseContainerItem looseContainerItem = ContextMenu.targetInteractable as LooseContainerItem;
                    if (looseContainerItem.ContainerInventoryManager.ParentInventory.slotVisualsCreated)
                        InventoryUI.GetContainerUI(looseContainerItem.ContainerInventoryManager).CloseContainerInventory();

                    if (itemData.Item.Equipment.EquipSlot == EquipSlot.Quiver)
                    {
                        UnitManager.player.QuiverInventoryManager.SwapInventories(looseContainerItem.ContainerInventoryManager);
                        if (UnitManager.player.UnitEquipment.slotVisualsCreated && itemData.Item is Quiver)
                            UnitManager.player.UnitEquipment.GetEquipmentSlot(EquipSlot.Quiver).InventoryItem.QuiverInventoryItem.UpdateQuiverSprites();
                    }
                    else if (itemData.Item.Equipment.EquipSlot == EquipSlot.Back)
                        UnitManager.player.BackpackInventoryManager.SwapInventories(looseContainerItem.ContainerInventoryManager);
                }
                else if (ContextMenu.targetSlot != null)
                {
                    if (itemData.Item.Equipment.EquipSlot == EquipSlot.Quiver)
                    {
                        UnitManager.player.QuiverInventoryManager.Initialize();
                        if (UnitManager.player.UnitEquipment.slotVisualsCreated && itemData.Item is Quiver)
                            UnitManager.player.UnitEquipment.GetEquipmentSlot(EquipSlot.Quiver).InventoryItem.QuiverInventoryItem.UpdateQuiverSprites();
                    }
                    else if (itemData.Item.Equipment.EquipSlot == EquipSlot.Back)
                        UnitManager.player.BackpackInventoryManager.Initialize();
                }

                if (ContextMenu.targetInteractable != null && ContextMenu.targetInteractable is LooseItem)
                    LooseItemPool.ReturnToPool((LooseItem)ContextMenu.targetInteractable);
            }
            else if (ContextMenu.targetInteractable != null && ContextMenu.targetInteractable is LooseItem)
            {
                LooseItem looseItem = ContextMenu.targetInteractable as LooseItem;
                looseItem.FumbleItem();
            }

            ContextMenu.DisableContextMenu();
        }

        void UnequipItem()
        {
            EquipmentSlot equipmentSlot = ContextMenu.targetSlot as EquipmentSlot;
            UnitManager.player.unitActionHandler.GetAction<UnequipAction>().QueueAction(equipmentSlot.EquipSlot);
            ContextMenu.DisableContextMenu();
        }

        public void SetupOpenContainerButton() => SetupButton("Search", OpenContainer);

        void OpenContainer()
        {
            if (ContextMenu.targetSlot != null)
            {
                ContainerEquipmentSlot containerEquipmentSlot = ContextMenu.targetSlot as ContainerEquipmentSlot;
                InventoryUI.ShowContainerUI(containerEquipmentSlot.containerInventoryManager, containerEquipmentSlot.ParentSlot().GetItemData().Item);
            }
            else if (ContextMenu.targetInteractable != null)
            {
                LooseContainerItem looseContainerItem = ContextMenu.targetInteractable as LooseContainerItem;
                InventoryUI.ShowContainerUI(looseContainerItem.ContainerInventoryManager, looseContainerItem.ItemData.Item);
            }
            else if (ContextMenu.targetUnit != null)
                UnitManager.player.unitActionHandler.GetAction<InteractAction>().QueueAction(ContextMenu.targetUnit.unitInteractable);

            ContextMenu.DisableContextMenu();
        }

        public void SetupCloseContainerButton() => SetupButton("Close", CloseContainer);

        void CloseContainer()
        {
            if (ContextMenu.targetSlot != null)
            {
                ContainerEquipmentSlot containerEquipmentSlot = ContextMenu.targetSlot as ContainerEquipmentSlot;
                InventoryUI.GetContainerUI(containerEquipmentSlot.containerInventoryManager).CloseContainerInventory();
            }
            else if (ContextMenu.targetInteractable != null)
            {
                LooseContainerItem looseContainerItem = ContextMenu.targetInteractable as LooseContainerItem;
                InventoryUI.GetContainerUI(looseContainerItem.ContainerInventoryManager).CloseContainerInventory();
            }
            else if (ContextMenu.targetUnit != null)
                InventoryUI.ToggleNPCInventory();

            ContextMenu.DisableContextMenu();
        }

        public void SetupSplitStackButton(ItemData itemData) => SetupButton("Split Stack", delegate { OpenSplitStackUI(itemData, ContextMenu.targetSlot); });

        void OpenSplitStackUI(ItemData itemData, Slot targetSlot)
        {
            SplitStack.Instance.Open(itemData, targetSlot);
            ContextMenu.DisableContextMenu();
        }

        public void SetupDropItemButton() => SetupButton("Drop", DropItem);

        void DropItem()
        {
            if (ContextMenu.targetSlot != null)
            {
                if (ContextMenu.targetSlot.InventoryItem.myUnitEquipment != null)
                {
                    EquipmentSlot targetEquipmentSlot = ContextMenu.targetSlot as EquipmentSlot;
                    DropItemManager.DropItem(ContextMenu.targetSlot.InventoryItem.myUnitEquipment, targetEquipmentSlot.EquipSlot);
                }
                else if (ContextMenu.targetSlot.InventoryItem.myInventory != null)
                    DropItemManager.DropItem(ContextMenu.targetSlot.InventoryItem.GetMyUnit(), ContextMenu.targetSlot.InventoryItem.myInventory, ContextMenu.targetSlot.GetItemData());
            }

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
