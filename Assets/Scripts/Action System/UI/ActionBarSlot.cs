using GeneralUI;
using InventorySystem;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using ContextMenu = GeneralUI.ContextMenu;

namespace UnitSystem.ActionSystem.UI
{
    public class ActionBarSlot : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        [SerializeField] protected Button button;
        [SerializeField] protected Image iconImage;
        [SerializeField] RectTransform rectTransform;
        [SerializeField] protected GameObject selectedImageGameObject;
        [SerializeField] protected ActionBarSection actionBarSection = ActionBarSection.None;

        public ActionType ActionType { get; protected set; }
        public BaseAction Action { get; protected set; }

        protected PlayerActionHandler playerActionHandler;

        void Awake()
        {
            playerActionHandler = GameObject.FindGameObjectWithTag("Player").GetComponent<PlayerActionHandler>();
            DeactivateButton();
        }

        public void SetupAction(ActionType actionType)
        {
            ActionType = actionType;
            Action = actionType.GetAction(playerActionHandler.Unit);
            if (Action != null)
                Action.SetActionBarSlot(this);

            UpdateIcon();
            iconImage.enabled = true;

            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() =>
            {
                if (playerActionHandler.QueuedActions.Count == 0)
                {
                    playerActionHandler.OnClick_ActionBarSlot(actionType);
                    TooltipManager.ClearInventoryTooltips();
                    if (!ContextMenu.IsActive)
                        TooltipManager.ShowActionBarTooltip(this);
                }
            });
        }

        public void UpdateIcon() 
        {
            if (Action != null)
                iconImage.sprite = Action.ActionIcon();
        }

        public virtual void ResetButton()
        {
            ActionType = null;
            if (Action != null)
                Action.SetActionBarSlot(null);

            Action = null;
            iconImage.sprite = null;
            HideSlot();
        }

        public void HideSlot()
        {
            iconImage.enabled = false;
            DeactivateButton();
        }

        public virtual void ShowSlot()
        {
            iconImage.enabled = true;
            UpdateActionVisual();
        }

        public void UpdateSelectedVisual()
        {
            if (ActionType == null)
                return;

            // Show the selected visual if the Action assigned to this button is the currently selected Action
            selectedImageGameObject.SetActive(playerActionHandler.SelectedActionType == ActionType);
        }

        public void UpdateActionVisual()
        {
            if (ActionType == null || playerActionHandler.AvailableActionTypes.Contains(ActionType) == false)
            {
                ResetButton();
                return;
            }

            if (Action == null || Action.ActionBarSection() == ActionBarSection.None)
            {
                ResetButton();
                return;
            }

            if (Action.IsValidAction() && playerActionHandler.Unit.stats.HasEnoughEnergy(Action.InitialEnergyCost()))
                ActivateButton();
            else
                DeactivateButton();
        }

        public void ActivateButton()
        {
            button.interactable = true;
        }

        void DeactivateButton()
        {
            button.interactable = false;
        }

        public ActionBarSection ActionBarSection => actionBarSection;

        public ItemActionBarSlot ItemActionBarSlot => this as ItemActionBarSlot;

        public RectTransform RectTransform => rectTransform;

        public void OnPointerEnter(PointerEventData eventData)
        {
            ActionSystemUI.SetHighlightedActionSlot(this);
            ContextMenu.DisableContextMenu();

            if (TooltipManager.currentActionBarSlot == null || TooltipManager.currentActionBarSlot != this)
            {
                TooltipManager.SetCurrentActionBarSlot(this);

                if (InventoryUI.isDraggingItem == false && ActionSystemUI.IsDraggingAction == false && ActionType != null)
                    TooltipManager.ShowActionBarTooltip(this);
            }
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            if (ActionSystemUI.HighlightedActionSlot == this)
                ActionSystemUI.SetHighlightedActionSlot(null);

            if (InventoryUI.isDraggingItem == false)
                TooltipManager.ClearInventoryTooltips();
        }
    }
}
