using UnityEngine.UI;
using UnityEngine;
using System.Text;
using InventorySystem;
using TMPro;
using Utilities;
using UnitSystem;
using UnitSystem.ActionSystem.UI;
using System.Collections;
using InteractableObjects;
using UnitSystem.ActionSystem.Actions;

namespace GeneralUI
{
    public class Tooltip : MonoBehaviour
    {
        [SerializeField] RectTransform rectTransform;
        [SerializeField] TextMeshProUGUI textMesh;
        [SerializeField] Image image;
        [SerializeField] Button button;

        readonly StringBuilder stringBuilder = new();

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
            stringBuilder.Append($"<align=center><b><size=22>{StringUtilities.SplitTextIntoParagraphs(itemData.Name(), maxCharactersPerLine_Title)}");
            if (itemData.IsBroken)
                stringBuilder.Append(" (Broken)");
            stringBuilder.Append("</size></b></align>\n");

            // Subtitle
            if (itemData.Item is Item_Weapon)
            {
                stringBuilder.Append("<align=center><i><size=18>");
                if (itemData.Item is Item_MeleeWeapon)
                {
                    if (itemData.Item.MeleeWeapon.IsTwoHanded)
                        stringBuilder.Append("Two-Handed ");
                    else if (itemData.Item.MeleeWeapon.IsVersatile)
                        stringBuilder.Append("Versatile ");
                }

                stringBuilder.Append($"{StringUtilities.EnumToSpacedString(itemData.Item.Weapon.WeaponType)}</size></i></align>\n");
            }
            else if (itemData.Item is Item_Armor)
            {
                stringBuilder.Append("<size=18><align=center><i>Protects ");
                if (itemData.Item is Item_BodyArmor)
                {
                    stringBuilder.Append("torso");
                    if (itemData.Item.BodyArmor.ProtectsArms && itemData.Item.BodyArmor.ProtectsLegs)
                        stringBuilder.Append(", arms, and legs");
                    else if (itemData.Item.BodyArmor.ProtectsArms)
                        stringBuilder.Append(" and arms");
                    else if (itemData.Item.BodyArmor.ProtectsLegs)
                        stringBuilder.Append(" and legs");
                }
                else if (itemData.Item is Item_Shirt)
                {
                    stringBuilder.Append("torso");
                    if (itemData.Item.Shirt.ProtectsArms)
                        stringBuilder.Append(" and arms");
                }
                else if (itemData.Item is Item_LegArmor)
                    stringBuilder.Append(" legs");
                else if (itemData.Item is Item_Helm)
                    stringBuilder.Append(" head");
                else if (itemData.Item is Item_Gloves)
                    stringBuilder.Append(" hands");
                else if (itemData.Item is Item_Boots)
                    stringBuilder.Append(" feet");
                stringBuilder.Append("</i></align></size>\n");
            }

            // Equipped?
            if (this == TooltipManager.WorldTooltips[1] || this == TooltipManager.WorldTooltips[2] || (slot is EquipmentSlot && UnitManager.player.UnitEquipment.Slots.Contains((EquipmentSlot)slot)))
                stringBuilder.Append("<align=center><i><b><size=18>- Equipped -</size></b></i></align>\n\n");
            else
                stringBuilder.Append("\n");

            // Description
            stringBuilder.Append($"<size=16>{StringUtilities.SplitTextIntoParagraphs(itemData.Item.Description, maxCharactersPerLine)}</size>\n");

            if (itemData.ThrowingDamageMultiplier != 0f && itemData.Item is Item_Weapon == false)
            {
                if (itemData.ThrowingDamageMultiplier < 0f)
                    stringBuilder.Append($"\n  Throwing Damage: {itemData.ThrowingDamageMultiplier * 100f}%");
                else
                    stringBuilder.Append($"\n  Throwing Damage: +{itemData.ThrowingDamageMultiplier * 100f}%");
            }

            if (itemData.Item is Item_Weapon)
            {
                stringBuilder.Append($"\n  Damage: {itemData.MinDamage} - {itemData.MaxDamage}");

                if (itemData.ThrowingDamageMultiplier != 0f)
                {
                    if (itemData.ThrowingDamageMultiplier < 0f)
                        stringBuilder.Append($"\n  Throwing Damage: {itemData.ThrowingDamageMultiplier * 100f}%");
                    else
                        stringBuilder.Append($"\n  Throwing Damage: +{itemData.ThrowingDamageMultiplier * 100f}%");
                }

                stringBuilder.Append($"\n  Armor Pierce: {itemData.ArmorPierce * 100f}%");
                stringBuilder.Append($"\n  Vs. Armor: {itemData.EffectivenessAgainstArmor * 100f}%");

                if (itemData.AccuracyModifier != 0f)
                {
                    if (itemData.AccuracyModifier < 0f)
                        stringBuilder.Append($"\n  Accuracy: {itemData.AccuracyModifier * 100f}%");
                    else
                        stringBuilder.Append($"\n  Accuracy: +{itemData.AccuracyModifier * 100f}%");
                }

                if (itemData.BlockChanceModifier != 0f)
                {
                    if (itemData.BlockChanceModifier < 0f)
                        stringBuilder.Append($"\n  Block Chance: {itemData.BlockChanceModifier * 100f}%");
                    else
                        stringBuilder.Append($"\n  Block Chance: +{itemData.BlockChanceModifier * 100f}%");
                }

                stringBuilder.Append("\n");
            }
            else if (itemData.Item is Item_Shield)
            {
                if (itemData.BlockChanceModifier != 0f)
                {
                    if (itemData.BlockChanceModifier < 0f)
                        stringBuilder.Append($"\n  Block Chance: {itemData.BlockChanceModifier * 100f}%");
                    else
                        stringBuilder.Append($"\n  Block Chance: +{itemData.BlockChanceModifier * 100f}%");
                }

                stringBuilder.Append($"\n  Bash Damage: {itemData.MinDamage} - {itemData.MaxDamage}");
                stringBuilder.Append("\n");
            }
            else if (itemData.Item is Item_Armor)
            {
                stringBuilder.Append($"\n  Armor: {itemData.Defense}");
                stringBuilder.Append("\n");
            }
            else if (itemData.Item is Item_Ammunition)
            {
                stringBuilder.Append($"\n  Armor Pierce: {itemData.ArmorPierce * 100f}%");
                stringBuilder.Append($"\n  Vs. Armor: {itemData.EffectivenessAgainstArmor * 100f}%");
                stringBuilder.Append("\n");
            }
            else if (itemData.Item is Item_Backpack)
            {
                if (itemData.Item.Backpack.InventorySections.Length > 1)
                {
                    stringBuilder.Append($"\n  <i>+{itemData.Item.Backpack.InventorySections.Length - 1} additional pockets</i>");
                    stringBuilder.Append("\n");
                }
            }
            else if (itemData.Item is Item_Quiver)
            {
                stringBuilder.Append($"\n  <i>+{itemData.Item.Quiver.InventorySections[0].AmountOfSlots} {StringUtilities.EnumToSpacedString(itemData.Item.Quiver.InventorySections[0].AllowedItemTypes[0])} slots</i>");
                stringBuilder.Append("\n");
            }
            else if (itemData.Item is Item_Belt)
            {
                // Added Belt Attachments
                Item_Belt belt = itemData.Item as Item_Belt;
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
            
            // Durability, uses, or stack size
            if (itemData.MaxDurability != 0f)
            {
                stringBuilder.Append($"\n<size=16>Durability: {Mathf.CeilToInt(itemData.CurrentDurability)} / {itemData.MaxDurability}</size>");
                if (itemData.CurrentDurability <= 0)
                    stringBuilder.Append(" <size=16>(Broken)</size>");
            }
            else if (itemData.Item.MaxUses > 1)
                stringBuilder.Append($"\n<size=16>Remaining Uses: {itemData.RemainingUses} / {itemData.Item.MaxUses}</size>");
            else if (itemData.Item.MaxStackSize > 1)
                stringBuilder.Append($"\n<size=16>Remaining: {itemData.CurrentStackSize} / {itemData.Item.MaxStackSize}</size>");

            // Weight
            stringBuilder.Append($"\n<size=16>Weight: {itemData.Weight()} lbs</size>");

            // Value
            if (itemData.Item.MaxStackSize > 1)
                stringBuilder.Append($"\n<size=16>Value: {itemData.Value * itemData.CurrentStackSize} g ({itemData.Value}g each at {Mathf.RoundToInt(itemData.Value / itemData.Item.Weight * 100f) / 100f} g/lb)</size>");
            else
                stringBuilder.Append($"\n<size=16>Value: {itemData.Value} g ({Mathf.RoundToInt(itemData.Value / itemData.Item.Weight * 100f) / 100f} g/lb)</size>");

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
                if (itemActionBarSlot.ItemData == null || itemActionBarSlot.ItemData.Item == null)
                    return;

                stringBuilder.Append($"<align=center><size=22><b>{itemActionBarSlot.ItemData.Item.Name}</b></size></align>");
            }
            else
            {
                stringBuilder.Append($"<align=center><size=22><b>{actionBarSlot.Action.ActionName()}</b></size></align>\n\n");
                stringBuilder.Append(StringUtilities.SplitTextIntoParagraphs(actionBarSlot.ActionType.GetAction(UnitManager.player).TooltipDescription(), maxCharactersPerLine));
            }

            textMesh.text = stringBuilder.ToString();
            gameObject.SetActive(true);

            RecalculateTooltipSize();
            CalculatePosition(actionBarSlot);
        }

        public void ShowLooseItemTooltip(Interactable_LooseItem looseItem, ItemData looseItemData, bool clickable)
        {
            if (looseItem == null || looseItemData == null || looseItemData.Item == null)
                ClearTooltip();

            stringBuilder.Clear();
            stringBuilder.Append($"<align=center><size=16><b>{looseItemData.Item.Name}");
            if (looseItemData.CurrentStackSize > 1)
                stringBuilder.Append($" x {looseItemData.CurrentStackSize}");
            else if (looseItemData.IsBroken)
                stringBuilder.Append(" (Broken)");
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

        public void ShowUnitHitChanceTooltip(Unit targetUnit, Action_Base selectedAction)
        {
            if (selectedAction is Action_BaseAttack)
            {
                if (targetUnit.HealthSystem.IsDead)
                    return;

                float hitChance = UnitManager.player.Stats.HitChance(targetUnit, selectedAction as Action_BaseAttack) * 100f;
                if (hitChance < 0f) 
                    hitChance = 0f;
                else if (hitChance > 100f) 
                    hitChance = 100f;
                else
                    hitChance = Mathf.RoundToInt(hitChance * 100f) / 100f;

                stringBuilder.Clear();
                stringBuilder.Append($"{hitChance}%");
                textMesh.text = stringBuilder.ToString();
            }

            gameObject.SetActive(true);

            RecalculateTooltipSize();
            StartCoroutine(CalculatePosition(targetUnit));
        }

        public void ClearTooltip()
        {
            if (gameObject.activeSelf == false)
                return;

            stringBuilder.Clear();
            button.onClick.RemoveAllListeners();
            gameObject.SetActive(false);
        }

        void InteractWithLooseItem_OnClick(Interactable_LooseItem looseItem) => UnitManager.player.UnitActionHandler.InteractAction.QueueAction(looseItem);

        void CalculatePosition(Slot slot)
        {
            float tooltipWidth = rectTransform.rect.width * TooltipManager.Canvas.scaleFactor;
            float tooltipHeight = rectTransform.rect.height * TooltipManager.Canvas.scaleFactor;
            if (TooltipManager.ActiveInventoryTooltips == 0)
            {
                TooltipManager.AddToActiveInventoryTooltips();

                float slotWidth;
                newTooltipPosition = slot.ParentSlot().transform.position;

                if (slot is EquipmentSlot)
                {
                    slotWidth = slot.InventoryItem.RectTransform.rect.width * TooltipManager.Canvas.scaleFactor;

                    // Determine x position
                    if (newTooltipPosition.x <= tooltipWidth + slotWidth) // Too far left
                        newTooltipPosition.Set(newTooltipPosition.x + ((tooltipWidth + slotWidth) / 2f), newTooltipPosition.y, 0);
                    else
                        newTooltipPosition.Set(newTooltipPosition.x - ((tooltipWidth + slotWidth) / 2f), newTooltipPosition.y, 0);

                    // Determine y position
                    if (newTooltipPosition.y >= Screen.height - (tooltipHeight / 2f)) // Too close to the top
                        newTooltipPosition.Set(newTooltipPosition.x, Screen.height - (tooltipHeight / 2f) - defaultSlotSize, 0);
                }
                else if (slot is InventorySlot)
                {
                    int itemWidth = slot.GetItemData().Item.Width;
                    int itemHeight = slot.GetItemData().Item.Height;
                    float slotHeight;

                    InventorySlot inventorySlot = slot.InventoryItem.MyInventory.GetSlotFromCoordinate(1, 1);
                    slotWidth = inventorySlot.InventoryItem.RectTransform.rect.width * TooltipManager.Canvas.scaleFactor;
                    slotHeight = inventorySlot.InventoryItem.RectTransform.rect.height * TooltipManager.Canvas.scaleFactor;

                    // Determine x position
                    if (newTooltipPosition.x <= tooltipWidth + slotWidth) // Too far left
                        newTooltipPosition.Set(newTooltipPosition.x + ((tooltipWidth + slotWidth) / 2f), newTooltipPosition.y, 0);
                    else
                        newTooltipPosition.Set(newTooltipPosition.x - (tooltipWidth / 2f) - (itemWidth * slotWidth) + (slotWidth / 2f), newTooltipPosition.y, 0);

                    // Determine y position
                    if (newTooltipPosition.y >= Screen.height - (tooltipHeight / 2f)) // Too close to the top
                        newTooltipPosition.Set(newTooltipPosition.x, Screen.height - (tooltipHeight / 2f) - defaultSlotSize, 0);
                    else if (Mathf.RoundToInt(slotHeight / TooltipManager.Canvas.scaleFactor) == defaultSlotSize) // Normal slot size
                        newTooltipPosition.Set(newTooltipPosition.x, newTooltipPosition.y + (itemHeight * (slotHeight / 2f)) - (slotHeight / 2f), 0);
                    else // Abnormal slot size (i.e. arrow slot)
                        newTooltipPosition.Set(newTooltipPosition.x, newTooltipPosition.y, 0);

                    if (newTooltipPosition.y <= tooltipHeight / 2f) // Too close to the bottom
                        newTooltipPosition.Set(newTooltipPosition.x, (tooltipHeight / 2f) + defaultSlotSize, 0);
                }

                rectTransform.position = newTooltipPosition;
            }
            else if (TooltipManager.ActiveInventoryTooltips == 1) // First weapon tooltip (can be either left or right hand weapon depending on how many are equipped)
            {
                TooltipManager.AddToActiveInventoryTooltips();

                EquipSlot equipSlot = slot.GetItemData().Item.Equipment.EquipSlot;
                EquipmentSlot equipmentSlot = null;
                if (UnitEquipment.IsHeldItemEquipSlot(equipSlot))
                {
                    if (UnitManager.player.UnitEquipment.CurrentWeaponSet == WeaponSet.One)
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
                    float slotWidth = equipmentSlot.InventoryItem.RectTransform.rect.width * TooltipManager.Canvas.scaleFactor;
                    newTooltipPosition = equipmentSlot.transform.position;
                    newTooltipPosition.Set(newTooltipPosition.x - ((tooltipWidth + slotWidth) / 2f), newTooltipPosition.y, 0);

                    // Determine y position
                    if (newTooltipPosition.y >= Screen.height - (tooltipHeight / 2f)) // Too close to the top
                        newTooltipPosition.Set(newTooltipPosition.x, Screen.height - (tooltipHeight / 2f) - defaultSlotSize, 0);

                    rectTransform.position = newTooltipPosition;
                }
            }
            else // Second weapon tooltip (always right hand weapon)
            {
                EquipmentSlot equipmentSlot = null;
                if (UnitManager.player.UnitEquipment.CurrentWeaponSet == WeaponSet.One)
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
                    float slotWidth = equipmentSlot.InventoryItem.RectTransform.rect.width * TooltipManager.Canvas.scaleFactor;
                    newTooltipPosition = equipmentSlot.transform.position;
                    newTooltipPosition.Set(newTooltipPosition.x - ((tooltipWidth + slotWidth) / 2f), newTooltipPosition.y, 0);

                    // Determine y position
                    if (newTooltipPosition.y >= Screen.height - (tooltipHeight / 2f)) // Too close to the top
                        newTooltipPosition.Set(newTooltipPosition.x, Screen.height - (tooltipHeight / 2f) - defaultSlotSize, 0);

                    rectTransform.position = newTooltipPosition;
                }
            }
        }

        void CalculatePosition(ActionBarSlot actionBarSlot)
        {
            newTooltipPosition.Set(actionBarSlot.transform.position.x, ActionSystemUI.ActionButtonContainer.rect.height * TooltipManager.Canvas.scaleFactor + (rectTransform.rect.height * TooltipManager.Canvas.scaleFactor / 2f), 0);
            rectTransform.position = newTooltipPosition;
        }

        IEnumerator CalculatePosition(Transform looseItemTransform)
        {
            float maxTooltipHeight = rectTransform.rect.height * TooltipManager.Canvas.scaleFactor;
            float verticalSpacing = 2f;
            while (gameObject.activeSelf && looseItemTransform == targetTransform)
            {
                Vector2 screenPosition = RectTransformUtility.WorldToScreenPoint(Camera.main, looseItemTransform.position);
                newTooltipPosition.Set(screenPosition.x, screenPosition.y + (rectTransform.rect.height * TooltipManager.Canvas.scaleFactor * 2f), 0);

                // Loop through WorldTooltips and adjust positions to prevent vertical overlap
                foreach (Tooltip tooltip in TooltipManager.WorldTooltips)
                {
                    if (tooltip == this)
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

        IEnumerator CalculatePosition(Unit targetUnit)
        {
            float maxTooltipHeight = rectTransform.rect.height * TooltipManager.Canvas.scaleFactor;
            float verticalSpacing = 2f;

            while (gameObject.activeSelf)
            {
                Vector2 screenPosition = RectTransformUtility.WorldToScreenPoint(Camera.main, targetUnit.transform.position);
                newTooltipPosition.Set(screenPosition.x, screenPosition.y + (rectTransform.rect.height * TooltipManager.Canvas.scaleFactor * 2f), 0);

                // Loop through WorldTooltips and adjust positions to prevent vertical overlap
                foreach (Tooltip tooltip in TooltipManager.WorldTooltips)
                {
                    if (tooltip == this)
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
