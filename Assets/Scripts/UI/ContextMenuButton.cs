using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class ContextMenuButton : MonoBehaviour
{
    [SerializeField] Button button;
    [SerializeField] TextMeshProUGUI buttonText;
    [SerializeField] RectTransform rectTransform;

    StringBuilder stringBuilder = new StringBuilder();

    public void SetupMoveToButton(GridPosition targetGridPosition) => SetupButton("Move To", delegate { MoveTo(targetGridPosition); }); 

    void MoveTo(GridPosition targetGridPosition)
    {
        UnitManager.Instance.player.unitActionHandler.SetTargetGridPosition(targetGridPosition);
        UnitManager.Instance.player.unitActionHandler.QueueAction(UnitManager.Instance.player.unitActionHandler.GetAction<MoveAction>());

        ContextMenu.Instance.DisableContextMenu();
    }

    public void SetupAddToBackpackButton(ItemData itemData)
    {
        stringBuilder.Clear();
        stringBuilder.Append("Put in ");
        if (UnitManager.Instance.player.CharacterEquipment.EquippedItemDatas[(int)EquipSlot.Back].Item is Backpack)
            stringBuilder.Append("Bag");

        SetupButton(stringBuilder.ToString(), delegate { AddToBackpack(itemData); });
    }

    void AddToBackpack(ItemData itemData) 
    {
        if (UnitManager.Instance.player.BackpackInventoryManager.TryAddItem(itemData))
        {
            if (ContextMenu.Instance.TargetInteractable != null && ContextMenu.Instance.TargetInteractable is LooseItem)
                LooseItemPool.ReturnToPool((LooseItem)ContextMenu.Instance.TargetInteractable);
        }
        else if (ContextMenu.Instance.TargetInteractable != null && ContextMenu.Instance.TargetInteractable is LooseItem)
        {
            LooseItem looseItem = ContextMenu.Instance.TargetInteractable as LooseItem;
            looseItem.FumbleItem();
        }

        ContextMenu.Instance.DisableContextMenu();
    }

    public void SetupTakeItemButton(ItemData itemData)
    {
        stringBuilder.Clear();
        stringBuilder.Append("Take");
        if (itemData.MyInventory() != null && itemData.MyInventory() is ContainerInventory)
        {
            ContainerInventory containerInventory = itemData.MyInventory() as ContainerInventory;
            if (containerInventory.containerInventoryManager == UnitManager.Instance.player.BackpackInventoryManager || containerInventory.containerInventoryManager == UnitManager.Instance.player.QuiverInventoryManager)
                stringBuilder.Append(" Out");
        }

        SetupButton(stringBuilder.ToString(), delegate { TakeItem(itemData); });
    }

    void TakeItem(ItemData itemData)
    {
        if (itemData.MyInventory() != null && itemData.MyInventory() is ContainerInventory)
        {
            ContainerInventory containerInventory = itemData.MyInventory() as ContainerInventory;
            if (containerInventory.containerInventoryManager == UnitManager.Instance.player.BackpackInventoryManager || containerInventory.containerInventoryManager == UnitManager.Instance.player.QuiverInventoryManager)
                UnitManager.Instance.player.MainInventory().TryAddItem(itemData);
        }
        else if (UnitManager.Instance.player.TryAddItemToInventories(itemData))
        {
            if (InventoryUI.Instance.npcEquipmentSlots[0].CharacterEquipment != null && InventoryUI.Instance.npcEquipmentSlots[0].CharacterEquipment.ItemDataEquipped(itemData))
                InventoryUI.Instance.npcEquipmentSlots[0].CharacterEquipment.RemoveEquipment(itemData);
            else if (ContextMenu.Instance.TargetInteractable != null && ContextMenu.Instance.TargetInteractable is LooseItem)
                LooseItemPool.ReturnToPool((LooseItem)ContextMenu.Instance.TargetInteractable);
        }
        else if (ContextMenu.Instance.TargetInteractable != null && ContextMenu.Instance.TargetInteractable is LooseItem)
        {
            LooseItem looseItem = ContextMenu.Instance.TargetInteractable as LooseItem;
            looseItem.FumbleItem();
        }

        ContextMenu.Instance.DisableContextMenu();
    }

    public void SetupUseItemButton(ItemData itemData, int amountToUse = 1)
    {
        if (itemData.Item is Equipment)
        {
            if (UnitManager.Instance.player.CharacterEquipment.ItemDataEquipped(itemData))
            {
                stringBuilder.Clear();
                stringBuilder.Append("Unequip");
                if (ContextMenu.Instance.TargetSlot is ContainerEquipmentSlot)
                {
                    ContainerEquipmentSlot containerSlot = ContextMenu.Instance.TargetSlot as ContainerEquipmentSlot;
                    if ((containerSlot.EquipSlot != EquipSlot.Quiver || containerSlot.GetItemData().Item is Quiver) && containerSlot.containerInventoryManager.ContainsAnyItems()) // We always will drop equipped containers if they have items in them, as they can't go in the inventory
                        stringBuilder.Append(" & Drop");
                }
                
                SetupButton(stringBuilder.ToString(), UnequipItem);
            }
            else if (ContextMenu.Instance.TargetInteractable != null || (ContextMenu.Instance.TargetSlot != null && ContextMenu.Instance.TargetSlot is EquipmentSlot == false))
            {
                if (itemData.Item is Ammunition && UnitManager.Instance.player.CharacterEquipment.EquipSlotHasItem(EquipSlot.Quiver) && UnitManager.Instance.player.CharacterEquipment.EquippedItemDatas[(int)EquipSlot.Quiver].Item is Quiver)
                {
                    Quiver quiver = UnitManager.Instance.player.CharacterEquipment.EquippedItemDatas[(int)EquipSlot.Quiver].Item as Quiver;
                    if (quiver.AllowedProjectileType == itemData.Item.Ammunition.ProjectileType)
                        SetupButton("Add to Quiver", delegate { UseItem(itemData); });
                    else
                        SetupButton("Equip", delegate { EquipItem(itemData); });
                }
                else
                    SetupButton("Equip", delegate { EquipItem(itemData); });
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
        if (itemData.Item.Use(UnitManager.Instance.player, itemData, amountToUse))
        {
            if (ContextMenu.Instance.TargetInteractable != null && ContextMenu.Instance.TargetInteractable is LooseItem)
                LooseItemPool.ReturnToPool((LooseItem)ContextMenu.Instance.TargetInteractable);
        }
        else if (ContextMenu.Instance.TargetInteractable != null && ContextMenu.Instance.TargetInteractable is LooseItem)
        {
            LooseItem looseItem = ContextMenu.Instance.TargetInteractable as LooseItem;
            looseItem.FumbleItem();
        }

        ContextMenu.Instance.DisableContextMenu();
    }

    void EquipItem(ItemData itemData)
    {
        if (UnitManager.Instance.player.CharacterEquipment.TryEquipItem(itemData))
        {
            if (ContextMenu.Instance.TargetInteractable != null && ContextMenu.Instance.TargetInteractable is LooseContainerItem)
            {
                LooseContainerItem looseContainerItem = ContextMenu.Instance.TargetInteractable as LooseContainerItem;
                if (looseContainerItem.ContainerInventoryManager.ParentInventory.SlotVisualsCreated)
                    InventoryUI.Instance.GetContainerUI(looseContainerItem.ContainerInventoryManager).CloseContainerInventory();

                if (itemData.Item.Equipment.EquipSlot == EquipSlot.Quiver)
                {
                    UnitManager.Instance.player.QuiverInventoryManager.TransferInventory(looseContainerItem.ContainerInventoryManager);
                    if (UnitManager.Instance.player.CharacterEquipment.SlotVisualsCreated && itemData.Item is Quiver)
                        UnitManager.Instance.player.CharacterEquipment.GetEquipmentSlot(EquipSlot.Quiver).InventoryItem.QuiverInventoryItem.UpdateQuiverSprites();
                }
                else if (itemData.Item.Equipment.EquipSlot == EquipSlot.Back)
                    UnitManager.Instance.player.BackpackInventoryManager.TransferInventory(looseContainerItem.ContainerInventoryManager);
            }
            else if (ContextMenu.Instance.TargetSlot != null)
            {
                if (itemData.Item.Equipment.EquipSlot == EquipSlot.Quiver)
                {
                    UnitManager.Instance.player.QuiverInventoryManager.Initialize();
                    if (UnitManager.Instance.player.CharacterEquipment.SlotVisualsCreated && itemData.Item is Quiver)
                        UnitManager.Instance.player.CharacterEquipment.GetEquipmentSlot(EquipSlot.Quiver).InventoryItem.QuiverInventoryItem.UpdateQuiverSprites();
                }
                else if (itemData.Item.Equipment.EquipSlot == EquipSlot.Back)
                    UnitManager.Instance.player.BackpackInventoryManager.Initialize();
            }

            if (ContextMenu.Instance.TargetInteractable != null && ContextMenu.Instance.TargetInteractable is LooseItem)
                LooseItemPool.ReturnToPool(ContextMenu.Instance.TargetInteractable as LooseItem);
        }
        else if (ContextMenu.Instance.TargetInteractable != null && ContextMenu.Instance.TargetInteractable is LooseItem)
        {
            LooseItem looseItem = ContextMenu.Instance.TargetInteractable as LooseItem;
            looseItem.FumbleItem();
        }

        ContextMenu.Instance.DisableContextMenu();
    }

    void UnequipItem()
    {
        EquipmentSlot equipmentSlot = ContextMenu.Instance.TargetSlot as EquipmentSlot;
        equipmentSlot.CharacterEquipment.UnequipItem(equipmentSlot.EquipSlot);
        ContextMenu.Instance.DisableContextMenu();
    }

    public void SetupOpenContainerButton() => SetupButton("Open", OpenContainer);

    void OpenContainer()
    {
        if (ContextMenu.Instance.TargetSlot != null && ContextMenu.Instance.TargetSlot is ContainerEquipmentSlot)
        {
            ContainerEquipmentSlot containerEquipmentSlot = ContextMenu.Instance.TargetSlot as ContainerEquipmentSlot;
            InventoryUI.Instance.ShowContainerUI(containerEquipmentSlot.containerInventoryManager, containerEquipmentSlot.ParentSlot().GetItemData().Item);
        }
        else if (ContextMenu.Instance.TargetInteractable != null && ContextMenu.Instance.TargetInteractable is LooseContainerItem)
        {
            LooseContainerItem looseContainerItem = ContextMenu.Instance.TargetInteractable as LooseContainerItem;
            InventoryUI.Instance.ShowContainerUI(looseContainerItem.ContainerInventoryManager, looseContainerItem.ItemData.Item);
        }

        ContextMenu.Instance.DisableContextMenu();
    }

    public void SetupCloseContainerButton() => SetupButton("Close", CloseContainer);

    void CloseContainer()
    {
        if (ContextMenu.Instance.TargetSlot != null)
        {
            ContainerEquipmentSlot containerEquipmentSlot = ContextMenu.Instance.TargetSlot as ContainerEquipmentSlot;
            InventoryUI.Instance.GetContainerUI(containerEquipmentSlot.containerInventoryManager).CloseContainerInventory();
        }
        else if (ContextMenu.Instance.TargetInteractable != null)
        {
            LooseContainerItem looseContainerItem = ContextMenu.Instance.TargetInteractable as LooseContainerItem;
            InventoryUI.Instance.GetContainerUI(looseContainerItem.ContainerInventoryManager).CloseContainerInventory();
        }

        ContextMenu.Instance.DisableContextMenu();
    }

    public void SetupSplitStackButton(ItemData itemData) => SetupButton("Split Stack", delegate { OpenSplitStackUI(itemData, ContextMenu.Instance.TargetSlot); });

    void OpenSplitStackUI(ItemData itemData, Slot targetSlot)
    {
        SplitStack.Instance.Open(itemData, targetSlot);
        ContextMenu.Instance.DisableContextMenu();
    }

    public void SetupDropItemButton() => SetupButton("Drop", DropItem);

    void DropItem()
    {
        if (ContextMenu.Instance.TargetSlot != null)
        {
            if (ContextMenu.Instance.TargetSlot.InventoryItem.myCharacterEquipment != null)
            {
                EquipmentSlot targetEquipmentSlot = ContextMenu.Instance.TargetSlot as EquipmentSlot;
                DropItemManager.DropItem(ContextMenu.Instance.TargetSlot.InventoryItem.myCharacterEquipment, targetEquipmentSlot.EquipSlot);
            }
            else if (ContextMenu.Instance.TargetSlot.InventoryItem.myInventory != null)
                DropItemManager.DropItem(ContextMenu.Instance.TargetSlot.InventoryItem.GetMyUnit(), ContextMenu.Instance.TargetSlot.InventoryItem.myInventory, ContextMenu.Instance.TargetSlot.GetItemData());
        }

        ContextMenu.Instance.DisableContextMenu();
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
