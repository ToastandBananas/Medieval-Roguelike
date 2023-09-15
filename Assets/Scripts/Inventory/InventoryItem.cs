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

    public Vector2 GetInventoryItemOffset()
    {
        int width = itemData.Item.width;
        int height = itemData.Item.height;
        return new Vector2((-0.5f * (width - 1)) * rectTransform.rect.width, (0.5f * (height - 1))) * rectTransform.rect.height;
    }

    public Vector2 GetDraggedItemOffset()
    {
        int width = itemData.Item.width;
        int height = itemData.Item.height;

        return new Vector2(((-width * slotSize) / 2) + (slotSize / 2), ((height * slotSize) / 2) - (slotSize / 2));
    }

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
                iconImage.sprite = oppositeWeaponSlot.InventoryItem.itemData.Item.inventorySprite;
            }
            else
                return;
        }
        else
        {
            spriteItemData = itemData;
            iconImage.sprite = spriteItemData.Item.inventorySprite;
        }

        if (mySlot is InventorySlot)
        {
            rectTransform.offsetMin = new Vector2(-slotSize * (spriteItemData.Item.width - 1), 0);
            rectTransform.offsetMax = new Vector2(0, slotSize * (spriteItemData.Item.height - 1));
        }
        else
            rectTransform.sizeDelta = new Vector2(slotSize * spriteItemData.Item.width, slotSize * spriteItemData.Item.height);

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
                EnableIconImage();
        }
        else
            EnableIconImage();
    }

    public void SetupDraggedSprite()
    {
        iconImage.sprite = itemData.Item.inventorySprite;
        rectTransform.sizeDelta = new Vector2(slotSize * itemData.Item.width, slotSize * itemData.Item.height);
        iconImage.enabled = true;
    }

    public void UpdateStackSizeText()
    {
        if (mySlot == null)
        {
            if (itemData.CurrentStackSize() == 1)
                stackSizeText.text = "";
            else
                stackSizeText.text = itemData.CurrentStackSize().ToString();
        }
        else
        {
            if (mySlot is InventorySlot)
            {
                InventorySlot myInventorySlot = mySlot as InventorySlot;
                if (myInventorySlot.GetParentSlot() == null)
                    return;

                if (myInventorySlot.GetParentSlot().InventoryItem.itemData.CurrentStackSize() == 1)
                    myInventorySlot.GetParentSlot().InventoryItem.stackSizeText.text = "";
                else
                    myInventorySlot.GetParentSlot().InventoryItem.stackSizeText.text = myInventorySlot.GetParentSlot().InventoryItem.itemData.CurrentStackSize().ToString();
            }
            else
            {
                EquipmentSlot myEquipmentSlot = mySlot as EquipmentSlot;
                if (myEquipmentSlot.InventoryItem.itemData.CurrentStackSize() == 1)
                    myEquipmentSlot.InventoryItem.stackSizeText.text = "";
                else
                    myEquipmentSlot.InventoryItem.stackSizeText.text = myEquipmentSlot.InventoryItem.itemData.CurrentStackSize().ToString();
            }
        }
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
                if (myInventorySlot.GetParentSlot() == null)
                    return;

                myInventorySlot.GetParentSlot().InventoryItem.stackSizeText.text = "";
            }
            else
            {
                EquipmentSlot myEquipmentSlot = mySlot as EquipmentSlot;
                myEquipmentSlot.InventoryItem.stackSizeText.text = "";
            }
        }
    }

    public void RemoveIconSprite() => iconImage.sprite = null;

    public void DisableIconImage()
    {
        iconImage.enabled = false;
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
