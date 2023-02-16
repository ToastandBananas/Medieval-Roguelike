using System;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class UnitActionSystemUI : MonoBehaviour
{
    public static UnitActionSystemUI Instance { get; private set; }

    [SerializeField] Transform actionButtonPrefab;
    [SerializeField] Transform actionButtonContainerTransform;
    [SerializeField] TextMeshProUGUI actionPointsText;

    int amountActionButtonsToPool = 8;
    List<ActionButtonUI> actionButtons = new List<ActionButtonUI>();
    
    UnitActionHandler playerActionHandler;

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
        playerActionHandler = UnitManager.Instance.player.unitActionHandler;

        playerActionHandler.OnSelectedActionChanged += UnitActionSystem_OnSelectedActionChanged;

        InitializeActionButtonPool();

        UpdateActionPoints();
        SetupUnitActionButtons();
        UpdateSelectedVisual(); 
    }

    void SetupUnitActionButtons()
    {
        HideActionButtons();

        BaseAction[] baseActionArray = playerActionHandler.baseActionArray;
        for (int i = 0; i < baseActionArray.Length; i++)
        {
            ActionButtonUI newActionButton = GetActionButtonFromPool();
            newActionButton.gameObject.SetActive(true);
            newActionButton.SetBaseAction(baseActionArray[i]);
        }

        UpdateActionVisuals();
    }

    void UnitActionSystem_OnSelectedActionChanged(object sender, EventArgs e)
    {
        if (playerActionHandler.queuedAction == null && playerActionHandler.selectedAction.ActionIsUsedInstantly())
            playerActionHandler.QueueAction(playerActionHandler.selectedAction);
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

    public bool SelectedActionValid() => playerActionHandler.selectedAction.IsValidAction();

    void UpdateSelectedVisual()
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

    public void UpdateActionPoints() => actionPointsText.text = "Last Used AP: " + playerActionHandler.unit.stats.lastUsedAP;
}
