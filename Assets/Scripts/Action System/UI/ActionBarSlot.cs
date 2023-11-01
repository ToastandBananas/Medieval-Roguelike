using GeneralUI;
using InventorySystem;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace ActionSystem
{
    public class ActionBarSlot : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        [SerializeField] protected Button button;
        [SerializeField] protected Image iconImage;
        [SerializeField] protected GameObject selectedImageGameObject;
        [SerializeField] protected ActionBarSection actionBarSection = ActionBarSection.None;

        public ActionType actionType { get; protected set; }
        public BaseAction action { get; protected set; }

        protected PlayerActionHandler playerActionHandler;

        void Awake()
        {
            playerActionHandler = GameObject.FindGameObjectWithTag("Player").GetComponent<PlayerActionHandler>();
            DeactivateButton();
        }

        public void SetupAction(ActionType actionType)
        {
            this.actionType = actionType;
            action = actionType.GetAction(playerActionHandler.unit);
            iconImage.sprite = actionType.ActionIcon;
            iconImage.enabled = true;

            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() =>
            {
                if (playerActionHandler.queuedActions.Count == 0)
                {
                    playerActionHandler.OnClick_ActionBarSlot(actionType);
                }
            });
        }

        public virtual void ResetButton()
        {
            actionType = null;
            action = null;
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
            if (actionType == null)
                return;

            // Show the selected visual if the Action assigned to this button is the currently selected Action
            selectedImageGameObject.SetActive(playerActionHandler.selectedActionType == actionType);
        }

        public void UpdateActionVisual()
        {
            if (actionType == null || playerActionHandler.AvailableActionTypes.Contains(actionType) == false)
            {
                ResetButton();
                return;
            }

            if (action == null || action.ActionBarSection() == ActionBarSection.None)
            {
                ResetButton();
                return;
            }

            if (action.IsValidAction() && playerActionHandler.unit.stats.HasEnoughEnergy(action.GetEnergyCost()))
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

        public void OnPointerEnter(PointerEventData eventData)
        {
            ActionSystemUI.SetHighlightedActionSlot(this);

            if (TooltipManager.currentActionBarSlot == null || TooltipManager.currentActionBarSlot != this)
            {
                TooltipManager.SetCurrentActionBarSlot(this);

                if (InventoryUI.isDraggingItem == false && ActionSystemUI.isDraggingAction == false && actionType != null)
                    TooltipManager.ShowActionBarTooltip(this);
            }
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            if (ActionSystemUI.highlightedActionSlot == this)
                ActionSystemUI.SetHighlightedActionSlot(null);

            if (InventoryUI.isDraggingItem == false)
                TooltipManager.ClearInventoryTooltips();
        }
    }
}
