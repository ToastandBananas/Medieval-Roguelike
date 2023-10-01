using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class InventoryItem : MonoBehaviour
{
    public Inventory myInventory { get; private set; }
    public CharacterEquipment myCharacterEquipment { get; private set; }
    public ItemData itemData { get; private set; }

    [Header("Components")]
    [SerializeField] protected Image iconImage;
    [SerializeField] protected RectTransform rectTransform;
    [SerializeField] protected Slot mySlot;
    [SerializeField] protected TextMeshProUGUI stackSizeText;

    public readonly static int slotSize = 60;
    public readonly static float placeholderIconSizeFactor = 5f / 6f;

    public Vector2 GetDraggedItemOffset() => new Vector2(((-itemData.Item.Width * slotSize) / 2) + (slotSize / 2), ((itemData.Item.Height * slotSize) / 2) - (slotSize / 2));

    public void SetupIconSprite(bool fullyOpaque)
    {
        ItemData spriteItemData;
        if ((itemData == null || itemData.Item == null) && mySlot is EquipmentSlot)
        {
            EquipmentSlot myEquipmentSlot = mySlot as EquipmentSlot;
            if (myEquipmentSlot.IsHeldItemSlot())
            {
                EquipmentSlot oppositeWeaponSlot = myEquipmentSlot.GetOppositeWeaponSlot();
                spriteItemData = oppositeWeaponSlot.InventoryItem.itemData;
                iconImage.sprite = oppositeWeaponSlot.InventoryItem.itemData.Item.InventorySprite(spriteItemData);
            }
            else
                return;
        }
        else
        {
            spriteItemData = itemData;
            iconImage.sprite = spriteItemData.Item.InventorySprite(spriteItemData);
        }

        // Setup icon size
        if (mySlot is InventorySlot)
        {
            if (myInventory.InventoryLayout.HasStandardSlotSize())
            {
                rectTransform.offsetMin = new Vector2(-slotSize * (spriteItemData.Item.Width - 1), 0);
                rectTransform.offsetMax = new Vector2(0, slotSize * (spriteItemData.Item.Height - 1));
            }
            else
            {
                rectTransform.offsetMin = Vector2.zero;
                rectTransform.offsetMax = Vector2.zero;
            }
        }

        iconImage.rectTransform.sizeDelta = new Vector2(slotSize * spriteItemData.Item.Width, slotSize * spriteItemData.Item.Height);

        Color imageColor = iconImage.color;
        if (fullyOpaque)
            imageColor.a = 1f;
        else
            imageColor.a = 0.3f;

        iconImage.color = imageColor;

        if (mySlot is EquipmentSlot)
        {
            EquipmentSlot myEquipmentSlot = (EquipmentSlot)mySlot;
            if (myEquipmentSlot.CharacterEquipment.IsHeldItemEquipSlot(myEquipmentSlot.EquipSlot)
                && (myEquipmentSlot.CharacterEquipment.currentWeaponSet == WeaponSet.One && myEquipmentSlot.EquipSlot != EquipSlot.LeftHeldItem1 && myEquipmentSlot.EquipSlot != EquipSlot.RightHeldItem1)
                || (myEquipmentSlot.CharacterEquipment.currentWeaponSet == WeaponSet.Two && myEquipmentSlot.EquipSlot != EquipSlot.LeftHeldItem2 && myEquipmentSlot.EquipSlot != EquipSlot.RightHeldItem2))
            {
                DisableIconImage();
            }
            else
            {
                myEquipmentSlot.PlaceholderImage.enabled = false;
                EnableIconImage();
            }
        }
        else
            EnableIconImage();
    }

    public void ShowPlaceholderIcon()
    {
        if (myInventory != null && myInventory.InventoryLayout.PlaceholderSprite != null)
        {
            iconImage.sprite = myInventory.InventoryLayout.PlaceholderSprite;
            iconImage.rectTransform.sizeDelta = new Vector2(slotSize * myInventory.InventoryLayout.SlotWidth * placeholderIconSizeFactor, slotSize * myInventory.InventoryLayout.SlotHeight * placeholderIconSizeFactor);

            // Setup opacity
            Color imageColor = iconImage.color;
            imageColor.a = 0.5f;
            iconImage.color = imageColor;
        }
        else if (myCharacterEquipment != null && mySlot != null)
        {
            EquipmentSlot equipmentSlot = mySlot as EquipmentSlot;
            equipmentSlot.PlaceholderImage.enabled = true;
            iconImage.enabled = false;
        }
        else
            iconImage.enabled = false;
    }

    public void SetupDraggedSprite()
    {
        iconImage.sprite = itemData.Item.InventorySprite(itemData);
        rectTransform.sizeDelta = new Vector2(slotSize * itemData.Item.Width, slotSize * itemData.Item.Height);
        iconImage.rectTransform.sizeDelta = new Vector2(slotSize * itemData.Item.Width, slotSize * itemData.Item.Height);
        EnableIconImage();
    }

    public void UpdateStackSizeVisuals()
    {
        if (mySlot == null || mySlot is EquipmentSlot)
        {
            if (itemData.CurrentStackSize == 1)
                stackSizeText.text = "";
            else
                stackSizeText.text = itemData.CurrentStackSize.ToString();
        }
        else if (mySlot is InventorySlot)
        {
            InventorySlot myInventorySlot = mySlot as InventorySlot;
            if (myInventorySlot.ParentSlot() == null || myInventorySlot.ParentSlot().InventoryItem.itemData == null)
                return;

            if (myInventorySlot.ParentSlot().InventoryItem.itemData.CurrentStackSize == 1)
                myInventorySlot.ParentSlot().InventoryItem.stackSizeText.text = "";
            else
                myInventorySlot.ParentSlot().InventoryItem.stackSizeText.text = myInventorySlot.ParentSlot().InventoryItem.itemData.CurrentStackSize.ToString();
        }

        SetupIconSprite(true);
    }

    public void ClearStackSizeText()
    {
        if (mySlot == null)
            stackSizeText.text = "";
        else
        {
            if (mySlot is InventorySlot)
            {
                InventorySlot myInventorySlot = mySlot as InventorySlot;
                if (myInventorySlot.ParentSlot() == null)
                    return;

                myInventorySlot.ParentSlot().InventoryItem.stackSizeText.text = "";
            }
            else
            {
                EquipmentSlot myEquipmentSlot = mySlot as EquipmentSlot;
                myEquipmentSlot.InventoryItem.stackSizeText.text = "";
            }
        }
    }

    public void DisableIconImage()
    {
        if (InventoryUI.Instance.DraggedItem == this)
            iconImage.enabled = false;
        else
            ShowPlaceholderIcon();

        stackSizeText.enabled = false;
    }

    public void EnableIconImage()
    {
        iconImage.enabled = true;
        stackSizeText.enabled = true;
    }

    public Unit GetMyUnit()
    {
        if (myInventory != null)
            return myInventory.MyUnit;
        else if (myCharacterEquipment != null)
            return myCharacterEquipment.MyUnit;
        return null;
    }

    public void SetMyInventory(Inventory inv) => myInventory = inv;

    public void SetMyCharacterEquipment(CharacterEquipment charEquipment) => myCharacterEquipment = charEquipment;

    public void SetItemData(ItemData newItemData) => itemData = newItemData;

    public RectTransform RectTransform() => rectTransform;
}
