using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ContextMenuButton : MonoBehaviour
{
    [SerializeField] Button button;
    [SerializeField] TextMeshProUGUI buttonText;
    [SerializeField] RectTransform rectTransform;

    public void SetupOpenContainerButton() => SetupButton("Open", OpenContainer);

    void OpenContainer()
    {
        if (ContextMenu.Instance.TargetSlot != null)
        {
            if (ContextMenu.Instance.TargetSlot is ContainerEquipmentSlot)
            {
                ContainerEquipmentSlot containerEquipmentSlot = ContextMenu.Instance.TargetSlot as ContainerEquipmentSlot;
                InventoryUI.Instance.ShowContainerUI(containerEquipmentSlot.containerInventoryManager, containerEquipmentSlot.GetParentSlot().GetItemData().Item);
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
