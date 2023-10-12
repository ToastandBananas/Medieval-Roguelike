using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ActionSystem
{
    public class ActionButtonUI : MonoBehaviour
    {
        [SerializeField] TextMeshProUGUI textMesh;
        [SerializeField] Button button;
        [SerializeField] GameObject selectedImageGameObject;
        [SerializeField] GameObject invalidActionImageGameObject;

        ActionType actionType;

        PlayerActionHandler playerActionHandler;

        void Awake()
        {
            playerActionHandler = GameObject.FindGameObjectWithTag("Player").GetComponent<PlayerActionHandler>();
        }

        public void SetActionType(ActionType actionType)
        {
            this.actionType = actionType;
            textMesh.text = actionType.ActionName.ToUpper();

            button.onClick.RemoveAllListeners();

            button.onClick.AddListener(() =>
            {
                if (playerActionHandler.queuedActions.Count == 0)
                    playerActionHandler.OnClick_SetSelectedActionType(actionType);
            });
        }

        public void ResetButton()
        {
            actionType = null;
            gameObject.SetActive(false);
        }

        public void UpdateSelectedVisual()
        {
            // Show the selected visual if the Action assigned to this button is the currently selected Action
            selectedImageGameObject.SetActive(playerActionHandler.selectedActionType == actionType);
        }

        public void UpdateActionVisual()
        {
            if (actionType == null || playerActionHandler.AvailableActionTypes.Contains(actionType) == false)
            {
                transform.gameObject.SetActive(false);
                return;
            }

            BaseAction action = actionType.GetAction(playerActionHandler.unit);
            if (action == null || action.IsHotbarAction() == false)
            {
                transform.gameObject.SetActive(false);
                return;
            }

            transform.gameObject.SetActive(true);
            if (action.IsValidAction() && playerActionHandler.unit.stats.HasEnoughEnergy(action.GetEnergyCost()))
                ActivateButton();
            else
                DeactivateButton();
        }

        void ActivateButton()
        {
            button.interactable = true;
            invalidActionImageGameObject.SetActive(false);
        }

        void DeactivateButton()
        {
            button.interactable = false;
            invalidActionImageGameObject.SetActive(true);
        }

        public ActionType ActionType => actionType;
    }
}
