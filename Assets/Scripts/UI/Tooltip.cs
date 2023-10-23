using UnityEngine.UI;
using UnityEngine;
using System.Text;
using InventorySystem;
using TMPro;
using System.Collections.Generic;

namespace GeneralUI
{
    public class Tooltip : MonoBehaviour
    {
        [SerializeField] RectTransform rectTransform;
        [SerializeField] TextMeshProUGUI textMesh;

        StringBuilder stringBuilder = new StringBuilder();
        StringBuilder currentLine = new StringBuilder();

        readonly int maxCharactersPerLine = 50;
        Vector3 newTooltipPosition;

        public void ShowItemTooltip(Slot slot)
        {
            ItemData itemData = slot.GetItemData();
            if (itemData == null || itemData.Item == null)
                return;

            stringBuilder.Clear();

            // Name
            stringBuilder.Append($"<b><size=20>{itemData.Item.Name}</size></b>\n\n");

            // Description
            stringBuilder.Append("<size=16>");
            SplitText(itemData.Item.Description);
            stringBuilder.Append("</size>");
            // stringBuilder.Append("\n");

            textMesh.text = stringBuilder.ToString();

            gameObject.SetActive(true);

            RecalculateTooltipSize();
            CalculatePosition(slot);
        }

        public void ClearTooltip()
        {
            stringBuilder.Clear();
            gameObject.SetActive(false);
        }

        void CalculatePosition(Slot slot)
        {
            float slotWidth = slot.InventoryItem.RectTransform().rect.width;
            newTooltipPosition = slot.ParentSlot().transform.position;

            if (slot is EquipmentSlot)
            {
                if (slot.transform.position.x <= rectTransform.sizeDelta.x + slotWidth)
                    newTooltipPosition.Set(newTooltipPosition.x + rectTransform.sizeDelta.x + (slotWidth / 4f), newTooltipPosition.y, 0);
                else
                    newTooltipPosition.Set(newTooltipPosition.x - slotWidth + (slotWidth / 2f), newTooltipPosition.y, 0);
            }
            else if (slot is InventorySlot)
            {
                int itemWidth = slot.GetItemData().Item.Width;
                int itemHeight = slot.GetItemData().Item.Height;
                float slotHeight;

                if (slot == slot.ParentSlot())
                {
                    slotWidth = slot.InventoryItem.myInventory.GetSlotFromCoordinate(1, 1).InventoryItem.RectTransform().rect.width;
                    slotHeight = slot.InventoryItem.myInventory.GetSlotFromCoordinate(1, 1).InventoryItem.RectTransform().rect.height;
                }
                else
                    slotHeight = slot.InventoryItem.RectTransform().rect.height;

                if (slot.ParentSlot().transform.position.x <= rectTransform.sizeDelta.x + slotWidth)
                    newTooltipPosition.Set(newTooltipPosition.x + rectTransform.sizeDelta.x + (slotWidth / 4f), newTooltipPosition.y + (itemHeight * (slotHeight / 2f)) - (slotHeight / 2f), 0);
                else
                    newTooltipPosition.Set(newTooltipPosition.x - (itemWidth * slotWidth) + (slotWidth / 2f), newTooltipPosition.y + (itemHeight * (slotHeight / 2f)) - (slotHeight / 2f), 0);
            }

            rectTransform.position = newTooltipPosition;
        }

        void SplitText(string originalText)
        {
            currentLine.Clear();
            foreach (string word in originalText.Split(' '))
            {
                if (currentLine.Length + word.Length <= maxCharactersPerLine)
                {
                    currentLine.Append($"{word} ");
                }
                else
                {
                    stringBuilder.AppendLine(currentLine.ToString().TrimEnd());
                    currentLine.Clear().Append($"{word} ");
                }
            }

            // Add the remaining text (if any) as the last line.
            if (currentLine.Length > 0)
                stringBuilder.AppendLine(currentLine.ToString().TrimEnd());
        }

        void RecalculateTooltipSize()
        {
            LayoutRebuilder.ForceRebuildLayoutImmediate(textMesh.rectTransform);
            rectTransform.sizeDelta = textMesh.rectTransform.sizeDelta;
            LayoutRebuilder.ForceRebuildLayoutImmediate(rectTransform);
        }
    }
}
