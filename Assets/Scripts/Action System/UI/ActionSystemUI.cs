using System;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using GridSystem;
using UnitSystem;
using System.Linq;
using Controls;
using UnityEngine.UI;
using ContextMenu = GeneralUI.ContextMenu;
using GeneralUI;
using InventorySystem;

namespace ActionSystem
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

        public static bool isDraggingAction { get; private set; }
        static ActionBarSlot actionSlotDraggedFrom;

        static List<ActionBarSlot> basicActionButtons = new List<ActionBarSlot>();
        static List<ActionBarSlot> specialActionButtons = new List<ActionBarSlot>();
        static List<ItemActionBarSlot> itemActionButtons = new List<ItemActionBarSlot>();

        public static ActionBarSlot selectedActionSlot { get; private set; }
        public static ActionBarSlot highlightedActionSlot { get; private set; }
        static PlayerActionHandler playerActionHandler;

        static readonly int maxActionButtonContainerHeight = 224;
        static readonly int minActionButtonContainerHeight = 96;
        static readonly int actionButtonRowHeightAdjustment = 64;

        float dragTimer = 0f;
        float startDragTime = 0.15f;

        void Awake()
        {
            if (Instance != null)
            {
                Debug.LogError("There's more than one UnitActionSystemUI! " + transform + " - " + Instance);
                Destroy(gameObject);
                return;
            }
            Instance = this;

            playerActionHandler = UnitManager.player.unitActionHandler as PlayerActionHandler;
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
                if (isDraggingAction == false && dragTimer < startDragTime)
                    dragTimer += Time.deltaTime;

                if (isDraggingAction)
                    draggedActionImage.transform.position = Input.mousePosition;
                else if (dragTimer >= startDragTime && highlightedActionSlot != null && highlightedActionSlot.actionType != null)
                {
                    playerActionHandler.SetDefaultSelectedAction();
                    TooltipManager.ClearTooltips();

                    Cursor.visible = false;
                    isDraggingAction = true;
                    actionSlotDraggedFrom = highlightedActionSlot;
                    actionSlotDraggedFrom.HideSlot();

                    draggedActionImage.sprite = highlightedActionSlot.actionType.ActionIcon;
                    draggedActionImage.transform.position = Input.mousePosition;
                    draggedActionImage.enabled = true;
                }
            }
            else if (GameControls.gamePlayActions.menuSelect.WasReleased)
            {
                if (isDraggingAction)
                {
                    if (highlightedActionSlot != null && highlightedActionSlot != actionSlotDraggedFrom && highlightedActionSlot.ActionBarSection == actionSlotDraggedFrom.ActionBarSection)
                        SwapSlots();
                    else // Return to original slot
                        actionSlotDraggedFrom.ShowSlot();

                    TooltipManager.ShowTooltips(highlightedActionSlot);

                    Cursor.visible = true;
                    isDraggingAction = false;
                    actionSlotDraggedFrom = null;
                    draggedActionImage.sprite = null;
                    draggedActionImage.enabled = false;
                }

                dragTimer = 0f;
            }
        }

        void SwapSlots()
        {
            ActionType draggedActionType = actionSlotDraggedFrom.actionType;
            if (actionSlotDraggedFrom.ActionBarSection == ActionBarSection.Item)
            {
                ItemActionBarSlot itemActionSlotDraggedFrom = actionSlotDraggedFrom as ItemActionBarSlot;
                ItemActionBarSlot highlightedItemActionSlot = highlightedActionSlot as ItemActionBarSlot;
                ItemData draggedItemData = itemActionSlotDraggedFrom.itemData;

                actionSlotDraggedFrom.ResetButton();
                if (highlightedActionSlot.actionType != null)
                {
                    itemActionSlotDraggedFrom.SetupAction(highlightedItemActionSlot.itemData);
                    itemActionSlotDraggedFrom.ShowSlot();
                    highlightedItemActionSlot.ResetButton();
                }

                highlightedItemActionSlot.SetupAction(draggedItemData);
                highlightedItemActionSlot.ShowSlot();
            }
            else
            {
                actionSlotDraggedFrom.ResetButton();
                if (highlightedActionSlot.actionType != null)
                {
                    actionSlotDraggedFrom.SetupAction(highlightedActionSlot.actionType);
                    actionSlotDraggedFrom.ShowSlot();
                    highlightedActionSlot.ResetButton();
                }

                highlightedActionSlot.SetupAction(draggedActionType);
                highlightedActionSlot.ShowSlot();
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
            BaseAction baseAction = actionType.GetAction(UnitManager.player);
            if (baseAction.ActionBarSection() == ActionBarSection.Basic)
            {
                for (int i = 0; i < basicActionButtons.Count; i++)
                {
                    if (basicActionButtons[i].actionType != null)
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
                    if (specialActionButtons[i].actionType != null)
                        continue;

                    specialActionButtons[i].SetupAction(actionType);
                    specialActionButtons[i].ActivateButton();
                    return;
                }
            }
        }

        public static void RemoveButton(ActionType actionType)
        {
            BaseAction baseAction = actionType.GetAction(UnitManager.player);
            if (baseAction == null)
                return;

            if (baseAction.ActionBarSection() == ActionBarSection.Basic)
            {
                for (int i = 0; i < basicActionButtons.Count; i++)
                {
                    if (basicActionButtons[i].actionType == actionType)
                        basicActionButtons[i].ResetButton();
                }
            }
            else if (baseAction.ActionBarSection() == ActionBarSection.Special)
            {
                for (int i = 0; i < specialActionButtons.Count; i++)
                {
                    if (specialActionButtons[i].actionType == actionType)
                        specialActionButtons[i].ResetButton();
                }
            }
        }

        static void UnitActionSystem_OnSelectedActionChanged(object sender, EventArgs e)
        {
            BaseAction selectedAction = playerActionHandler.selectedActionType.GetAction(playerActionHandler.unit);
            if (selectedAction.ActionIsUsedInstantly())
            {
                // If trying to reload a ranged weapon and the Player has a quiver with more than one type of projectile, bring up a context menu option asking which projectile to load up
                if (selectedAction is ReloadAction && selectedAction.unit.UnitEquipment.QuiverEquipped() && selectedAction.unit.QuiverInventoryManager.ParentInventory.ItemDatas.Count > 1)
                    ContextMenu.BuildReloadContextMenu();
                else
                    selectedAction.QueueAction(); // Instant actions don't have a target grid position, so just do a simple queue
            }
            else
                GridSystemVisual.UpdateAttackRangeGridVisual();
        }

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
                if (itemActionButtons[i].itemData == null || itemActionButtons[i].itemData.Item == null)
                    return itemActionButtons[i];
            }
            return null;
        }

        public static bool ItemActionBarAlreadyHasItem(ItemData itemData)
        {
            for (int i = 0; i < itemActionButtons.Count; i++)
            {
                if (itemActionButtons[i].itemData != null && itemActionButtons[i].itemData == itemData)
                    return true;
            }
            return false;
        }

        public static bool SelectedActionValid() => playerActionHandler.selectedActionType.GetAction(playerActionHandler.unit).IsValidAction();

        public static void SetSelectedActionSlot(ActionBarSlot actionSlot)
        {
            if (selectedActionSlot != null)
                selectedActionSlot.UpdateSelectedVisual();

            selectedActionSlot = actionSlot;
            if (selectedActionSlot != null)
                selectedActionSlot.UpdateSelectedVisual();
        }

        public static void SetHighlightedActionSlot(ActionBarSlot actionSlot)
        {
            highlightedActionSlot = actionSlot;
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
                if (itemActionButtons[i].itemData != null && itemActionButtons[i].itemData.Item != null && playerActionHandler.unit.UnitInventoryManager.ContainsItemDataInAnyInventory(itemActionButtons[i].itemData) == false)
                    itemActionButtons[i].ResetButton();
            }
        }

        public static void UpdateActionPointsText() => Instance.actionPointsText.text = $"Last Used AP: {playerActionHandler.unit.stats.lastUsedAP}";

        public static void UpdateEnergyText() => Instance.energyText.text = $"Energy: {playerActionHandler.unit.stats.currentEnergy}";

        public static void UpdateHealthText() => Instance.healthText.text = $"Health: {playerActionHandler.unit.health.CurrentHealth}";

        public static RectTransform ActionButtonContainer => Instance.actionButtonContainer;
    }
}
