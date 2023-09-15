using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ContextMenuButton : MonoBehaviour
{
    [SerializeField] Button button;
    [SerializeField] TextMeshProUGUI buttonText;
    [SerializeField] RectTransform rectTransform;

    public void SetupOpenContainerButton()
    {
        gameObject.name = "Open";
        buttonText.text = "Open";
        button.onClick.AddListener(OpenContainer);
        gameObject.SetActive(true);
    }

    void OpenContainer()
    {
        if (ContextMenu.Instance.TargetSlot != null)
        {
            if (ContextMenu.Instance.TargetSlot.InventoryItem.myCharacterEquipment != null)
                InventoryUI.Instance.ShowContainerUI(ContextMenu.Instance.TargetSlot.InventoryItem.GetMyUnit().BackpackInventory(), ContextMenu.Instance.TargetSlot.GetParentSlot().GetItemData().Item);
        }

        ContextMenu.Instance.DisableContextMenu();
    }

    public void SetupDropItemButton()
    {
        gameObject.name = "Drop";
        buttonText.text = "Drop";
        button.onClick.AddListener(DropItem);
        gameObject.SetActive(true);
    }

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

    public void Disable()
    {
        button.onClick.RemoveAllListeners();
        gameObject.SetActive(false);
    }
}
