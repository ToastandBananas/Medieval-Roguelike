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
        UnitActionSystem.Instance.OnSelectedUnitChanged += UnitActionSystem_OnSelectedUnitChanged;
        UnitActionSystem.Instance.OnUnitDeselected += UnitActionSystem_OnUnitDeselected;
        UnitActionSystem.Instance.OnSelectedActionChanged += UnitActionSystem_OnSelectedActionChanged;
        UnitActionSystem.Instance.OnActionStarted += UnitActionSystem_OnActionStarted;
        TurnSystem.Instance.OnTurnChanged += TurnSystem_OnTurnChanged;
        Unit.OnAnyActionPointsChanged += Unit_OnAnyActionPointsChanged;

        InitializeActionButtonPool();

        UpdateActionPoints();
        if (UnitActionSystem.Instance.SelectedUnit() != null)
        {
            SetupUnitActionButtons();
            UpdateSelectedVisual();
        }
    }

    void SetupUnitActionButtons()
    {
        HideActionButtons();

        Unit selectedUnit = UnitActionSystem.Instance.SelectedUnit();
        BaseAction[] baseActionArray = selectedUnit.GetBaseActionArray();
        for (int i = 0; i < baseActionArray.Length; i++)
        {
            ActionButtonUI newActionButton = GetActionButtonFromPool();
            newActionButton.gameObject.SetActive(true);
            newActionButton.SetBaseAction(baseActionArray[i]);
        }

        UpdateActionVisuals();
    }

    void UnitActionSystem_OnSelectedUnitChanged(object sender, EventArgs e)
    {
        SetupUnitActionButtons();
        UpdateSelectedVisual();
        UpdateActionPoints();
    }

    void UnitActionSystem_OnSelectedActionChanged(object sender, EventArgs e)
    {
        if (UnitActionSystem.Instance.SelectedAction().ActionIsUsedInstantly())
            UnitActionSystem.Instance.TryStartAction(UnitActionSystem.Instance.SelectedUnit().GridPosition());
        else
            UpdateSelectedVisual();
    }

    void UnitActionSystem_OnActionStarted(object sender, EventArgs e) => UpdateActionPoints();

    void UnitActionSystem_OnUnitDeselected(object sender, EventArgs e)
    {
        HideActionButtons();
        UpdateActionPoints();
    }

    void TurnSystem_OnTurnChanged(object sender, EventArgs e) => UpdateActionPoints();

    void Unit_OnAnyActionPointsChanged(object sender, EventArgs e) => UpdateActionPoints();

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

    public bool SelectedActionValid() => UnitActionSystem.Instance.SelectedAction().IsValidAction();

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

    void UpdateActionPoints()
    {
        if (UnitActionSystem.Instance.SelectedUnit() == null)
            actionPointsText.text = "";
        else
            actionPointsText.text = "Action Points: " + UnitActionSystem.Instance.SelectedUnit().ActionPoints();
    }
}
