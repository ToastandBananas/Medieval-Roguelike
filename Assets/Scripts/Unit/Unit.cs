using System;
using UnityEngine;
using Pathfinding;

public class Unit : MonoBehaviour
{
    public static event EventHandler OnAnyActionPointsChanged;
    public static event EventHandler OnAnyUnitSpawned;
    public static event EventHandler OnAnyUnitDead;

    [Header("Components")]
    [SerializeField] Animator unitAnim;
    [SerializeField] SingleNodeBlocker singleNodeBlocker;
    [SerializeField] Transform leftHeldItemParent, rightHeldItemParent;
    HeldItem leftHeldItem, rightHeldItem;

    [Header("Unit Info")]
    [SerializeField] float shoulderHeight = 0.25f;

    [SerializeField] int maxActionPoints = 100;
    int actionPoints;

    [SerializeField][Range(1, 100)] float rangedAccuracy = 33f;

    GridPosition gridPosition;

    Alliance alliance;
    HealthSystem healthSystem;
    UnitAnimator unitAnimator;

    BaseAction[] baseActionArray;

    void Awake()
    {
        alliance = GetComponent<Alliance>();
        healthSystem = GetComponent<HealthSystem>();
        unitAnimator = GetComponent<UnitAnimator>();

        baseActionArray = GetComponents<BaseAction>();

        actionPoints = maxActionPoints;
    }

    void Start()
    {
        singleNodeBlocker.manager = LevelGrid.Instance.GetBlockManager();
        LevelGrid.Instance.AddSingleNodeBlockerToList(singleNodeBlocker, LevelGrid.Instance.GetUnitSingleNodeBlockerList());
        BlockCurrentPosition();

        SetLeftHeldItem();
        SetRightHeldItem();

        gridPosition = LevelGrid.Instance.GetGridPosition(transform.position);
        LevelGrid.Instance.AddUnitAtGridPosition(gridPosition, this);

        TurnSystem.Instance.OnTurnChanged += TurnSystem_OnTurnChanged;

        healthSystem.OnDead += HealthSystem_OnDead;

        OnAnyUnitSpawned?.Invoke(this, EventArgs.Empty);
    }

    void FixedUpdate()
    {
        if (GetAction<MoveAction>().IsActive())
            UpdateGridPosition();
    }

    public void UpdateGridPosition()
    {
        GridPosition newGridPosition = LevelGrid.Instance.GetGridPosition(transform.position);
        if (newGridPosition != gridPosition)
        {
            // Unit changed Grid Position
            GridPosition oldGridPosition = gridPosition;
            gridPosition = newGridPosition;
            LevelGrid.Instance.UnitMovedGridPosition(this, oldGridPosition, newGridPosition);
        }
    }

    #region Actions

    public T GetAction<T>() where T : BaseAction
    {
        foreach (BaseAction baseAction in baseActionArray)
        {
            if (baseAction is T)
                return (T)baseAction;
        }
        return null;
    }

    public BaseAction[] GetBaseActionArray() => baseActionArray;

    void SpendActionPoints(int amount)
    {
        actionPoints -= amount;
        
        OnAnyActionPointsChanged?.Invoke(this, EventArgs.Empty);
    }

    public int ActionPoints() => actionPoints;

    public int MaxActionPoints() => maxActionPoints;

    #endregion

    public float RangedAccuracy() => rangedAccuracy;

    public float ShoulderHeight() => shoulderHeight;

    public GridPosition GridPosition() => gridPosition;

    public Vector3 WorldPosition() => LevelGrid.Instance.GetWorldPosition(gridPosition);

    public Animator UnitAnimator() => unitAnim;

    public bool CanSpendActionPointsToTakeAction(BaseAction baseAction) => actionPoints >= baseAction.GetActionPointsCost();

    public bool CanSpendActionPointsToMove(MoveAction moveAction, float moveDistance) => actionPoints >= moveAction.GetActionPointsCost(moveDistance);

    public bool CanSpendActionPointsToInteract(InteractAction interactAction, Interactable interactable) => actionPoints >= interactAction.GetActionPointsCost(interactable);

    public bool TrySpendActionPointsToTakeAction(BaseAction baseAction)
    {
        if (CanSpendActionPointsToTakeAction(baseAction))
        {
            SpendActionPoints(baseAction.GetActionPointsCost());
            return true;
        }

        return false;
    }

    public bool TrySpendActionPointsToMove(GridPosition endGridPosition)
    {
        MoveAction moveAction = GetAction<MoveAction>();

        ABPath path = ABPath.Construct(transform.position, LevelGrid.Instance.GetWorldPosition(endGridPosition));
        path.traversalProvider = LevelGrid.Instance.DefaultTraversalProvider();

        GetAction<MoveAction>().Seeker().StartPath(path);

        path.BlockUntilCalculated();

        float moveDistance = TacticsPathfindingUtilities.CalculateWorldSpaceMoveDistanceFromPath_XZ(path);

        if (CanSpendActionPointsToMove(moveAction, moveDistance))
        {
            SpendActionPoints(moveAction.GetActionPointsCost(moveDistance));
            return true;
        }

        return false;
    }

    public bool TrySpendActionPointsToInteract(GridPosition interactGridPosition)
    {
        InteractAction interactAction = GetAction<InteractAction>();
        Interactable interactable = LevelGrid.Instance.GetInteractableAtGridPosition(interactGridPosition);
        if (CanSpendActionPointsToInteract(interactAction, interactable))
        {
            SpendActionPoints(interactAction.GetActionPointsCost(interactable));
            return true;
        }

        return false;
    }

    void TurnSystem_OnTurnChanged(object sender, EventArgs e)
    {
        if ((alliance.IsPlayer() == false && TurnSystem.Instance.IsPlayerTurn() == false) 
            || (alliance.IsPlayer() && TurnSystem.Instance.IsPlayerTurn()))
        {
            actionPoints = maxActionPoints;

            OnAnyActionPointsChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    public void TakeDamage(int damageAmount)
    {
        healthSystem.Damage(damageAmount);
    }

    void HealthSystem_OnDead(object sender, EventArgs e)
    {
        LevelGrid.Instance.RemoveUnitAtGridPosition(gridPosition);
        LevelGrid.Instance.AddDeadUnitAtGridPosition(gridPosition, this);

        unitAnimator.Die();
        OnAnyUnitDead?.Invoke(this, EventArgs.Empty);
    }

    public HealthSystem HealthSystem() => healthSystem;

    public HeldItem LeftHeldItem() => leftHeldItem;

    public HeldItem RightHeldItem() => rightHeldItem;

    public bool IsRangedUnit()
    {
        if (leftHeldItem != null && leftHeldItem is RangedWeapon)
            return true;

        return false;
    }

    public void SetLeftHeldItem()
    {
        if (leftHeldItemParent.childCount > 0)
        {
            leftHeldItem = leftHeldItemParent.GetChild(0).GetComponent<HeldItem>();
            leftHeldItem.SetupBaseActions();
            unitAnimator.SetLeftHeldItemAnim(leftHeldItem.GetComponent<Animator>());
        }
    }

    public void SetRightHeldItem()
    {
        if (rightHeldItemParent.childCount > 0)
        {
            rightHeldItem = rightHeldItemParent.GetChild(0).GetComponent<HeldItem>();
            rightHeldItem.SetupBaseActions();
            unitAnimator.SetRightHeldItemAnim(rightHeldItem.GetComponent<Animator>());
        }
    }

    public void BlockCurrentPosition() => singleNodeBlocker.BlockAtCurrentPosition();

    public void UnblockCurrentPosition() => singleNodeBlocker.Unblock();

    #region Alliance Passthrough Functions
    public bool IsPlayer() => alliance.IsPlayer();

    public Faction GetCurrentFaction() => alliance.GetCurrentFaction();

    public bool IsAlly(Faction factionToCheckAgainst) => alliance.IsAlly(factionToCheckAgainst);

    public bool IsEnemy(Faction factionToCheckAgainst) => alliance.IsEnemy(factionToCheckAgainst);

    public Faction[] GetAlliedFactions() => alliance.GetAlliedFactions();

    public Faction[] GetEnemyFactions() => alliance.GetEnemyFactions();
    #endregion
}
