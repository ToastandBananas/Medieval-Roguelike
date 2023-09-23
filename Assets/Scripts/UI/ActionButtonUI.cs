using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class ActionButtonUI : MonoBehaviour
{
    [SerializeField] TextMeshProUGUI textMesh;
    [SerializeField] Button button;
    [SerializeField] GameObject selectedImageGameObject;
    [SerializeField] GameObject invalidActionImageGameObject;

    BaseAction baseAction;

    PlayerActionHandler playerActionHandler;

    void Awake()
    {
        playerActionHandler = GameObject.FindGameObjectWithTag("Player").GetComponent<PlayerActionHandler>();
    }

    public void SetBaseAction(BaseAction baseAction)
    {
        this.baseAction = baseAction;
        textMesh.text = baseAction.GetActionName().ToUpper();

        button.onClick.RemoveAllListeners();

        button.onClick.AddListener(() =>
        {
            if (playerActionHandler.queuedAction == null)
                playerActionHandler.OnClick_SetSelectedAction(baseAction);
        });
    }

    public void ResetButton()
    {
        baseAction = null;
        gameObject.SetActive(false);
    }

    public void UpdateSelectedVisual()
    {
        // Show the selected visual if the Action assigned to this button is the currently selected Action
        selectedImageGameObject.SetActive(playerActionHandler.selectedAction == baseAction);
    }

    public void UpdateActionVisual()
    {
        // Show the invalid action visual if the Action assigned to this button is an invalid action
        if (baseAction == null || baseAction.IsValidAction() == false)
            transform.gameObject.SetActive(false);
        else
        {
            transform.gameObject.SetActive(true);
            if (playerActionHandler.unit.stats.HasEnoughEnergy(baseAction.GetEnergyCost()))
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

    public BaseAction GetBaseAction() => baseAction;
}
