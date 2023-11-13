using UnityEngine.UI;
using UnityEngine;
using System.Text;
using InventorySystem;
using TMPro;
using System;
using UnitSystem;
using ActionSystem;
using System.Collections;
using InteractableObjects;
using Utilities;
using GridSystem;

namespace GeneralUI
{
    public class Tooltip : MonoBehaviour
    {
        [SerializeField] RectTransform rectTransform;
        [SerializeField] TextMeshProUGUI textMesh;
        [SerializeField] Image image;
        [SerializeField] Button button;

        StringBuilder stringBuilder = new StringBuilder();
        StringBuilder secondaryStringBuilder = new StringBuilder();

        readonly int maxCharactersPerLine_Title = 20;
        readonly int maxCharactersPerLine = 44;
        readonly int defaultSlotSize = 60;

        Vector3 newTooltipPosition;
        Transform targetTransform;

        public void ShowInventoryTooltip(Slot slot)
        {
            ItemData itemData = slot.GetItemData();
            if (itemData == null || itemData.Item == null)
                return;

            stringBuilder.Clear();

            // Name
            stringBuilder.Append("<align=center><b><size=22>");
            SplitText(itemData.Name(), maxCharactersPerLine_Title);
            stringBuilder.Append("</size></b></align>");
            if (itemData.Item is Weapon)
            {
                stringBuilder.Append("<align=center><i><size=18>");
                if (itemData.Item is MeleeWeapon)
                {
                    if (itemData.Item.MeleeWeapon.IsTwoHanded)
                        stringBuilder.Append("Two-Handed ");
                    else if (itemData.Item.MeleeWeapon.IsVersatile)
                        stringBuilder.Append("Versatile ");
                }

                stringBuilder.Append($"{EnumToSpacedString(itemData.Item.Weapon.WeaponType)}</size></i></align>\n");
            }

            // If Equipped
            if (this == TooltipManager.WorldTooltips[1] || this == TooltipManager.WorldTooltips[2] || (slot is EquipmentSlot && UnitManager.player.UnitEquipment.slots.Contains((EquipmentSlot)slot)))
                stringBuilder.Append("<align=center><i><b><size=18>- Equipped -</size></b></i></align>\n\n");
            else
                stringBuilder.Append("\n");

            // Description
            stringBuilder.Append("<size=16>");
            SplitText(itemData.Item.Description, maxCharactersPerLine);
            stringBuilder.Append("</size>");

            if (itemData.Item.MaxUses > 1)
                stringBuilder.Append($"\n  <i>Remaining Uses: {itemData.RemainingUses} / {itemData.Item.MaxUses}</i>\n");
            else if (itemData.Item.MaxStackSize > 1)
                stringBuilder.Append($"\n  <i>{itemData.CurrentStackSize} / {itemData.Item.MaxStackSize}</i>\n");

            if (itemData.Item is Weapon)
            {
                stringBuilder.Append($"\n  Damage: {itemData.Damage}");

                if (itemData.AccuracyModifier != 0f)
                {
                    if (itemData.AccuracyModifier < 0f)
                        stringBuilder.Append($"\n  Accuracy: -{itemData.AccuracyModifier * 100f}%");
                    else
                        stringBuilder.Append($"\n  Accuracy: +{itemData.AccuracyModifier * 100f}%");
                }

                if (itemData.BlockChanceAddOn != 0f)
                {
                    if (itemData.BlockChanceAddOn < 0f)
                        stringBuilder.Append($"\n  Block Chance: -{itemData.BlockChanceAddOn * 100f}%");
                    else
                        stringBuilder.Append($"\n  Block Chance: +{itemData.BlockChanceAddOn * 100f}%");
                }
                stringBuilder.Append("\n");
            }
            else if (itemData.Item is Shield)
            {
                if (itemData.BlockPower < 0)
                    stringBuilder.Append($"\n  Block Power: -{itemData.BlockPower}");
                else
                    stringBuilder.Append($"\n  Block Power: +{itemData.BlockPower}");

                if (itemData.BlockChanceAddOn != 0f)
                {
                    if (itemData.BlockChanceAddOn < 0f)
                        stringBuilder.Append($"\n  Block Chance: -{itemData.BlockChanceAddOn * 100f}%");
                    else
                        stringBuilder.Append($"\n  Block Chance: +{itemData.BlockChanceAddOn * 100f}%");
                }

                stringBuilder.Append($"\n  Bash Damage: {itemData.Damage}");
                stringBuilder.Append("\n");
            }
            else if (itemData.Item is Armor)
            {
                if (itemData.Defense != 0)
                    stringBuilder.Append($"\n  Armor: {itemData.Defense}");
                stringBuilder.Append("\n");
            }
            else if (itemData.Item is Backpack)
            {
                if (itemData.Item.Backpack.InventorySections.Length > 1)
                {
                    stringBuilder.Append($"\n  <i>+{itemData.Item.Backpack.InventorySections.Length - 1} additional pockets</i>");
                    stringBuilder.Append("\n");
                }
            }
            else if (itemData.Item is Quiver)
            {
                stringBuilder.Append($"\n  <i>+{itemData.Item.Quiver.InventorySections[0].AmountOfSlots} {EnumToSpacedString(itemData.Item.Quiver.InventorySections[0].AllowedItemTypes[0])} slots</i>");
                stringBuilder.Append("\n");
            }
            else if (itemData.Item is Belt)
            {
                // Added Belt Attachments
                Belt belt = itemData.Item as Belt;
                if (belt.BeltPouchNames.Length > 0)
                {
                    for (int i = 0; i < belt.BeltPouchNames.Length; i++)
                    {
                        if (belt.BeltPouchNames[i] != "")
                            stringBuilder.Append($"\n  <i>+ {belt.BeltPouchNames[i]}</i>");
                        else
                            Debug.LogWarning($"{belt.Name} has an empty Belt Pouch Name at index {i}");
                    }

                    stringBuilder.Append("\n");
                }
            }

            stringBuilder.Append($"\n<size=16>Weight: {itemData.Weight()} lbs</size>");
            stringBuilder.Append($"\n<size=16>Value: {itemData.Value} g ({Mathf.RoundToInt(itemData.Value / itemData.Weight() * 100f) / 100f} g/lb)</size>");

            textMesh.text = stringBuilder.ToString();
            gameObject.SetActive(true);

            RecalculateTooltipSize();
            CalculatePosition(slot);
        }

        public void ShowActionTooltip(ActionBarSlot actionBarSlot)
        {
            stringBuilder.Clear();
            if (actionBarSlot is ItemActionBarSlot)
            {
                ItemActionBarSlot itemActionBarSlot = actionBarSlot as ItemActionBarSlot;
                if (itemActionBarSlot.itemData == null || itemActionBarSlot.itemData.Item == null)
                    return;

                stringBuilder.Append($"<align=center><size=22><b>{itemActionBarSlot.itemData.Item.Name}</b></size></align>");
            }
            else
            {
                stringBuilder.Append($"<align=center><size=22><b>{actionBarSlot.actionType.ActionName}</b></size></align>\n\n");
                SplitText(actionBarSlot.actionType.GetAction(UnitManager.player).TooltipDescription(), maxCharactersPerLine);
            }

            textMesh.text = stringBuilder.ToString();
            gameObject.SetActive(true);

            RecalculateTooltipSize();
            CalculatePosition(actionBarSlot);
        }

        public void ShowLooseItemTooltip(LooseItem looseItem, ItemData looseItemData, bool clickable)
        {
            if (looseItem == null || looseItemData == null || looseItemData.Item == null)
                ClearTooltip();

            stringBuilder.Clear();
            stringBuilder.Append($"<align=center><size=16><b>{looseItemData.Item.Name}");
            if (looseItemData.CurrentStackSize > 1)
                stringBuilder.Append($" x {looseItemData.CurrentStackSize}");
            stringBuilder.Append("</b></size></align>");

            textMesh.text = stringBuilder.ToString();
            if (clickable)
            {
                button.onClick.AddListener(delegate { InteractWithLooseItem_OnClick(looseItem); });
                button.interactable = true;
                image.raycastTarget = true;
            }
            else
            {
                button.interactable = false;
                image.raycastTarget = false;
            }

            gameObject.SetActive(true);

            RecalculateTooltipSize();

            targetTransform = looseItem.transform;
            StartCoroutine(CalculatePosition(targetTransform));
        }

        public void ShowUnitTooltip(Unit targetUnit, BaseAction selectedAction)
        {
            if (selectedAction is BaseAttackAction)
            {
                Debug.Log($"Show Unit Tooltip for {targetUnit.name} | Hit Chance: {UnitManager.player.stats.HitChance(targetUnit, selectedAction as BaseAttackAction)}");
                stringBuilder.Clear();
                stringBuilder.Append($"{UnitManager.player.stats.HitChance(targetUnit, selectedAction as BaseAttackAction)}");
            }
        }

        public void ClearTooltip()
        {
            stringBuilder.Clear();
            button.onClick.RemoveAllListeners();
            gameObject.SetActive(false);
        }

        void InteractWithLooseItem_OnClick(LooseItem looseItem)
        {
            if (TacticsPathfindingUtilities.CalculateWorldSpaceDistance_XYZ(UnitManager.player.GridPosition, looseItem.GridPosition()) > TacticsPathfindingUtilities.diaganolDistance)
            {
                UnitManager.player.unitActionHandler.GetAction<InteractAction>().SetTargetInteractable(looseItem);
                UnitManager.player.unitActionHandler.moveAction.QueueAction(LevelGrid.GetNearestSurroundingGridPosition(looseItem.GridPosition(), UnitManager.player.GridPosition, TacticsPathfindingUtilities.diaganolDistance, looseItem.CanInteractAtMyGridPosition()));
            }
            else
                UnitManager.player.unitActionHandler.GetAction<InteractAction>().QueueAction(looseItem);
        }

        void CalculatePosition(Slot slot)
        {
            if (TooltipManager.activeInventoryTooltips == 0)
            {
                TooltipManager.AddToActiveInventoryTooltips();

                float slotWidth = slot.InventoryItem.RectTransform.rect.width;
                newTooltipPosition = slot.ParentSlot().transform.position;

                if (slot is EquipmentSlot)
                {
                    // Determine x position
                    if (newTooltipPosition.x <= rectTransform.rect.width + slotWidth) // Too far left
                        newTooltipPosition.Set(newTooltipPosition.x + ((rectTransform.rect.width + slotWidth) / 2f), newTooltipPosition.y, 0);
                    else
                        newTooltipPosition.Set(newTooltipPosition.x - ((rectTransform.rect.width + slotWidth) / 2f), newTooltipPosition.y, 0);

                    // Determine y position
                    if (newTooltipPosition.y >= Screen.height - (rectTransform.rect.height / 2f)) // Too close to the top
                        newTooltipPosition.Set(newTooltipPosition.x, Screen.height - (rectTransform.rect.height / 2f) - defaultSlotSize, 0);
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
                    if (newTooltipPosition.x <= rectTransform.rect.width + slotWidth) // Too far left
                        newTooltipPosition.Set(newTooltipPosition.x + ((rectTransform.rect.width + slotWidth) / 2f), newTooltipPosition.y, 0);
                    else
                        newTooltipPosition.Set(newTooltipPosition.x - (rectTransform.rect.width / 2f) - (itemWidth * slotWidth) + (slotWidth / 2f), newTooltipPosition.y, 0);

                    // Determine y position
                    if (newTooltipPosition.y >= Screen.height - (rectTransform.rect.height / 2f)) // Too close to the top
                        newTooltipPosition.Set(newTooltipPosition.x, Screen.height - (rectTransform.rect.height / 2f) - defaultSlotSize, 0);
                    else if (Mathf.RoundToInt(slotHeight) == defaultSlotSize) // Abnormal slot size (i.e. arrow slot)
                        newTooltipPosition.Set(newTooltipPosition.x, newTooltipPosition.y + (itemHeight * (slotHeight / 2f)) - (slotHeight / 2f), 0);

                    if (newTooltipPosition.y <= rectTransform.rect.height / 2f) // Too close to the bottom
                        newTooltipPosition.Set(newTooltipPosition.x, (rectTransform.rect.height / 2f) + defaultSlotSize, 0);
                }

                rectTransform.position = newTooltipPosition;
            }
            else if (TooltipManager.activeInventoryTooltips == 1) // First weapon tooltip (can be either left or right hand weapon depending on how many are equipped)
            {
                TooltipManager.AddToActiveInventoryTooltips();

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
                    newTooltipPosition.Set(newTooltipPosition.x - ((rectTransform.rect.width + equipmentSlot.InventoryItem.RectTransform.rect.width) / 2f), newTooltipPosition.y, 0);

                    // Determine y position
                    if (newTooltipPosition.y >= Screen.height - (rectTransform.rect.height / 2f)) // Too close to the top
                        newTooltipPosition.Set(newTooltipPosition.x, Screen.height - (rectTransform.rect.height / 2f) - defaultSlotSize, 0);

                    rectTransform.position = newTooltipPosition;
                }
            }
            else // Second weapon tooltip (always right hand weapon)
            {
                // TooltipManager.AddToActiveInventoryTooltips();

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
                    newTooltipPosition.Set(newTooltipPosition.x - ((rectTransform.rect.width + equipmentSlot.InventoryItem.RectTransform.rect.width) / 2f), newTooltipPosition.y, 0);

                    // Determine y position
                    if (newTooltipPosition.y >= Screen.height - (rectTransform.rect.height / 2f)) // Too close to the top
                        newTooltipPosition.Set(newTooltipPosition.x, Screen.height - (rectTransform.rect.height / 2f) - defaultSlotSize, 0);

                    rectTransform.position = newTooltipPosition;
                }
            }
        }

        void CalculatePosition(ActionBarSlot actionBarSlot)
        {
            newTooltipPosition.Set(actionBarSlot.transform.position.x, ActionSystemUI.ActionButtonContainer.rect.height + (rectTransform.rect.height / 2f), 0);
            rectTransform.position = newTooltipPosition;
        }

        IEnumerator CalculatePosition(Transform looseItemTransform)
        {
            float maxTooltipHeight = rectTransform.rect.height;
            float verticalSpacing = 2f;
            while (gameObject.activeSelf && looseItemTransform == targetTransform)
            {
                Vector2 screenPosition = RectTransformUtility.WorldToScreenPoint(Camera.main, looseItemTransform.position);
                newTooltipPosition.Set(screenPosition.x, screenPosition.y + (rectTransform.rect.height * 2f), 0);

                // Loop through WorldTooltips and adjust positions to prevent vertical overlap
                foreach (Tooltip tooltip in TooltipManager.WorldTooltips)
                {
                    if (tooltip.gameObject.activeSelf == false || tooltip == this)
                        continue;

                    // Check if the tooltip will overlap with the previous tooltip vertically
                    if (Mathf.Abs(newTooltipPosition.y - tooltip.rectTransform.position.y) < maxTooltipHeight)
                    {
                        // Adjust the tooltip's vertical position to stack them vertically
                        newTooltipPosition.y = tooltip.rectTransform.position.y + maxTooltipHeight + verticalSpacing;
                    }
                }

                // Set the tooltip's position after adjusting
                rectTransform.position = newTooltipPosition;

                // Wait for the next frame before checking positions again
                yield return null;
                yield return null;
            }
        }

        string EnumToSpacedString(Enum enumValue)
        {
            secondaryStringBuilder.Clear();
            string enumString = enumValue.ToString();

            secondaryStringBuilder.Append(enumString[0]); // Append the first character
            for (int i = 1; i < enumString.Length; i++)
            {
                char currentChar = enumString[i];

                if (char.IsUpper(currentChar) && char.IsLower(enumString[i - 1]))
                {
                    // Insert a space before a capital letter that follows a lowercase letter
                    secondaryStringBuilder.Append(' ');
                }

                secondaryStringBuilder.Append(currentChar);
            }

            return secondaryStringBuilder.ToString();
        }

        void SplitText(string originalText, int maxCharsPerLine)
        {
            secondaryStringBuilder.Clear();
            foreach (string word in originalText.Split(' '))
            {
                if (secondaryStringBuilder.Length + word.Length <= maxCharsPerLine)
                {
                    secondaryStringBuilder.Append($"{word} ");
                }
                else
                {
                    stringBuilder.AppendLine(secondaryStringBuilder.ToString().TrimEnd());
                    secondaryStringBuilder.Clear().Append($"{word} ");
                }
            }

            // Add the remaining text (if any) as the last line.
            if (secondaryStringBuilder.Length > 0)
                stringBuilder.AppendLine($"{secondaryStringBuilder.ToString().TrimEnd()}");
        }

        void RecalculateTooltipSize()
        {
            LayoutRebuilder.ForceRebuildLayoutImmediate(textMesh.rectTransform);
            rectTransform.sizeDelta = textMesh.rectTransform.sizeDelta;
            LayoutRebuilder.ForceRebuildLayoutImmediate(rectTransform);
        }

        public Button Button => button;
        public Image Image => image;
    }
}
