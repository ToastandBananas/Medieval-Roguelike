using System;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using System.Linq;
using Controls;
using UnityEngine.UI;
using GeneralUI;
using InventorySystem;
using UnitSystem.ActionSystem.Actions;

namespace UnitSystem.ActionSystem.UI
{
    public enum ActionBarSection { None, Basic, Special, Item }

    public class ActionSystemUI : MonoBehaviour
    {
        public static ActionSystemUI Instance { get; private set; }

        [SerializeField] Transform actionButtonPrefab;
        [SerializeField] Image draggedActionImage;

        [Header("Parent Transforms")]
        [SerializeField] RectTransform actionButtonContainer;
        [SerializeField] RectTransform basicActionButtonsBackground;
        [SerializeField] RectTransform specialActionsBackground;
        [SerializeField] RectTransform itemActionsBackground;
        [SerializeField] RectTransform basicActionsParentTransform;
        [SerializeField] RectTransform specialActionsParentTransform;
        [SerializeField] RectTransform itemActionsParentTransform;
        [SerializeField] RectTransform basicActionsRowButtonParentTransform;
        [SerializeField] RectTransform specialActionsRowButtonParentTransform;
        [SerializeField] RectTransform itemActionsRowButtonParentTransform;

        [Header("Change Action Button Rows")]
        [SerializeField] ChangeActionButtonRow basicActionChangeRow;
        [SerializeField] ChangeActionButtonRow specialActionChangeRow;
        [SerializeField] ChangeActionButtonRow itemActionChangeRow;

        [Header("Stat Texts")]
        [SerializeField] TextMeshProUGUI actionPointsText;
        [SerializeField] TextMeshProUGUI energyText;
        [SerializeField] TextMeshProUGUI healthText;

        public static bool IsDraggingAction { get; private set; }
        static ActionBarSlot actionSlotDraggedFrom;

        static List<ActionBarSlot> basicActionButtons = new();
        static List<ActionBarSlot> specialActionButtons = new();
        static List<ItemActionBarSlot> itemActionButtons = new();

        public static ActionBarSlot SelectedActionSlot { get; private set; }
        public static ActionBarSlot HighlightedActionSlot { get; private set; }
        static PlayerActionHandler playerActionHandler;

        static readonly int maxActionButtonContainerHeight = 228;
        static readonly int minActionButtonContainerHeight = 96;
        static readonly int actionButtonRowHeightAdjustment = 66;

        float dragTimer = 0f;
        readonly float startDragTime = 0.15f;

        void Awake()
        {
            if (Instance != null)
            {
                Debug.LogError("There's more than one UnitActionSystemUI! " + transform + " - " + Instance);
                Destroy(gameObject);
                return;
            }
            Instance = this;

            playerActionHandler = UnitManager.player.UnitActionHandler as PlayerActionHandler;
            playerActionHandler.OnSelectedActionChanged += UnitActionSystem_OnSelectedActionChanged;

            basicActionButtons = basicActionsParentTransform.GetComponentsInChildren<ActionBarSlot>().ToList();
            specialActionButtons = specialActionsParentTransform.GetComponentsInChildren<ActionBarSlot>().ToList();
            itemActionButtons = itemActionsParentTransform.GetComponentsInChildren<ItemActionBarSlot>().ToList();

            SetupUnitActionButtons();
        }

        void Start()
        {
            UpdateActionPointsText();
            UpdateEnergyText();
            UpdateHealthText();

            UpdateActionVisuals();
        }

        void Update()
        {
            if (GameControls.gamePlayActions.menuSelect.IsPressed)
            {
                if (IsDraggingAction == false && dragTimer < startDragTime)
                    dragTimer += Time.deltaTime;

                if (IsDraggingAction)
                    draggedActionImage.transform.position = Input.mousePosition;
                else if (dragTimer >= startDragTime && HighlightedActionSlot != null && HighlightedActionSlot.ActionType != null)
                {
                    playerActionHandler.SetDefaultSelectedAction();
                    TooltipManager.ClearInventoryTooltips();

                    Cursor.visible = false;
                    IsDraggingAction = true;
                    actionSlotDraggedFrom = HighlightedActionSlot;
                    actionSlotDraggedFrom.HideSlot();

                    if (HighlightedActionSlot is ItemActionBarSlot)
                        draggedActionImage.sprite = HighlightedActionSlot.ItemActionBarSlot.ItemData.Item.HotbarSprite(HighlightedActionSlot.ItemActionBarSlot.ItemData);
                    else
                        draggedActionImage.sprite = HighlightedActionSlot.Action.ActionIcon();

                    draggedActionImage.transform.position = Input.mousePosition;
                    draggedActionImage.enabled = true;
                }
            }
            else if (GameControls.gamePlayActions.menuSelect.WasReleased)
            {
                if (IsDraggingAction)
                {
                    if (HighlightedActionSlot != null && HighlightedActionSlot != actionSlotDraggedFrom && HighlightedActionSlot.ActionBarSection == actionSlotDraggedFrom.ActionBarSection)
                        SwapSlots();
                    else // Return to original slot
                        actionSlotDraggedFrom.ShowSlot();

                    TooltipManager.ShowActionBarTooltip(HighlightedActionSlot);

                    Cursor.visible = true;
                    IsDraggingAction = false;
                    actionSlotDraggedFrom = null;
                    draggedActionImage.sprite = null;
                    draggedActionImage.enabled = false;
                }

                dragTimer = 0f;
            }
        }

        void SwapSlots()
        {
            ActionType draggedActionType = actionSlotDraggedFrom.ActionType;
            if (actionSlotDraggedFrom.ActionBarSection == ActionBarSection.Item)
            {
                ItemActionBarSlot itemActionSlotDraggedFrom = actionSlotDraggedFrom as ItemActionBarSlot;
                ItemActionBarSlot highlightedItemActionSlot = HighlightedActionSlot as ItemActionBarSlot;
                ItemData draggedItemData = itemActionSlotDraggedFrom.ItemData;

                actionSlotDraggedFrom.ResetButton();
                if (HighlightedActionSlot.ActionType != null)
                {
                    itemActionSlotDraggedFrom.SetupAction(highlightedItemActionSlot.ItemData);
                    itemActionSlotDraggedFrom.ShowSlot();
                    highlightedItemActionSlot.ResetButton();
                }

                highlightedItemActionSlot.SetupAction(draggedItemData);
                highlightedItemActionSlot.ShowSlot();
            }
            else
            {
                actionSlotDraggedFrom.ResetButton();
                if (HighlightedActionSlot.ActionType != null)
                {
                    actionSlotDraggedFrom.SetupAction(HighlightedActionSlot.ActionType);
                    actionSlotDraggedFrom.ShowSlot();
                    HighlightedActionSlot.ResetButton();
                }

                HighlightedActionSlot.SetupAction(draggedActionType);
                HighlightedActionSlot.ShowSlot();
            }
        }

        public static void SetupUnitActionButtons()
        {
            for (int i = 0; i < playerActionHandler.AvailableActionTypes.Count; i++)
            {
                if (playerActionHandler.AvailableActionTypes[i].GetAction(UnitManager.player).ActionBarSection() == ActionBarSection.None)
                    continue;

                AddButton(playerActionHandler.AvailableActionTypes[i]);
            }

            UpdateActionVisuals();
        }

        public static void AddButton(ActionType actionType)
        {
            Action_Base baseAction = actionType.GetAction(UnitManager.player);
            if (baseAction.ActionBarSection() == ActionBarSection.Basic)
            {
                for (int i = 0; i < basicActionButtons.Count; i++)
                {
                    if (basicActionButtons[i].ActionType != null)
                        continue;

                    basicActionButtons[i].SetupAction(actionType);
                    basicActionButtons[i].ActivateButton();
                    return;
                }
            }
            else if (baseAction.ActionBarSection() == ActionBarSection.Special)
            {
                for (int i = 0; i < specialActionButtons.Count; i++)
                {
                    if (specialActionButtons[i].ActionType != null)
                        continue;

                    specialActionButtons[i].SetupAction(actionType);
                    specialActionButtons[i].ActivateButton();
                    return;
                }
            }
        }

        public static void RemoveButton(ActionType actionType)
        {
            Action_Base baseAction = actionType.GetAction(UnitManager.player);
            if (baseAction == null)
                return;

            if (baseAction.ActionBarSection() == ActionBarSection.Basic)
            {
                for (int i = 0; i < basicActionButtons.Count; i++)
                {
                    if (basicActionButtons[i].ActionType == actionType)
                        basicActionButtons[i].ResetButton();
                }
            }
            else if (baseAction.ActionBarSection() == ActionBarSection.Special)
            {
                for (int i = 0; i < specialActionButtons.Count; i++)
                {
                    if (specialActionButtons[i].ActionType == actionType)
                        specialActionButtons[i].ResetButton();
                }
            }
        }

        static void UnitActionSystem_OnSelectedActionChanged(object sender, EventArgs e) => playerActionHandler.SelectedAction.OnActionSelected();

        public void IncreaseRowCount()
        {
            if (actionButtonContainer.sizeDelta.y < maxActionButtonContainerHeight)
            {
                actionButtonContainer.sizeDelta = new Vector2(actionButtonContainer.sizeDelta.x, actionButtonContainer.sizeDelta.y + actionButtonRowHeightAdjustment);

                basicActionButtonsBackground.sizeDelta = new Vector2(basicActionButtonsBackground.sizeDelta.x, basicActionButtonsBackground.sizeDelta.y + actionButtonRowHeightAdjustment);
                specialActionsBackground.sizeDelta = new Vector2(specialActionsBackground.sizeDelta.x, specialActionsBackground.sizeDelta.y + actionButtonRowHeightAdjustment);
                itemActionsBackground.sizeDelta = new Vector2(itemActionsBackground.sizeDelta.x, itemActionsBackground.sizeDelta.y + actionButtonRowHeightAdjustment);

                basicActionsRowButtonParentTransform.sizeDelta = new Vector2(basicActionsRowButtonParentTransform.sizeDelta.x, basicActionsRowButtonParentTransform.sizeDelta.y + actionButtonRowHeightAdjustment);
                specialActionsRowButtonParentTransform.sizeDelta = new Vector2(specialActionsRowButtonParentTransform.sizeDelta.x, specialActionsRowButtonParentTransform.sizeDelta.y + actionButtonRowHeightAdjustment);
                itemActionsRowButtonParentTransform.sizeDelta = new Vector2(itemActionsRowButtonParentTransform.sizeDelta.x, itemActionsRowButtonParentTransform.sizeDelta.y + actionButtonRowHeightAdjustment);
            }

            if (actionButtonContainer.sizeDelta.y == maxActionButtonContainerHeight)
            {
                basicActionChangeRow.DeactivateButtons();
                specialActionChangeRow.DeactivateButtons();
                itemActionChangeRow.DeactivateButtons();

                if (basicActionsParentTransform.offsetMax.y == actionButtonRowHeightAdjustment)
                    basicActionChangeRow.IncreaseRow();

                if (specialActionsParentTransform.offsetMax.y == actionButtonRowHeightAdjustment)
                    specialActionChangeRow.IncreaseRow();

                if (itemActionsParentTransform.offsetMax.y == actionButtonRowHeightAdjustment)
                    itemActionChangeRow.IncreaseRow();
            }
            else if (actionButtonContainer.sizeDelta.y != minActionButtonContainerHeight)
            {
                if (basicActionsParentTransform.offsetMax.y == actionButtonRowHeightAdjustment * 2)
                    basicActionChangeRow.IncreaseRow();

                if (specialActionsParentTransform.offsetMax.y == actionButtonRowHeightAdjustment * 2)
                    specialActionChangeRow.IncreaseRow();

                if (itemActionsParentTransform.offsetMax.y == actionButtonRowHeightAdjustment * 2)
                    itemActionChangeRow.IncreaseRow();
            }
        }
        
        public void DecreaseRowCount()
        {
            if (actionButtonContainer.sizeDelta.y > minActionButtonContainerHeight)
            {
                actionButtonContainer.sizeDelta = new Vector2(actionButtonContainer.sizeDelta.x, actionButtonContainer.sizeDelta.y - actionButtonRowHeightAdjustment);

                basicActionButtonsBackground.sizeDelta = new Vector2(basicActionButtonsBackground.sizeDelta.x, basicActionButtonsBackground.sizeDelta.y - actionButtonRowHeightAdjustment);
                specialActionsBackground.sizeDelta = new Vector2(specialActionsBackground.sizeDelta.x, specialActionsBackground.sizeDelta.y - actionButtonRowHeightAdjustment);
                itemActionsBackground.sizeDelta = new Vector2(itemActionsBackground.sizeDelta.x, itemActionsBackground.sizeDelta.y - actionButtonRowHeightAdjustment);

                basicActionsRowButtonParentTransform.sizeDelta = new Vector2(basicActionsRowButtonParentTransform.sizeDelta.x, basicActionsRowButtonParentTransform.sizeDelta.y - actionButtonRowHeightAdjustment);
                specialActionsRowButtonParentTransform.sizeDelta = new Vector2(specialActionsRowButtonParentTransform.sizeDelta.x, specialActionsRowButtonParentTransform.sizeDelta.y - actionButtonRowHeightAdjustment);
                itemActionsRowButtonParentTransform.sizeDelta = new Vector2(itemActionsRowButtonParentTransform.sizeDelta.x, itemActionsRowButtonParentTransform.sizeDelta.y - actionButtonRowHeightAdjustment);
            }

            basicActionChangeRow.ActivateButtons();
            specialActionChangeRow.ActivateButtons();
            itemActionChangeRow.ActivateButtons();
        }

        public static ItemActionBarSlot GetNextAvailableItemActionBarSlot()
        {
            for (int i = 0; i < itemActionButtons.Count; i++)
            {
                if (itemActionButtons[i].ItemData == null || itemActionButtons[i].ItemData.Item == null)
                    return itemActionButtons[i];
            }
            return null;
        }

        public static bool ItemActionBarAlreadyHasItem(ItemData itemData)
        {
            for (int i = 0; i < itemActionButtons.Count; i++)
            {
                if (itemActionButtons[i].ItemData != null && itemActionButtons[i].ItemData == itemData)
                    return true;
            }
            return false;
        }

        public static ItemActionBarSlot GetItemActionBarSlot(ItemData itemData)
        {
            if (itemData == null)
                return null;

            for (int i = 0; i < itemActionButtons.Count; i++)
            {
                if (itemActionButtons[i].ItemData == itemData)
                    return itemActionButtons[i];
            }
            return null;
        }

        public static ActionBarSlot GetActionBarSlot(ActionType actionType)
        {
            for (int i = 0; i < basicActionButtons.Count; i++)
            {
                if (basicActionButtons[i].ActionType == actionType)
                    return basicActionButtons[i];
            }

            for (int i = 0; i < specialActionButtons.Count; i++)
            {
                if (specialActionButtons[i].ActionType == actionType)
                    return specialActionButtons[i];
            }
            return null;
        }

        public static bool SelectedActionValid() => playerActionHandler.SelectedActionType.GetAction(playerActionHandler.Unit).IsValidAction();

        public static void SetSelectedActionSlot(ActionBarSlot actionSlot)
        {
            if (SelectedActionSlot != null)
                SelectedActionSlot.Deselect();

            SelectedActionSlot = actionSlot;
            if (SelectedActionSlot != null)
                SelectedActionSlot.Select();
        }

        public static void SetHighlightedActionSlot(ActionBarSlot actionSlot)
        {
            HighlightedActionSlot = actionSlot;
        }

        public static void UpdateActionVisuals()
        {
            for (int i = 0; i < basicActionButtons.Count; i++)
            {
                basicActionButtons[i].UpdateActionVisual();
            }

            for (int i = 0; i < specialActionButtons.Count; i++)
            {
                specialActionButtons[i].UpdateActionVisual();
            }

            for (int i = 0; i < itemActionButtons.Count; i++)
            {
                if (itemActionButtons[i].ItemData != null && itemActionButtons[i].ItemData.Item != null && !playerActionHandler.Unit.UnitInventoryManager.ContainsItemDataInAnyInventory(itemActionButtons[i].ItemData))
                    itemActionButtons[i].ResetButton();
            }
        }

        public static void UpdateActionPointsText() => Instance.actionPointsText.text = $"Last Used AP: {playerActionHandler.Unit.Stats.LastUsedAP}";

        public static void UpdateEnergyText() => Instance.energyText.text = $"Energy: {playerActionHandler.Unit.Stats.CurrentEnergy}";

        public static void UpdateHealthText() => Instance.healthText.text = $"Health: {playerActionHandler.Unit.HealthSystem.CurrentHealth}";

        public static RectTransform ActionButtonContainer => Instance.actionButtonContainer;
    }
}
