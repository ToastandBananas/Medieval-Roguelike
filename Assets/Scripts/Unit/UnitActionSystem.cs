using UnityEngine;
using System;
using UnityEngine.EventSystems;

public class UnitActionSystem : MonoBehaviour
{
    public static UnitActionSystem Instance { get; private set; }

    public event EventHandler OnSelectedUnitChanged;
    public event EventHandler OnUnitDeselected;
    public event EventHandler OnSelectedActionChanged;
    public event EventHandler OnActiveAIUnitChanged;
    public event EventHandler<bool> OnBusyChanged;
    public event EventHandler OnActionStarted;

    [Header("Layer Masks")]
    [SerializeField] LayerMask unitsLayerMask;
    [SerializeField] LayerMask actionObstaclesMask;

    Unit selectedUnit, previousSelectedUnit, activeAIUnit;
    Interactable selectedInteractable;
    BaseAction selectedAction, previousSelectedAction;

    bool isBusy;

    void Awake()
    {
        if (Instance != null)
        {
            Debug.LogError("There's more than one UnitActionSystem! " + transform + " - " + Instance);
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    void Update()
    {
        // If there is already an Action taking place
        if (isBusy)
        {
            ActionLineRenderer.Instance.HideLineRenderers();
            return;
        }

        // If it's an enemy or ally's turn
        if (TurnSystem.Instance.IsPlayerTurn() == false)
        {
            ActionLineRenderer.Instance.HideLineRenderers();
            return;
        }

        // If the mouse pointer is over a UI button
        if (EventSystem.current.IsPointerOverGameObject())
        {
            ActionLineRenderer.Instance.HideLineRenderers();
            return;
        }

        // If the pointer is over a player's Unit, meaning that the player is trying to select the Unit
        if (TryHandleUnitSelection())
            return;

        if (selectedUnit == null)
            return;

        if (GameControls.gamePlayActions.context.WasPressed)
            DeselectUnit();

        HandleSelectedAction();
    }

    void HandleSelectedAction()
    {
        if (GameControls.gamePlayActions.select.WasPressed)
        {
            GridPosition mouseGridPosition = LevelGrid.Instance.GetGridPosition(WorldMouse.GetPosition());

            if (selectedAction.IsValidActionGridPosition(mouseGridPosition) == false)
                return;

            TryStartAction(mouseGridPosition);
        }

        if (selectedAction != null && selectedAction is MoveAction)
            StartCoroutine(ActionLineRenderer.Instance.DrawMovePath());
        else if (selectedUnit != null && selectedAction != null && selectedAction is TurnAction)
            ActionLineRenderer.Instance.DrawTurnArrow(selectedUnit.GetAction<TurnAction>().targetPosition);
        else
            ActionLineRenderer.Instance.HideLineRenderers();
    }

    public void TryStartAction(GridPosition gridPosition)
    {
        if (UnitActionSystemUI.Instance.SelectedActionValid() == false)
            return;

        if (selectedAction is MoveAction)
        {
            if (selectedUnit.TrySpendActionPointsToMove(gridPosition) == false)
                return;
        }
        else if (selectedAction is InteractAction)
        {
            if (selectedUnit.TrySpendActionPointsToInteract(gridPosition) == false)
                return;

            selectedInteractable = LevelGrid.Instance.GetInteractableAtGridPosition(gridPosition);
        }
        else if (selectedAction is TurnAction)
        {
            TurnAction turnAction = (TurnAction)selectedAction;
            if (turnAction.currentDirection == turnAction.DetermineTurnDirection())
                return;
        }
        else
        {
            if (selectedUnit.TrySpendActionPointsToTakeAction(selectedAction) == false)
                return;
        }

        SetBusy();
        selectedAction.TakeAction(gridPosition, ClearBusy);

        OnActionStarted?.Invoke(this, EventArgs.Empty);

        // Default back to the Move Action or the previously selected action if possible
        SetSelectedAction(selectedUnit.GetAction<MoveAction>());
    }

    bool TryHandleUnitSelection()
    {
        if (GameControls.gamePlayActions.select.WasPressed)
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out RaycastHit hit, float.MaxValue, unitsLayerMask))
            {
                if (hit.transform.TryGetComponent(out Unit unit))
                {
                    if (unit == selectedUnit) // Unit is already selected
                        return false;

                    if (unit.IsPlayer() == false) // If the Unit we clicked on is an enemy or NPC controlled Ally
                        return false;

                    SetSelectedUnit(unit);
                    return true;
                }
            }
        }

        return false;
    }

    void SetSelectedUnit(Unit unit)
    {
        previousSelectedUnit = selectedUnit;

        if (previousSelectedUnit != null)
            previousSelectedUnit.BlockCurrentPosition();

        selectedUnit = unit;
        selectedUnit.UnblockCurrentPosition();

        SetSelectedAction(unit.GetAction<MoveAction>()); // Default to the Move Action

        OnSelectedUnitChanged?.Invoke(this, EventArgs.Empty); // If anything is subscribed to this event (so not null), invoke the event
    }

    void DeselectUnit()
    {
        if (selectedUnit != null)
            selectedUnit.BlockCurrentPosition();

        selectedUnit = null;

        StartCoroutine(ActionLineRenderer.Instance.DelayHideLineRenderer());

        OnUnitDeselected?.Invoke(this, EventArgs.Empty);
    }

    public void SetActiveAIUnit(Unit AIUnit)
    {
        activeAIUnit = AIUnit;
        DeselectUnit();

        OnActiveAIUnitChanged?.Invoke(this, EventArgs.Empty);
    }

    public Unit SelectedUnit() => selectedUnit;

    public Unit PreviousSelectedUnit() => previousSelectedUnit;

    public Unit ActiveAIUnit() => activeAIUnit;

    public Interactable SelectedInteractable() => selectedInteractable;

    public void ClearSelectedInteractable() => selectedInteractable = null;

    public void SetSelectedAction(BaseAction baseAction)
    {
        if (previousSelectedAction == null)
            previousSelectedAction = baseAction;
        else
            previousSelectedAction = selectedAction;

        selectedAction = baseAction;

        OnSelectedActionChanged?.Invoke(this, EventArgs.Empty); // If anything is subscribed to this event (so not null), invoke the event
    }

    public BaseAction SelectedAction() => selectedAction;

    public LayerMask ActionObstaclesMask() => actionObstaclesMask;

    void SetBusy()
    {
        isBusy = true;
        OnBusyChanged?.Invoke(this, isBusy);
    }

    void ClearBusy()
    {
        isBusy = false;
        OnBusyChanged?.Invoke(this, isBusy);
    }
}
