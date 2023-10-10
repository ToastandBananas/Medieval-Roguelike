using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;
using InteractableObjects;
using GridSystem;
using InventorySystem;
using UnitSystem;

namespace ActionSystem
{
    public abstract class UnitActionHandler : MonoBehaviour
    {
        /// <summary>The Units targeted by the last attack and the item they blocked with (if they successfully blocked).</summary>
        public Dictionary<Unit, HeldItem> targetUnits { get; private set; }

        public List<BaseAction> queuedActions { get; private set; }
        public BaseAction queuedAttack { get; private set; }
        public BaseAction lastQueuedAction { get; protected set; }
        public List<int> queuedAPs { get; protected set; }

        [Header("Actions")]
        [SerializeField] List<ActionType> availableActionTypes = new List<ActionType>();
        protected List<BaseAction> availableActions = new List<BaseAction>();
        protected List<BaseAction> availableCombatActions = new List<BaseAction>();
        public ActionType selectedActionType { get; private set; }

        public Unit unit { get; private set; }
        public Unit targetEnemyUnit { get; protected set; }
        public GridPosition previousTargetEnemyGridPosition { get; private set; }

        [Header("Layer Masks")]
        [SerializeField] LayerMask attackObstacleMask;
        [SerializeField] LayerMask moveObstacleMask;

        public bool isMoving { get; private set; }
        public bool isAttacking { get; private set; }
        public bool isRotating { get; private set; }
        public bool isPerformingAction { get; protected set; }
        public bool canPerformActions { get; protected set; }

        public virtual void Awake()
        {
            unit = GetComponent<Unit>();
            queuedActions = new List<BaseAction>();
            queuedAPs = new List<int>();

            // Determine which BaseActions are combat actions and add them to the combatActions list
            for (int i = 0; i < availableActionTypes.Count; i++)
            {
                availableActionTypes[i].GetAction(unit);
            }

            targetUnits = new Dictionary<Unit, HeldItem>();

            // Default to the MoveAction
            SetDefaultSelectedAction();
        }

        #region Action Queue
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

        public virtual void QueueAction(BaseAction action, int position = -1)
        {
            GridSystemVisual.HideGridVisual();

            // if (unit.IsPlayer) Debug.Log(name + " queued " + action);
            queuedAttack = null;

            if (position == -1)
            {
                queuedActions.Add(action);
                lastQueuedAction = action;
                queuedAPs.Add(action.GetActionPointsCost());
            }
            else
            {
                queuedActions.Insert(0, action);
                queuedAPs.Insert(0, action.GetActionPointsCost());
            }

            // If the action changed while getting the action point cost (such as when running into a door)
            if (action != queuedActions[0])
                return;

            if (action.IsAttackAction())
                unit.unitAnimator.StopMovingForward();

            if (unit.isMyTurn)
            {
                if (canPerformActions == false)
                    TurnManager.Instance.FinishTurn(unit);
                else if (isMoving == false)
                    GetNextQueuedAction();
            }
        }

        public abstract void GetNextQueuedAction();

        public abstract void TakeTurn();

        public abstract void SkipTurn();

        public virtual void FinishAction()
        {
            if (queuedActions.Count == 0)
                return;

            queuedActions.RemoveAt(0);
            queuedAPs.RemoveAt(0);

            isPerformingAction = false;

            GridSystemVisual.UpdateAttackGridVisual();
        }

        public IEnumerator CancelAction()
        {
            if (queuedActions.Count > 0 && queuedActions[0] is MoveAction == false)
            {
                while (isPerformingAction)
                {
                    yield return null;
                }
            }
            else
            {
                MoveAction moveAction = GetAction<MoveAction>();
                if (moveAction.finalTargetGridPosition != unit.GridPosition)
                    moveAction.SetFinalTargetGridPosition(moveAction.nextTargetGridPosition);
            }

            ClearActionQueue(true);
            SetTargetEnemyUnit(null);
            SetQueuedAttack(null);

            GridSystemVisual.UpdateAttackGridVisual();
        }

        public void ClearActionQueue(bool stopMoveAnimation)
        {
            if (queuedActions.Count > 0 && queuedActions[0].IsAttackAction())
                queuedAttack = null;

            for (int i = queuedActions.Count - 1; i >= 0; i--)
            {
                if (queuedActions[i] is InventoryAction == false && queuedActions[i] is EquipmentAction == false)
                {
                    queuedActions.RemoveAt(i);
                    queuedAPs.RemoveAt(i);
                }
            }

            isPerformingAction = false;

            // If the Unit isn't moving, they might still be in a move animation, so cancel that
            if (stopMoveAnimation && isMoving == false)
                unit.unitAnimator.StopMovingForward();
        }

        public bool AttackQueued() => queuedActions.Count > 0 && (queuedActions[0] is MeleeAction || queuedActions[0] is ShootAction);
        #endregion

        #region Combat
        public void AttackTarget()
        {
            BaseAction selectedAction = selectedActionType.GetAction(unit);
            if (selectedAction.IsAttackAction())
                QueueAction(selectedActionType);
            else if (unit.UnitEquipment.RangedWeaponEquipped() && unit.UnitEquipment.HasValidAmmunitionEquipped())
            {
                if (unit.unitMeshManager.GetHeldRangedWeapon().isLoaded)
                    GetAction<ShootAction>().QueueAction(targetEnemyUnit);
                else
                    GetAction<ReloadAction>().QueueAction();
            }
            else if (unit.UnitEquipment.MeleeWeaponEquipped() || GetAction<MeleeAction>().CanFightUnarmed)
                GetAction<MeleeAction>().QueueAction(targetEnemyUnit);
        }

        public bool IsInAttackRange(Unit targetUnit, bool defaultCombatActionsOnly)
        {
            if (defaultCombatActionsOnly)
            {
                if (unit.UnitEquipment.RangedWeaponEquipped() && GetAction<ShootAction>().IsValidAction() && unit.SelectedAction is MeleeAction == false && GetAction<ShootAction>().IsInAttackRange(targetUnit))
                    return true;

                if (GetAction<MeleeAction>().IsValidAction() && GetAction<MeleeAction>().IsInAttackRange(targetUnit))
                    return true;
            }
            else
            {
                for (int i = 0; i < availableCombatActions.Count; i++)
                {
                    BaseAction action = availableCombatActions[i];
                    if (action.IsValidAction() && unit.stats.HasEnoughEnergy(action.GetEnergyCost()) && action.IsInAttackRange(targetUnit))
                        return true;
                }
            }
            return false;
        }

        public bool TryBlockRangedAttack(Unit attackingUnit)
        {
            if (unit.UnitEquipment.ShieldEquipped())
            {
                // If the attacker is in front of this Unit (greater chance to block)
                if (GetAction<TurnAction>().AttackerInFrontOfUnit(attackingUnit))
                {
                    float random = Random.Range(1f, 100f);
                    if (random <= unit.stats.ShieldBlockChance(unit.unitMeshManager.GetHeldShield(), false))
                    {
                        if (attackingUnit.unitActionHandler.targetUnits.ContainsKey(unit) == false)
                            attackingUnit.unitActionHandler.targetUnits.Add(unit, unit.unitMeshManager.GetHeldShield());
                        return true;
                    }
                }
                // If the attacker is beside this Unit (less of a chance to block)
                else if (GetAction<TurnAction>().AttackerBesideUnit(attackingUnit))
                {
                    float random = Random.Range(1f, 100f);
                    if (random <= unit.stats.ShieldBlockChance(unit.unitMeshManager.GetHeldShield(), true))
                    {
                        if (attackingUnit.unitActionHandler.targetUnits.ContainsKey(unit) == false)
                            attackingUnit.unitActionHandler.targetUnits.Add(unit, unit.unitMeshManager.GetHeldShield());
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
                if (unit.UnitEquipment.ShieldEquipped())
                {
                    // Try blocking with shield
                    random = Random.Range(1f, 100f);
                    if (random <= unit.stats.ShieldBlockChance(unit.unitMeshManager.GetHeldShield(), false))
                    {
                        if (attackingUnit.unitActionHandler.targetUnits.ContainsKey(unit) == false)
                            attackingUnit.unitActionHandler.targetUnits.Add(unit, unit.unitMeshManager.GetHeldShield());
                        return true;
                    }

                    // Still have a chance to block with weapon
                    if (unit.UnitEquipment.MeleeWeaponEquipped())
                    {
                        random = Random.Range(1f, 100f);
                        if (random <= unit.stats.WeaponBlockChance(unit.unitMeshManager.GetPrimaryMeleeWeapon(), false, true))
                        {
                            if (attackingUnit.unitActionHandler.targetUnits.ContainsKey(unit) == false)
                                attackingUnit.unitActionHandler.targetUnits.Add(unit, unit.unitMeshManager.GetPrimaryMeleeWeapon());
                            return true;
                        }
                    }
                }
                else if (unit.UnitEquipment.MeleeWeaponEquipped())
                {
                    if (unit.UnitEquipment.IsDualWielding())
                    {
                        // Try blocking with right weapon
                        random = Random.Range(1f, 100f);
                        if (random <= unit.stats.WeaponBlockChance(unit.unitMeshManager.GetPrimaryMeleeWeapon(), false, false) * GameManager.dualWieldPrimaryEfficiency)
                        {
                            if (attackingUnit.unitActionHandler.targetUnits.ContainsKey(unit) == false)
                                attackingUnit.unitActionHandler.targetUnits.Add(unit, unit.unitMeshManager.GetPrimaryMeleeWeapon());
                            return true;
                        }

                        // Try blocking with left weapon
                        random = Random.Range(1f, 100f);
                        if (random <= unit.stats.WeaponBlockChance(unit.unitMeshManager.GetLeftHeldMeleeWeapon(), false, false) * GameManager.dualWieldSecondaryEfficiency)
                        {
                            if (attackingUnit.unitActionHandler.targetUnits.ContainsKey(unit) == false)
                                attackingUnit.unitActionHandler.targetUnits.Add(unit, unit.unitMeshManager.GetLeftHeldMeleeWeapon());
                            return true;
                        }
                    }
                    else
                    {
                        // Try blocking with only weapon
                        random = Random.Range(1f, 100f);
                        if (random <= unit.stats.WeaponBlockChance(unit.unitMeshManager.GetPrimaryMeleeWeapon(), false, false))
                        {
                            if (attackingUnit.unitActionHandler.targetUnits.ContainsKey(unit) == false)
                                attackingUnit.unitActionHandler.targetUnits.Add(unit, unit.unitMeshManager.GetPrimaryMeleeWeapon());
                            return true;
                        }
                    }
                }
            }
            else if (targetUnitTurnAction.AttackerBesideUnit(attackingUnit))
            {
                if (unit.UnitEquipment.ShieldEquipped())
                {
                    // Try blocking with shield
                    random = Random.Range(1f, 100f);
                    if (random <= unit.stats.ShieldBlockChance(unit.unitMeshManager.GetHeldShield(), true))
                    {
                        if (attackingUnit.unitActionHandler.targetUnits.ContainsKey(unit) == false)
                            attackingUnit.unitActionHandler.targetUnits.Add(unit, unit.unitMeshManager.GetHeldShield());
                        return true;
                    }

                    // Still have a chance to block with weapon
                    if (unit.UnitEquipment.MeleeWeaponEquipped())
                    {
                        random = Random.Range(1f, 100f);
                        if (random <= unit.stats.WeaponBlockChance(unit.unitMeshManager.GetPrimaryMeleeWeapon(), true, true))
                        {
                            if (attackingUnit.unitActionHandler.targetUnits.ContainsKey(unit) == false)
                                attackingUnit.unitActionHandler.targetUnits.Add(unit, unit.unitMeshManager.GetPrimaryMeleeWeapon());
                            return true;
                        }
                    }
                }
                else if (unit.UnitEquipment.MeleeWeaponEquipped())
                {
                    if (unit.UnitEquipment.IsDualWielding())
                    {
                        // Try blocking with right weapon
                        random = Random.Range(1f, 100f);
                        if (random <= unit.stats.WeaponBlockChance(unit.unitMeshManager.GetPrimaryMeleeWeapon(), true, false) * GameManager.dualWieldPrimaryEfficiency)
                        {
                            if (attackingUnit.unitActionHandler.targetUnits.ContainsKey(unit) == false)
                                attackingUnit.unitActionHandler.targetUnits.Add(unit, unit.unitMeshManager.GetPrimaryMeleeWeapon());
                            return true;
                        }

                        // Try blocking with left weapon
                        random = Random.Range(1f, 100f);
                        if (random <= unit.stats.WeaponBlockChance(unit.unitMeshManager.GetLeftHeldMeleeWeapon(), true, false) * GameManager.dualWieldSecondaryEfficiency)
                        {
                            if (attackingUnit.unitActionHandler.targetUnits.ContainsKey(unit) == false)
                                attackingUnit.unitActionHandler.targetUnits.Add(unit, unit.unitMeshManager.GetLeftHeldMeleeWeapon());
                            return true;
                        }
                    }
                    else
                    {
                        // Try blocking with only weapon
                        random = Random.Range(1f, 100f);
                        if (random <= unit.stats.WeaponBlockChance(unit.unitMeshManager.GetPrimaryMeleeWeapon(), true, false))
                        {
                            if (attackingUnit.unitActionHandler.targetUnits.ContainsKey(unit) == false)
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
                    return ActionsPool.GetAction(targetType, unit) as T;
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
                previousTargetEnemyGridPosition = target.GridPosition;
        }

        public void SetQueuedAttack(BaseAction attackAction)
        {
            if (attackAction != null && attackAction.IsAttackAction())
                queuedAttack = attackAction;
            else
                queuedAttack = null;
        }

        public virtual void SetSelectedActionType(ActionType actionType) => selectedActionType = actionType;

        public BaseAction SelectedAction => selectedActionType.GetAction(unit);

        public void SetDefaultSelectedAction() => SetSelectedActionType(FindActionTypeByName("MoveAction"));

        public void SetIsMoving(bool isMoving) => this.isMoving = isMoving;

        public void SetIsAttacking(bool isAttacking) => this.isAttacking = isAttacking;

        public void SetIsRotating(bool isRotating) => this.isRotating = isRotating;

        public void SetCanPerformActions(bool canPerformActions) => this.canPerformActions = canPerformActions;

        public LayerMask AttackObstacleMask => attackObstacleMask;

        public LayerMask MoveObstacleMask => moveObstacleMask;

        public List<ActionType> AvailableActionTypes => availableActionTypes;

        public List<BaseAction> AvailableActions => availableActions;

        public List<BaseAction> AvailableCombatActions => availableCombatActions;
    }
}