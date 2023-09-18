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

    public void SetupTakeItemButton()
    {

    }

    void TakeItem()
    {

    }

    public void SetupUseItemButton(ItemData itemData, int amountToUse = 1)
    {
        if (itemData.Item.IsEquipment())
        {
            if (UnitManager.Instance.player.CharacterEquipment.ItemDataEquipped(itemData))
                SetupButton("Unequip", UnequipItem);
            else if (ContextMenu.Instance.TargetInteractable != null || (ContextMenu.Instance.TargetSlot != null && ContextMenu.Instance.TargetSlot is EquipmentSlot == false))
            {
                UnityAction equipItemAction = () => { EquipItem(itemData); };
                SetupButton("Equip", equipItemAction);
            }
        }
        else
        {
            stringBuilder.Clear();

            if (itemData.Item is Consumable)
            {
                Consumable consumable = itemData.Item as Consumable;

                if (consumable.itemType == ItemType.Food)
                    stringBuilder.Append("Eat");
                else
                    stringBuilder.Append("Drink");
            }
            else
                stringBuilder.Append("Use");

            if (itemData.Item.maxUses > 1)
            {
                if (amountToUse == itemData.RemainingUses)
                {
                    if (itemData.RemainingUses > 1)
                    {
                        if (itemData.RemainingUses < itemData.Item.maxUses)
                            stringBuilder.Append(" Remaining");
                        else
                            stringBuilder.Append(" All");
                    }
                }
                else if (amountToUse == Mathf.FloorToInt(itemData.RemainingUses * 0.75f))
                    stringBuilder.Append(" Three Quarters");
                else if (amountToUse == Mathf.FloorToInt(itemData.RemainingUses * 0.5f))
                    stringBuilder.Append(" Half");
                else if (amountToUse == Mathf.FloorToInt(itemData.RemainingUses * 0.25f))
                    stringBuilder.Append(" a Quarter");
                else if (amountToUse == Mathf.FloorToInt(itemData.RemainingUses * 0.1f))
                    stringBuilder.Append(" a Little Bit");
            }
            else if (itemData.Item.maxStackSize > 1)
            {
                if (amountToUse == itemData.CurrentStackSize)
                {
                    if (itemData.CurrentStackSize > 1)
                    {
                        if (itemData.CurrentStackSize < itemData.Item.maxStackSize)
                            stringBuilder.Append(" Remaining");
                        else
                            stringBuilder.Append(" All");
                    }
                }
                else if (amountToUse == Mathf.FloorToInt(itemData.CurrentStackSize * 0.75f))
                    stringBuilder.Append(" Three Quarters");
                else if (amountToUse == Mathf.FloorToInt(itemData.CurrentStackSize * 0.5f))
                    stringBuilder.Append(" Half");
                else if (amountToUse == Mathf.FloorToInt(itemData.CurrentStackSize * 0.25f))
                    stringBuilder.Append(" a Quarter");
                else if (amountToUse > 1 && amountToUse == Mathf.FloorToInt(itemData.CurrentStackSize * 0.1f))
                    stringBuilder.Append(" a Little Bit");
                else if (amountToUse == 1)
                    stringBuilder.Append(" One");
            }

            UnityAction useItemAction = () => { UseItem(itemData, amountToUse); };
            SetupButton(stringBuilder.ToString(), useItemAction);
        }
    }

    void UseItem(ItemData itemData, int amountToUse)
    {
        itemData.Item.Use(UnitManager.Instance.player, itemData, amountToUse);
        ContextMenu.Instance.DisableContextMenu();
    }

    void EquipItem(ItemData itemData)
    {
        CharacterEquipment characterEquipment = UnitManager.Instance.player.CharacterEquipment;
        characterEquipment.TryAddItemAt(itemData.Item.Equipment().EquipSlot, itemData);
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
        if (ContextMenu.Instance.TargetSlot != null)
        {
            if (ContextMenu.Instance.TargetSlot is ContainerEquipmentSlot)
            {
                ContainerEquipmentSlot containerEquipmentSlot = ContextMenu.Instance.TargetSlot as ContainerEquipmentSlot;
                InventoryUI.Instance.ShowContainerUI(containerEquipmentSlot.containerInventoryManager, containerEquipmentSlot.ParentSlot().GetItemData().Item);
            }
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

    void SetupButton(string buttonText, UnityEngine.Events.UnityAction action)
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
}
