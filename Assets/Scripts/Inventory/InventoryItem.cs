using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnitSystem;

namespace InventorySystem
{
    public class InventoryItem : MonoBehaviour
    {
        public Inventory MyInventory { get; private set; }
        public UnitEquipment MyUnitEquipment { get; private set; }
        public ItemData ItemData { get; private set; }

        [Header("Components")]
        [SerializeField] protected Image iconImage;
        [SerializeField] protected Image brokenIconImage;
        [SerializeField] protected RectTransform rectTransform;
        [SerializeField] protected Slot mySlot;
        [SerializeField] protected TextMeshProUGUI stackSizeText;

        public readonly static int slotSize = 60;
        public readonly static float placeholderIconSizeFactor = 5f / 6f;

        public Vector2 GetDraggedItemOffset() => new Vector2(((-ItemData.Item.Width * slotSize) / 2) + (slotSize / 2), ((ItemData.Item.Height * slotSize) / 2) - (slotSize / 2));

        public void SetupIconSprite(bool fullyOpaque)
        {
            ItemData spriteItemData;
            if ((ItemData == null || ItemData.Item == null) && mySlot is EquipmentSlot)
            {
                EquipmentSlot myEquipmentSlot = mySlot as EquipmentSlot;
                if (myEquipmentSlot.IsHeldItemSlot)
                {
                    EquipmentSlot oppositeWeaponSlot = myEquipmentSlot.GetOppositeWeaponSlot();
                    spriteItemData = oppositeWeaponSlot.InventoryItem.ItemData;
                    iconImage.sprite = oppositeWeaponSlot.InventoryItem.ItemData.Item.InventorySprite(spriteItemData);
                }
                else
                    return;
            }
            else
            {
                spriteItemData = ItemData;

                if (mySlot is EquipmentSlot && spriteItemData.Item is Item_Quiver)
                    iconImage.sprite = spriteItemData.Item.Quiver.EquippedSprite;
                else
                    iconImage.sprite = spriteItemData.Item.InventorySprite(spriteItemData);
            }

            // Setup icon size
            if (mySlot is InventorySlot)
            {
                if (MyInventory.InventoryLayout.HasStandardSlotSize())
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

            if (mySlot is EquipmentSlot && mySlot.EquipmentSlot.EquipSlot == EquipSlot.Quiver)
            {
                if (spriteItemData.Item is Item_Quiver)
                    QuiverInventoryItem.IconsParent_RectTransform.sizeDelta = new Vector2(slotSize * spriteItemData.Item.Width, slotSize * (spriteItemData.Item.Height + 1));
                else
                    QuiverInventoryItem.IconsParent_RectTransform.sizeDelta = new Vector2(slotSize * spriteItemData.Item.Width, slotSize * spriteItemData.Item.Height);
            }
            else
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
                if (UnitEquipment.IsHeldItemEquipSlot(myEquipmentSlot.EquipSlot)
                    && ((myEquipmentSlot.UnitEquipment.CurrentWeaponSet == WeaponSet.One && myEquipmentSlot.EquipSlot != EquipSlot.LeftHeldItem1 && myEquipmentSlot.EquipSlot != EquipSlot.RightHeldItem1)
                    || (myEquipmentSlot.UnitEquipment.CurrentWeaponSet == WeaponSet.Two && myEquipmentSlot.EquipSlot != EquipSlot.LeftHeldItem2 && myEquipmentSlot.EquipSlot != EquipSlot.RightHeldItem2)))
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
            if (MyInventory != null && MyInventory.InventoryLayout.PlaceholderSprite != null)
            {
                iconImage.sprite = MyInventory.InventoryLayout.PlaceholderSprite;
                iconImage.rectTransform.sizeDelta = new Vector2(slotSize * MyInventory.InventoryLayout.SlotWidth * placeholderIconSizeFactor, slotSize * MyInventory.InventoryLayout.SlotHeight * placeholderIconSizeFactor);

                // Setup opacity
                Color imageColor = iconImage.color;
                imageColor.a = 0.5f;
                iconImage.color = imageColor;
            }
            else if (MyUnitEquipment != null && mySlot != null)
            {
                mySlot.EquipmentSlot.PlaceholderImage.enabled = true;
                iconImage.enabled = false;
            }
            else
                iconImage.enabled = false;
        }

        public void SetupDraggedSprite()
        {
            iconImage.sprite = ItemData.Item.InventorySprite(ItemData);
            rectTransform.sizeDelta = new Vector2(slotSize * ItemData.Item.Width, slotSize * ItemData.Item.Height);
            iconImage.rectTransform.sizeDelta = new Vector2(slotSize * ItemData.Item.Width, slotSize * ItemData.Item.Height);
            EnableIconImage();
        }

        public void UpdateStackSizeVisuals()
        {
            if (mySlot == null || mySlot is EquipmentSlot)
            {
                if (ItemData.CurrentStackSize == 1)
                    stackSizeText.text = "";
                else
                    stackSizeText.text = ItemData.CurrentStackSize.ToString();
            }
            else if (mySlot is InventorySlot)
            {
                InventorySlot myInventorySlot = mySlot as InventorySlot;
                if (myInventorySlot.ParentSlot() == null || myInventorySlot.ParentSlot().InventoryItem.ItemData == null)
                    return;

                if (myInventorySlot.ParentSlot().InventoryItem.ItemData.CurrentStackSize == 1)
                    myInventorySlot.ParentSlot().InventoryItem.stackSizeText.text = "";
                else
                    myInventorySlot.ParentSlot().InventoryItem.stackSizeText.text = myInventorySlot.ParentSlot().InventoryItem.ItemData.CurrentStackSize.ToString();
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
            if (InventoryUI.DraggedItem == this)
                iconImage.enabled = false;
            else
                ShowPlaceholderIcon();

            stackSizeText.enabled = false;
            if (brokenIconImage != null)
                brokenIconImage.enabled = false;
        }

        public void EnableIconImage()
        {
            iconImage.enabled = true;
            stackSizeText.enabled = true;
            if (brokenIconImage != null && ItemData != null && ItemData.IsBroken)
                brokenIconImage.enabled = true;
        }

        public Unit GetMyUnit()
        {
            if (MyInventory != null)
                return MyInventory.MyUnit;
            else if (MyUnitEquipment != null)
                return MyUnitEquipment.MyUnit;
            return null;
        }

        public void SetMyInventory(Inventory inv) => MyInventory = inv;

        public void SetMyUnitEquipment(UnitEquipment charEquipment) => MyUnitEquipment = charEquipment;

        public void SetItemData(ItemData newItemData) => ItemData = newItemData;

        public RectTransform RectTransform => rectTransform;

        public QuiverInventoryItem QuiverInventoryItem => this as QuiverInventoryItem;
    }
}
