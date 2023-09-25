using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

public abstract class UnitActionHandler : MonoBehaviour
{
    public GridPosition targetGridPosition { get; protected set; }

    /// <summary>The Units targeted by the last attack and the item they blocked with (if they successfully blocked).</summary>
    public Dictionary<Unit, HeldItem> targetUnits { get; private set; }

    public BaseAction queuedAction { get; private set; }
    public BaseAction queuedAttack { get; private set; }
    public int queuedAP { get; protected set; }

    [Header("Actions")]
    [SerializeField] List<ActionType> availableActionTypes = new List<ActionType>();
    protected List<ActionType> availableCombatActions = new List<ActionType>();
    public ActionType selectedActionType { get; private set; }
    public ActionType lastQueuedActionType { get; protected set; }

    public Unit unit { get; private set; }
    public Unit targetEnemyUnit { get; protected set; }
    public GridPosition targetAttackGridPosition { get; protected set; }
    public Interactable targetInteractable { get; protected set; }
    public GridPosition previousTargetEnemyGridPosition { get; private set; }

    [Header("Layer Masks")]
    [SerializeField] LayerMask attackObstacleMask;
    [SerializeField] LayerMask moveObstacleMask;

    public bool isMoving { get; private set; }
    public bool isAttacking { get; private set; }
    public bool isRotating { get; private set; }
    public bool isPerformingAction { get; protected set; }
    public bool canPerformActions { get; protected set; }

    public GridPosition finalTargetGridPosition { get; private set; }
    public GridPosition nextTargetGridPosition { get; private set; }

    public virtual void Awake()
    {
        unit = GetComponent<Unit>();

        // Determine which BaseActions are combat actions and add them to the combatActions list
        for (int i = 0; i < availableActionTypes.Count; i++)
        {
            BaseAction action = availableActionTypes[i].GetAction(unit);
            if (action.IsAttackAction())
                availableCombatActions.Add(availableActionTypes[i]);
        }

        targetGridPosition = LevelGrid.GetGridPosition(transform.position);
        targetUnits = new Dictionary<Unit, HeldItem>();
    }

    #region Action Queue
    public void QueueAction(ActionType actionType, GridPosition targetGridPosition)
    {
        SetTargetGridPosition(targetGridPosition);
        QueueAction(actionType);
    }

    public void QueueAction(ActionType actionType)
    {
        if (actionType == null)
        {
            Debug.LogWarning("ActionType is null. Cannot queue action.");
            return;
        }

        BaseAction actionInstance = actionType.GetAction(unit);
        if (actionInstance != null)
            QueueAction(actionInstance);
        else
            Debug.LogWarning("Failed to obtain an action of type " + actionType.GetActionType().Name);
    }

    public void QueueAction(BaseAction action, GridPosition targetGridPosition)
    {
        SetTargetGridPosition(targetGridPosition);
        QueueAction(action);
    }

    public void QueueAction(BaseAction action)
    {
        GridSystemVisual.HideGridVisual();

        if (queuedAction != null)
        {
            ActionSystem.ReturnToPool(queuedAction);
            queuedAction = null;
            queuedAttack = null;
        }

        // if (unit.IsPlayer()) Debug.Log(name + " queued " + action);
        queuedAction = action;
        queuedAction.gameObject.SetActive(true);
        lastQueuedActionType = FindActionTypeByName(action.GetType().Name);
        queuedAP = action.GetActionPointsCost();

        // If the action changed while getting the action point cost (such as when running into a door)
        if (action != queuedAction)
        {
            ActionSystem.ReturnToPool(action);
            return;
        }

        if (action.IsAttackAction())
            unit.unitAnimator.StopMovingForward();

        if (unit.isMyTurn)
        {
            if (canPerformActions == false)
                TurnManager.Instance.FinishTurn(unit);
            else if (isMoving == false)
                GetNextQueuedAction();
        }

        SetSelectedActionType(FindActionTypeByName("MoveAction"));
    }

    public abstract void GetNextQueuedAction();

    public abstract void TakeTurn();

    public abstract void SkipTurn();

    public virtual void FinishAction() => ClearActionQueue(false);

    public IEnumerator CancelAction()
    {
        if (queuedAction is MoveAction == false)
        {
            while (isPerformingAction)
            {
                yield return null;
            }
        }
        else
        {
            if (finalTargetGridPosition != unit.gridPosition)
            {
                targetGridPosition = nextTargetGridPosition;
                finalTargetGridPosition = nextTargetGridPosition;
            }
        }

        ClearActionQueue(true);
        SetTargetInteractable(null);
        SetTargetEnemyUnit(null);
        SetQueuedAttack(null);

        GridSystemVisual.UpdateGridVisual();
    }

    public void ClearActionQueue(bool stopMoveAnimation)
    {
        if (queuedAction != null && queuedAction.IsAttackAction())
            queuedAttack = null;

        // Debug.Log("Clearing action queue");
        if (queuedAction != null)
        {
            ActionSystem.ReturnToPool(queuedAction);
            queuedAction = null;
        }

        queuedAP = 0;
        isPerformingAction = false;

        // If the Unit isn't moving, they might still be in a move animation, so cancel that
        if (stopMoveAnimation && isMoving == false)
            unit.unitAnimator.StopMovingForward();
    }

    public bool AttackQueued() => queuedAction is MeleeAction || queuedAction is ShootAction;
    #endregion

    #region Combat
    public void AttackTarget()
    {
        BaseAction selectedAction = selectedActionType.GetAction(unit);
        if (selectedAction.IsAttackAction())
            QueueAction(selectedActionType, targetAttackGridPosition);
        else if (unit.CharacterEquipment.RangedWeaponEquipped())
        {
            if (unit.unitMeshManager.GetRangedWeapon().isLoaded)
                QueueAction(GetAction<ShootAction>(), targetEnemyUnit.gridPosition);
            else
                QueueAction(GetAction<ReloadAction>());
        }
        else if (unit.CharacterEquipment.MeleeWeaponEquipped() || GetAction<MeleeAction>().CanFightUnarmed())
            QueueAction(GetAction<MeleeAction>(), targetEnemyUnit.gridPosition);
    }

    public bool IsInAttackRange(Unit targetUnit, bool defaultCombatActionsOnly)
    {
        if (defaultCombatActionsOnly)
        {
            if (GetAction<ShootAction>().IsValidAction() && GetAction<ShootAction>().IsInAttackRange(targetUnit))
                return true;

            if (GetAction<MeleeAction>().IsValidAction() && GetAction<MeleeAction>().IsInAttackRange(targetUnit))
                return true;
        }
        else 
        {
            for (int i = 0; i < availableCombatActions.Count; i++)
            {
                BaseAction action = availableCombatActions[i].GetAction(unit);
                if (action.IsValidAction() && unit.stats.HasEnoughEnergy(action.GetEnergyCost()) && action.IsInAttackRange(targetUnit))
                    return true;
            }
        }
        return false;
    }

    public bool TryBlockRangedAttack(Unit attackingUnit)
    {
        if (unit.CharacterEquipment.ShieldEquipped())
        {
            // If the attacker is in front of this Unit (greater chance to block)
            if (GetAction<TurnAction>().AttackerInFrontOfUnit(attackingUnit))
            {
                float random = Random.Range(1f, 100f);
                if (random <= unit.stats.ShieldBlockChance(unit.unitMeshManager.GetShield(), false))
                {
                    attackingUnit.unitActionHandler.targetUnits.Add(unit, unit.unitMeshManager.GetShield());
                    return true;
                }
            }
            // If the attacker is beside this Unit (less of a chance to block)
            else if (GetAction<TurnAction>().AttackerBesideUnit(attackingUnit))
            {
                float random = Random.Range(1f, 100f);
                if (random <= unit.stats.ShieldBlockChance(unit.unitMeshManager.GetShield(), true))
                {
                    attackingUnit.unitActionHandler.targetUnits.Add(unit, unit.unitMeshManager.GetShield());
                    return true;
                }
            }
        }

        if (attackingUnit.unitActionHandler.targetUnits.ContainsKey(unit) == false)
            attackingUnit.unitActionHandler.targetUnits.Add(unit, null);
        return false;
    }

    public bool TryBlockMeleeAttack(Unit attackingUnit)
    {
        float random;
        TurnAction targetUnitTurnAction = GetAction<TurnAction>();
        if (targetUnitTurnAction.AttackerInFrontOfUnit(attackingUnit))
        {
            if (unit.CharacterEquipment.ShieldEquipped())
            {
                // Try blocking with shield
                random = Random.Range(1f, 100f);
                if (random <= unit.stats.ShieldBlockChance(unit.unitMeshManager.GetShield(), false))
                {
                    attackingUnit.unitActionHandler.targetUnits.Add(unit, unit.unitMeshManager.GetShield());
                    return true;
                }

                // Still have a chance to block with weapon
                if (unit.CharacterEquipment.MeleeWeaponEquipped())
                {
                    random = Random.Range(1f, 100f);
                    if (random <= unit.stats.WeaponBlockChance(unit.unitMeshManager.GetPrimaryMeleeWeapon(), false, true))
                    {
                        attackingUnit.unitActionHandler.targetUnits.Add(unit, unit.unitMeshManager.GetPrimaryMeleeWeapon());
                        return true;
                    }
                }
            }
            else if (unit.CharacterEquipment.MeleeWeaponEquipped())
            {
                if (unit.CharacterEquipment.IsDualWielding())
                {
                    // Try blocking with right weapon
                    random = Random.Range(1f, 100f);
                    if (random <= unit.stats.WeaponBlockChance(unit.unitMeshManager.GetPrimaryMeleeWeapon(), false, false) * GameManager.dualWieldPrimaryEfficiency)
                    {
                        attackingUnit.unitActionHandler.targetUnits.Add(unit, unit.unitMeshManager.GetPrimaryMeleeWeapon());
                        return true;
                    }

                    // Try blocking with left weapon
                    random = Random.Range(1f, 100f);
                    if (random <= unit.stats.WeaponBlockChance(unit.unitMeshManager.GetLeftMeleeWeapon(), false, false) * GameManager.dualWieldSecondaryEfficiency)
                    {
                        attackingUnit.unitActionHandler.targetUnits.Add(unit, unit.unitMeshManager.GetLeftMeleeWeapon());
                        return true;
                    }
                }
                else
                {
                    // Try blocking with only weapon
                    random = Random.Range(1f, 100f);
                    if (random <= unit.stats.WeaponBlockChance(unit.unitMeshManager.GetPrimaryMeleeWeapon(), false, false))
                    {
                        attackingUnit.unitActionHandler.targetUnits.Add(unit, unit.unitMeshManager.GetPrimaryMeleeWeapon());
                        return true;
                    }
                }
            }
        }
        else if (targetUnitTurnAction.AttackerBesideUnit(attackingUnit))
        {
            if (unit.CharacterEquipment.ShieldEquipped())
            {
                // Try blocking with shield
                random = Random.Range(1f, 100f);
                if (random <= unit.stats.ShieldBlockChance(unit.unitMeshManager.GetShield(), true))
                {
                    attackingUnit.unitActionHandler.targetUnits.Add(unit, unit.unitMeshManager.GetShield());
                    return true;
                }

                // Still have a chance to block with weapon
                if (unit.CharacterEquipment.MeleeWeaponEquipped())
                {
                    random = Random.Range(1f, 100f);
                    if (random <= unit.stats.WeaponBlockChance(unit.unitMeshManager.GetPrimaryMeleeWeapon(), true, true))
                    {
                        attackingUnit.unitActionHandler.targetUnits.Add(unit, unit.unitMeshManager.GetPrimaryMeleeWeapon());
                        return true;
                    }
                }
            }
            else if (unit.CharacterEquipment.MeleeWeaponEquipped())
            {
                if (unit.CharacterEquipment.IsDualWielding())
                {
                    // Try blocking with right weapon
                    random = Random.Range(1f, 100f);
                    if (random <= unit.stats.WeaponBlockChance(unit.unitMeshManager.GetPrimaryMeleeWeapon(), true, false) * GameManager.dualWieldPrimaryEfficiency)
                    {
                        attackingUnit.unitActionHandler.targetUnits.Add(unit, unit.unitMeshManager.GetPrimaryMeleeWeapon());
                        return true;
                    }

                    // Try blocking with left weapon
                    random = Random.Range(1f, 100f);
                    if (random <= unit.stats.WeaponBlockChance(unit.unitMeshManager.GetLeftMeleeWeapon(), true, false) * GameManager.dualWieldSecondaryEfficiency)
                    {
                        attackingUnit.unitActionHandler.targetUnits.Add(unit, unit.unitMeshManager.GetLeftMeleeWeapon());
                        return true;
                    }
                }
                else
                {
                    // Try blocking with only weapon
                    random = Random.Range(1f, 100f);
                    if (random <= unit.stats.WeaponBlockChance(unit.unitMeshManager.GetPrimaryMeleeWeapon(), true, false))
                    {
                        attackingUnit.unitActionHandler.targetUnits.Add(unit, unit.unitMeshManager.GetPrimaryMeleeWeapon());
                        return true;
                    }
                }
            }
        }

        if (attackingUnit.unitActionHandler.targetUnits.ContainsKey(unit) == false)
            attackingUnit.unitActionHandler.targetUnits.Add(unit, null);
        return false;
    }
    #endregion

    public T GetAction<T>() where T : BaseAction
    {
        foreach (ActionType actionType in availableActionTypes)
        {
            Type targetType = actionType.GetActionType();
            if (typeof(T) == targetType)
                return ActionSystem.GetAction(targetType, unit) as T;
        }
        return null;
    }

    public ActionType FindActionTypeByName(string actionName)
    {
        foreach (ActionType actionType in availableActionTypes)
        {
            if (actionType.ActionTypeName == actionName)
                return actionType;
        }

        Debug.LogWarning("ActionType with name " + actionName + " not found.");
        return null;
    }

    public void SetPreviousTargetEnemyGridPosition(GridPosition newGridPosition) => previousTargetEnemyGridPosition = newGridPosition;

    public virtual void SetTargetEnemyUnit(Unit target)
    {
        if (target != null && target.health.IsDead())
        {
            targetEnemyUnit = null;
            return;
        }

        targetEnemyUnit = target;
        if (target != null)
        {
            targetAttackGridPosition = target.gridPosition;
            previousTargetEnemyGridPosition = target.gridPosition;
        }
    }

    public void SetTargetInteractable(Interactable interactable) => targetInteractable = interactable;

    public void SetTargetGridPosition(GridPosition targetGridPosition) => this.targetGridPosition = targetGridPosition;

    public void SetTargetAttackGridPosition(GridPosition targetAttackGridPosition) => this.targetAttackGridPosition = targetAttackGridPosition;

    public void SetQueuedAttack(BaseAction attackAction)
    {
        if (attackAction != null && attackAction.IsAttackAction())
            queuedAttack = attackAction;
        else
            queuedAttack = null;
    }

    public virtual void SetSelectedActionType(ActionType actionType) => selectedActionType = actionType;

    public void SetIsMoving(bool isMoving) => this.isMoving = isMoving;

    public void SetIsAttacking(bool isAttacking) => this.isAttacking = isAttacking;

    public void SetIsRotating(bool isRotating) => this.isRotating = isRotating;

    public void SetCanPerformActions(bool canPerformActions) => this.canPerformActions = canPerformActions;

    public void SetFinalTargetGridPosition(GridPosition finalGridPosition) => finalTargetGridPosition = finalGridPosition;

    public void SetNextTargetGridPosition(GridPosition nextGridPosition) => nextTargetGridPosition = nextGridPosition;

    public LayerMask AttackObstacleMask => attackObstacleMask;

    public LayerMask MoveObstacleMask => moveObstacleMask;

    public List<ActionType> AvailableActionTypes => availableActionTypes;

    public List<ActionType> AvailableCombatActions => availableCombatActions;
}
