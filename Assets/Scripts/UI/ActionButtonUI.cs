using TMPro;
using UnityEngine;
using UnityEngine.UI;

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
            if (playerActionHandler.queuedAction == null)
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
        if (actionType == null)
        {
            transform.gameObject.SetActive(false);
            return;
        }

        // Show the invalid action visual if the Action assigned to this button is an invalid action
        BaseAction action = actionType.GetAction(playerActionHandler.unit);
        if (action == null || action.IsValidAction() == false)
            transform.gameObject.SetActive(false);
        else
        {
            transform.gameObject.SetActive(true);
            if (playerActionHandler.unit.stats.HasEnoughEnergy(action.GetEnergyCost()))
                ActivateButton();
            else
                DeactivateButton();
        }
    }

    void ActivateButton()
    {
        button.interactable = true;
    }

    void DeactivateButton()
    {
        button.interactable = false;
    }

    public ActionType ActionType => actionType;
}
