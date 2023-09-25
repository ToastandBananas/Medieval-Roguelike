using System;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class ActionSystemUI : MonoBehaviour
{
    public static ActionSystemUI Instance { get; private set; }

    [SerializeField] Transform actionButtonPrefab;
    [SerializeField] Transform actionButtonContainerTransform;
    [SerializeField] TextMeshProUGUI actionPointsText;
    [SerializeField] TextMeshProUGUI energyText;
    [SerializeField] TextMeshProUGUI healthText;

    int amountActionButtonsToPool = 8;
    List<ActionButtonUI> actionButtons = new List<ActionButtonUI>();
    
    PlayerActionHandler playerActionHandler;

    void Awake()
    {
        if (Instance != null)
        {
            Debug.LogError("There's more than one UnitActionSystemUI! " + transform + " - " + Instance);
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    void Start()
    {
        playerActionHandler = UnitManager.Instance.player.unitActionHandler as PlayerActionHandler;
        playerActionHandler.OnSelectedActionChanged += UnitActionSystem_OnSelectedActionChanged;

        InitializeActionButtonPool();

        UpdateActionPointsText();
        UpdateEnergyText();
        UpdateHealthText();

        SetupUnitActionButtons();
        UpdateSelectedVisual(); 
    }

    void SetupUnitActionButtons()
    {
        HideActionButtons();

        for (int i = 0; i < playerActionHandler.AvailableActionTypes.Count; i++)
        {
            ActionButtonUI newActionButton = GetActionButtonFromPool();
            newActionButton.SetActionType(playerActionHandler.AvailableActionTypes[i]);
            newActionButton.gameObject.SetActive(true);
        }

        UpdateActionVisuals();
    }

    void UnitActionSystem_OnSelectedActionChanged(object sender, EventArgs e)
    {
        BaseAction selectedAction = playerActionHandler.selectedActionType.GetAction(playerActionHandler.unit);
        if (selectedAction.ActionIsUsedInstantly())
            playerActionHandler.QueueAction(selectedAction);
        else
        {
            UpdateSelectedVisual();
            GridSystemVisual.UpdateGridVisual();
        }
    }

    void HideActionButtons()
    {
        for (int i = 0; i < actionButtons.Count; i++)
        {
            actionButtons[i].ResetButton();
        }
    }

    void InitializeActionButtonPool()
    {
        for (int i = 0; i < amountActionButtonsToPool; i++)
        {
            ActionButtonUI newActionButton = CreateNewActionButton();
            newActionButton.gameObject.SetActive(false);
        }
    }

    ActionButtonUI GetActionButtonFromPool()
    {
        for (int i = 0; i < actionButtons.Count; i++)
        {
            if (actionButtons[i].gameObject.activeSelf == false)
                return actionButtons[i];
        }

        return CreateNewActionButton();
    }

    ActionButtonUI CreateNewActionButton()
    {
        ActionButtonUI newActionButton = Instantiate(actionButtonPrefab, actionButtonContainerTransform).GetComponent<ActionButtonUI>();
        actionButtons.Add(newActionButton);
        return newActionButton;
    }

    public List<ActionButtonUI> GetActionButtonsList() => actionButtons;

    public bool SelectedActionValid() => playerActionHandler.selectedActionType.GetAction(playerActionHandler.unit).IsValidAction();

    public void UpdateSelectedVisual()
    {
        for (int i = 0; i < actionButtons.Count; i++)
        {
            actionButtons[i].UpdateSelectedVisual();
        }
    }

    public void UpdateActionVisuals()
    {
        for (int i = 0; i < actionButtons.Count; i++)
        {
            actionButtons[i].UpdateActionVisual();
        }
    }

    public void UpdateActionPointsText() => actionPointsText.text = $"Last Used AP: {playerActionHandler.unit.stats.lastUsedAP}";

    public void UpdateEnergyText() => energyText.text = $"Energy: {playerActionHandler.unit.stats.currentEnergy}";

    public void UpdateHealthText() => healthText.text = $"Health: {playerActionHandler.unit.health.CurrentHealth()}";
}
