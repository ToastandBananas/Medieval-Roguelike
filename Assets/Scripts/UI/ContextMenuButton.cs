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
        gameObject.name = $"Open Container";
        buttonText.text = "Open";
        button.onClick.AddListener(OpenContainer);
        gameObject.SetActive(true);
    }

    void OpenContainer()
    {
        if (ContextMenu.Instance.TargetSlot != null)
        {
            if (ContextMenu.Instance.TargetSlot.InventoryItem().myCharacterEquipment != null)
                InventoryUI.Instance.ShowContainerUI(ContextMenu.Instance.TargetSlot.InventoryItem().MyUnit().BackpackInventory(), ContextMenu.Instance.TargetSlot.GetParentSlot().GetItemData().Item());
        }

        ContextMenu.Instance.DisableContextMenu();
    }

    public void Disable()
    {
        button.onClick.RemoveAllListeners();
        gameObject.SetActive(false);
    }
}
