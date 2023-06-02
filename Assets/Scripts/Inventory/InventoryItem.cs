using UnityEngine;
using UnityEngine.UI;

public class InventoryItem : MonoBehaviour
{
    public Inventory myInventory { get; private set; }
    public ItemData itemData { get; private set; }

    [Header("Components")]
    [SerializeField] Image image;
    [SerializeField] RectTransform rectTransform;

    readonly int slotSize = 48;

    public Vector2 GetInventoryItemOffset()
    {
        int width = itemData.Item().width;
        int height = itemData.Item().height;
        return new Vector2((-0.5f * (width - 1)) * rectTransform.rect.width, (0.5f * (height - 1))) * rectTransform.rect.height;
    }

    public Vector2 GetDraggedItemOffset()
    {
        int width = itemData.Item().width;
        int height = itemData.Item().height;

        return new Vector2(((-width * slotSize) / 2) + (slotSize / 2), ((height * slotSize) / 2) - (slotSize / 2));
    }

    public void SetupSprite()
    {
        image.sprite = itemData.Item().inventorySprite;
        rectTransform.offsetMin = new Vector2(-slotSize * (itemData.Item().width - 1), 0);
        rectTransform.offsetMax = new Vector2(0, slotSize * (itemData.Item().height - 1));
        image.enabled = true;
    }

    public void SetupDraggedSprite()
    {
        image.sprite = itemData.Item().inventorySprite;
        rectTransform.sizeDelta = new Vector2(slotSize * itemData.Item().width, slotSize * itemData.Item().height);
        image.enabled = true;
    }

    public void DisableSprite() => image.enabled = false;

    public void SetMyInventory(Inventory inv) => myInventory = inv;

    public void SetItemData(ItemData newItemData) => itemData = newItemData;

    public RectTransform RectTransform() => rectTransform;
}
