using UnityEngine.UI;
using UnityEngine;
using System.Text;
using InventorySystem;
using TMPro;
using System;
using UnitSystem;

namespace GeneralUI
{
    public class Tooltip : MonoBehaviour
    {
        [SerializeField] RectTransform rectTransform;
        [SerializeField] TextMeshProUGUI textMesh;

        StringBuilder tooltipStringBuilder = new StringBuilder();
        StringBuilder stringBuilder = new StringBuilder();

        readonly int maxCharactersPerLine_Title = 20;
        readonly int maxCharactersPerLine = 44;
        readonly int defaultSlotSize = 60;

        Vector3 newTooltipPosition;

        public void ShowItemTooltip(Slot slot)
        {
            ItemData itemData = slot.GetItemData();
            if (itemData == null || itemData.Item == null)
                return;

            tooltipStringBuilder.Clear();

            // Name
            tooltipStringBuilder.Append("<align=center><b><size=22>");
            SplitText(itemData.Name(), maxCharactersPerLine_Title);
            tooltipStringBuilder.Append("</size></b></align>");

            // If Equipped
            if (this == TooltipManager.Tooltips[1] || this == TooltipManager.Tooltips[2] || (slot is EquipmentSlot && UnitManager.player.UnitEquipment.slots.Contains((EquipmentSlot)slot)))
                tooltipStringBuilder.Append("<align=center><i><b><size=19>- Equipped -</size></b></i></align>\n\n");
            else
                tooltipStringBuilder.Append("\n");
            // tooltipStringBuilder.Append($"- {EnumToSpacedString(itemData.Item.ItemType)} -</align>\n\n");

            // Description
            tooltipStringBuilder.Append("<size=16>");
            SplitText(itemData.Item.Description, maxCharactersPerLine);
            stringBuilder.Append("</size>");
            tooltipStringBuilder.Append("\n");

            if (itemData.Item.MaxUses > 1)
                tooltipStringBuilder.Append($"<i>Remaining Uses: {itemData.RemainingUses} / {itemData.Item.MaxUses}</i>\n\n");
            else if (itemData.Item.MaxStackSize > 1)
                tooltipStringBuilder.Append($"<i>{itemData.CurrentStackSize} / {itemData.Item.MaxStackSize}</i>\n\n");

            if (itemData.Item is Weapon)
            {
                tooltipStringBuilder.Append($"Damage: {itemData.Damage}\n");
            }
            else if (itemData.Item is Shield)
            {
                tooltipStringBuilder.Append($"Block Power: {itemData.BlockPower}\n");
                tooltipStringBuilder.Append($"Bash Damage: {itemData.Damage}\n");
            }
            else if (itemData.Item is Armor)
            {
                tooltipStringBuilder.Append($"Armor: {itemData.Defense}\n");
            }
            else if (itemData.Item is Backpack)
            {
                if (itemData.Item.Backpack.InventorySections.Length > 1)
                    tooltipStringBuilder.Append($"<i>+{itemData.Item.Backpack.InventorySections.Length - 1} additional pockets</i>\n");
            }
            else if (itemData.Item is Quiver)
            {
                tooltipStringBuilder.Append($"<i>{itemData.Item.Quiver.InventorySections[0].AmountOfSlots} {EnumToSpacedString(itemData.Item.Quiver.InventorySections[0].AllowedItemTypes[0])} slots</i>\n");
            }

            tooltipStringBuilder.Append($"\nValue: {itemData.Value}");

            textMesh.text = tooltipStringBuilder.ToString();
            gameObject.SetActive(true);

            RecalculateTooltipSize();
            CalculatePosition(slot);
        }

        public void ClearTooltip()
        {
            tooltipStringBuilder.Clear();
            gameObject.SetActive(false);
        }

        void CalculatePosition(Slot slot)
        {
            if (this == TooltipManager.Tooltips[0])
            {
                float slotWidth = slot.InventoryItem.RectTransform.rect.width;
                newTooltipPosition = slot.ParentSlot().transform.position;

                if (slot is EquipmentSlot)
                {
                    // Determine x position
                    if (newTooltipPosition.x <= rectTransform.sizeDelta.x + slotWidth) // Too far left
                        newTooltipPosition.Set(newTooltipPosition.x + rectTransform.sizeDelta.x + (slotWidth / 4f), newTooltipPosition.y, 0);
                    else
                        newTooltipPosition.Set(newTooltipPosition.x - slotWidth + (slotWidth / 2f), newTooltipPosition.y, 0);

                    // Determine y position
                    if (newTooltipPosition.y >= Screen.height - (rectTransform.sizeDelta.y / 2f)) // Too close to the top
                        newTooltipPosition.Set(newTooltipPosition.x, Screen.height - (rectTransform.sizeDelta.y / 2f) - defaultSlotSize, 0);
                }
                else if (slot is InventorySlot)
                {
                    int itemWidth = slot.GetItemData().Item.Width;
                    int itemHeight = slot.GetItemData().Item.Height;
                    float slotHeight;

                    if (slot == slot.ParentSlot())
                    {
                        slotWidth = slot.InventoryItem.myInventory.GetSlotFromCoordinate(1, 1).InventoryItem.RectTransform.rect.width;
                        slotHeight = slot.InventoryItem.myInventory.GetSlotFromCoordinate(1, 1).InventoryItem.RectTransform.rect.height;
                    }
                    else
                        slotHeight = slot.InventoryItem.RectTransform.rect.height;

                    // Determine x position
                    if (newTooltipPosition.x <= rectTransform.sizeDelta.x + slotWidth) // Too far left
                        newTooltipPosition.Set(newTooltipPosition.x + rectTransform.sizeDelta.x + (slotWidth / 4f), newTooltipPosition.y, 0);
                    else
                        newTooltipPosition.Set(newTooltipPosition.x - (itemWidth * slotWidth) + (slotWidth / 2f), newTooltipPosition.y, 0);

                    // Determine y position
                    if (newTooltipPosition.y >= Screen.height - (rectTransform.sizeDelta.y / 2f)) // Too close to the top
                        newTooltipPosition.Set(newTooltipPosition.x, Screen.height - (rectTransform.sizeDelta.y / 2f) - defaultSlotSize, 0);
                    else if (Mathf.RoundToInt(slotHeight) == defaultSlotSize) // Abnormal slot size (i.e. arrow slot)
                        newTooltipPosition.Set(newTooltipPosition.x, newTooltipPosition.y + (itemHeight * (slotHeight / 2f)) - (slotHeight / 2f), 0);

                    if (newTooltipPosition.y <= rectTransform.sizeDelta.y / 2f) // Too close to the bottom
                        newTooltipPosition.Set(newTooltipPosition.x, (rectTransform.sizeDelta.y / 2f) + defaultSlotSize, 0);
                }

                rectTransform.position = newTooltipPosition;
            }
            else if (this == TooltipManager.Tooltips[1]) // First weapon tooltip (can be either left or right hand weapon depending on how many are equipped)
            {
                EquipSlot equipSlot = slot.GetItemData().Item.Equipment.EquipSlot;
                EquipmentSlot equipmentSlot = null;
                if (UnitEquipment.IsHeldItemEquipSlot(equipSlot))
                {
                    if (UnitManager.player.UnitEquipment.currentWeaponSet == WeaponSet.One)
                    {
                        if (UnitManager.player.UnitEquipment.EquipSlotHasItem(EquipSlot.LeftHeldItem1))
                            equipmentSlot = UnitManager.player.UnitEquipment.GetEquipmentSlot(EquipSlot.LeftHeldItem1);
                        else if (UnitManager.player.UnitEquipment.EquipSlotHasItem(EquipSlot.RightHeldItem1))
                            equipmentSlot = UnitManager.player.UnitEquipment.GetEquipmentSlot(EquipSlot.RightHeldItem1);
                    }
                    else
                    {
                        if (UnitManager.player.UnitEquipment.EquipSlotHasItem(EquipSlot.LeftHeldItem2))
                            equipmentSlot = UnitManager.player.UnitEquipment.GetEquipmentSlot(EquipSlot.LeftHeldItem2);
                        else if (UnitManager.player.UnitEquipment.EquipSlotHasItem(EquipSlot.RightHeldItem2))
                            equipmentSlot = UnitManager.player.UnitEquipment.GetEquipmentSlot(EquipSlot.RightHeldItem2);
                    }
                }
                else
                {
                    if (UnitManager.player.UnitEquipment.EquipSlotHasItem(equipSlot))
                        equipmentSlot = UnitManager.player.UnitEquipment.GetEquipmentSlot(equipSlot);
                }

                if (equipmentSlot != null)
                {
                    newTooltipPosition = equipmentSlot.transform.position;
                    newTooltipPosition.Set(newTooltipPosition.x - equipmentSlot.InventoryItem.RectTransform.rect.width + (equipmentSlot.InventoryItem.RectTransform.rect.width / 2f), newTooltipPosition.y, 0);

                    // Determine y position
                    if (newTooltipPosition.y >= Screen.height - (rectTransform.sizeDelta.y / 2f)) // Too close to the top
                        newTooltipPosition.Set(newTooltipPosition.x, Screen.height - (rectTransform.sizeDelta.y / 2f) - defaultSlotSize, 0);

                    rectTransform.position = newTooltipPosition;
                }
            }
            else // Second weapon tooltip (always right hand weapon)
            {
                EquipmentSlot equipmentSlot = null;
                if (UnitManager.player.UnitEquipment.currentWeaponSet == WeaponSet.One)
                {
                    if (UnitManager.player.UnitEquipment.EquipSlotHasItem(EquipSlot.RightHeldItem1))
                        equipmentSlot = UnitManager.player.UnitEquipment.GetEquipmentSlot(EquipSlot.RightHeldItem1);
                }
                else
                {
                    if (UnitManager.player.UnitEquipment.EquipSlotHasItem(EquipSlot.RightHeldItem2))
                        equipmentSlot = UnitManager.player.UnitEquipment.GetEquipmentSlot(EquipSlot.RightHeldItem2);
                }

                if (equipmentSlot != null)
                {
                    newTooltipPosition = equipmentSlot.transform.position;
                    newTooltipPosition.Set(newTooltipPosition.x - equipmentSlot.InventoryItem.RectTransform.rect.width + (equipmentSlot.InventoryItem.RectTransform.rect.width / 2f), newTooltipPosition.y, 0);

                    // Determine y position
                    if (newTooltipPosition.y >= Screen.height - (rectTransform.sizeDelta.y / 2f)) // Too close to the top
                        newTooltipPosition.Set(newTooltipPosition.x, Screen.height - (rectTransform.sizeDelta.y / 2f) - defaultSlotSize, 0);

                    rectTransform.position = newTooltipPosition;
                }
            }
        }

        string EnumToSpacedString(Enum enumValue)
        {
            stringBuilder.Clear();
            string enumString = enumValue.ToString();

            stringBuilder.Append(enumString[0]); // Append the first character
            for (int i = 1; i < enumString.Length; i++)
            {
                char currentChar = enumString[i];

                if (char.IsUpper(currentChar) && char.IsLower(enumString[i - 1]))
                {
                    // Insert a space before a capital letter that follows a lowercase letter
                    stringBuilder.Append(' ');
                }

                stringBuilder.Append(currentChar);
            }

            return stringBuilder.ToString();
        }

        void SplitText(string originalText, int maxCharsPerLine)
        {
            stringBuilder.Clear();
            foreach (string word in originalText.Split(' '))
            {
                if (stringBuilder.Length + word.Length <= maxCharsPerLine)
                {
                    stringBuilder.Append($"{word} ");
                }
                else
                {
                    tooltipStringBuilder.AppendLine(stringBuilder.ToString().TrimEnd());
                    stringBuilder.Clear().Append($"{word} ");
                }
            }

            // Add the remaining text (if any) as the last line.
            if (stringBuilder.Length > 0)
                tooltipStringBuilder.AppendLine($"{stringBuilder.ToString().TrimEnd()}");
        }

        void RecalculateTooltipSize()
        {
            LayoutRebuilder.ForceRebuildLayoutImmediate(textMesh.rectTransform);
            rectTransform.sizeDelta = textMesh.rectTransform.sizeDelta;
            LayoutRebuilder.ForceRebuildLayoutImmediate(rectTransform);
        }
    }
}
