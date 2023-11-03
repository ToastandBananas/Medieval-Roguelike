using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;
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
        public BaseAttackAction queuedAttack { get; private set; }
        public BaseAction lastQueuedAction { get; protected set; }
        public List<int> queuedAPs { get; protected set; }

        [Header("Actions")]
        [SerializeField] List<ActionType> availableActionTypes = new List<ActionType>();
        protected List<BaseAction> availableActions = new List<BaseAction>();
        protected List<BaseAttackAction> availableCombatActions = new List<BaseAttackAction>();
        public ActionType selectedActionType { get; private set; }
        public MoveAction moveAction { get; private set; }
        public TurnAction turnAction { get; private set; }

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

            moveAction = GetAction<MoveAction>();
            turnAction = GetAction<TurnAction>();

            targetUnits = new Dictionary<Unit, HeldItem>();

            // Default to the MoveAction
            SetDefaultSelectedAction();
        }

        #region Action Queue
        public void QueueAction(BaseAction action, GridPosition targetGridPosition, bool addToFrontOfQueue = false)
        {
            // Debug.Log(unit.name + " queues: " + action.name);
            action.SetTargetGridPosition(targetGridPosition);
            QueueAction(action, addToFrontOfQueue);
        }

        public virtual void QueueAction(BaseAction action, bool addToFrontOfQueue = false)
        {
            Debug.Log(unit.name + " queues: " + action.name);
            GridSystemVisual.HideGridVisual();

            if (unit.health.IsDead())
            {
                ClearActionQueue(true, true);
                return;
            }

            queuedAttack = null;

            for (int i = queuedActions.Count - 1; i >= 0; i--)
            {
                if ((action is MoveAction && queuedActions[i] is BaseAttackAction) || (action is BaseAttackAction && queuedActions[i] is MoveAction))
                {
                    int actionIndex = queuedActions.IndexOf(queuedActions[i]);
                    queuedActions.RemoveAt(actionIndex);
                    queuedAPs.RemoveAt(actionIndex);
                }
            }

            // Make sure not to queue multiple of certain types of actions:
            lastQueuedAction = action;
            if (queuedActions.Contains(action) == false || action.CanQueueMultiple())
            {
                if (addToFrontOfQueue)
                {
                    queuedActions.Insert(0, action);
                    queuedAPs.Insert(0, action.GetActionPointsCost());
                }
                else // Add to end of queue
                {
                    queuedActions.Add(action);
                    queuedAPs.Add(action.GetActionPointsCost());
                }
            }

            if (action is BaseAttackAction)
                unit.unitAnimator.StopMovingForward();

            StartCoroutine(TryTakeTurn());
        }

        public void RemoveActionFromQueue(BaseAction action)
        {
            if (queuedActions.Contains(action))
            {
                for (int i = queuedActions.Count - 1; i >= 0; i--)
                {
                    if (queuedActions[i] != action)
                        continue;

                    queuedActions.Remove(action);
                    if (queuedAPs.Count >= i + 1)
                        queuedAPs.RemoveAt(i);
                }
            }
        }

        IEnumerator TryTakeTurn()
        {
            if (unit.IsMyTurn)
            {
                while (isAttacking) // Wait in case the Unit is performing an opportunity attack
                    yield return null;

                if (canPerformActions == false)
                    TurnManager.Instance.FinishTurn(unit);
                else if (isMoving == false)
                    StartCoroutine(GetNextQueuedAction());
            }
        }

        public void ForceQueueAP(int amountAP)
        {
            queuedActions.Add(GetAction<InventoryAction>()); // Use InventoryAction because it doesn't actually do anything in its TakeAction method
            queuedAPs.Add(amountAP);
            StartCoroutine(TryTakeTurn());
        }

        public abstract IEnumerator GetNextQueuedAction();

        public abstract void TakeTurn();

        public virtual void SkipTurn()
        {
            if (isMoving == false)
                unit.unitAnimator.StopMovingForward();
        }

        public virtual void FinishAction()
        {
            if (queuedActions.Count == 0)
                return;
            
            queuedActions.RemoveAt(0);
            if (queuedAPs.Count > 0)
                queuedAPs.RemoveAt(0);

            isPerformingAction = false;

            GridSystemVisual.UpdateAttackGridVisual();
        }

        public void CancelActions() => StartCoroutine(CancelActions_Coroutine());

        IEnumerator CancelActions_Coroutine()
        {
            if (queuedActions.Count > 0 && queuedActions[0] is MoveAction == false)
            {
                while (isPerformingAction)
                    yield return null;
            }
            else
            {
                if (moveAction.finalTargetGridPosition != unit.GridPosition)
                    moveAction.SetFinalTargetGridPosition(moveAction.nextTargetGridPosition);
            }

            ClearActionQueue(true);
            SetTargetEnemyUnit(null);
            queuedAttack = null;

            if (unit.IsPlayer)
                unit.unitActionHandler.SetDefaultSelectedAction();
        }

        public void InterruptActions()
        {
            for (int i = queuedActions.Count - 1; i >= 0; i--)
            {
                if (queuedActions[i].IsInterruptable())
                {
                    queuedActions.RemoveAt(i);
                    if (queuedAPs.Count >= i + 1)
                        queuedAPs.RemoveAt(i);
                }
            }
        }

        public void ClearActionQueue(bool stopMoveAnimation, bool forceClearAll = false)
        {
            for (int i = queuedActions.Count - 1; i >= 0; i--)
            {
                if (forceClearAll || queuedActions[i].CanBeClearedFromActionQueue())
                {
                    if (queuedAttack == queuedActions[i])
                        queuedAttack = null;

                    queuedActions.RemoveAt(i);
                    if (queuedAPs.Count >= i + 1)
                        queuedAPs.RemoveAt(i);
                }
            }

            isPerformingAction = false;

            // If the Unit isn't moving, they might still be in a move animation, so cancel that
            if (stopMoveAnimation && isMoving == false)
                unit.unitAnimator.StopMovingForward();
        }

        public bool AttackQueuedNext() => queuedActions.Count > 0 && queuedActions[0] is BaseAttackAction;
        #endregion

        #region Combat
        public void AttackTarget() => StartCoroutine(AttackTarget_Coroutine());

        IEnumerator AttackTarget_Coroutine()
        {
            if (targetEnemyUnit == null)
                yield break;

            BaseAction selectedAction = selectedActionType.GetAction(unit);
            if (selectedAction is BaseAttackAction)
            {
                if (targetEnemyUnit.unitActionHandler.isMoving) // Wait for the targetEnemyUnit to stop moving
                {
                    while (targetEnemyUnit.unitActionHandler.isMoving)
                        yield return null;

                    if (selectedAction.BaseAttackAction.IsInAttackRange(targetEnemyUnit, unit.GridPosition, targetEnemyUnit.GridPosition) == false) // Check if they're now out of attack range
                    {
                        moveAction.QueueAction(selectedAction.BaseAttackAction.GetNearestAttackPosition(unit.GridPosition, targetEnemyUnit)); // Move to the nearest valid attack position
                        yield break;
                    }
                }

                selectedAction.BaseAttackAction.QueueAction(targetEnemyUnit);
            }
            else if (unit.UnitEquipment.RangedWeaponEquipped() && unit.UnitEquipment.HasValidAmmunitionEquipped())
            {
                if (unit.unitMeshManager.GetHeldRangedWeapon().isLoaded)
                {
                    if (targetEnemyUnit.unitActionHandler.isMoving) // Wait for the targetEnemyUnit to stop moving
                    {
                        while (targetEnemyUnit.unitActionHandler.isMoving)
                            yield return null;

                        if (GetAction<ShootAction>().IsInAttackRange(targetEnemyUnit, unit.GridPosition, targetEnemyUnit.GridPosition) == false) // Check if they're now out of attack range
                        {
                            moveAction.QueueAction(selectedAction.BaseAttackAction.GetNearestAttackPosition(unit.GridPosition, targetEnemyUnit)); // Move to the nearest valid attack position
                            yield break;
                        }
                    }

                    GetAction<ShootAction>().QueueAction(targetEnemyUnit);
                }
                else
                    GetAction<ReloadAction>().QueueAction();
            }
            else if (unit.UnitEquipment.MeleeWeaponEquipped() || unit.stats.CanFightUnarmed)
            {
                if (targetEnemyUnit.unitActionHandler.isMoving) // Wait for the targetEnemyUnit to stop moving
                {
                    while (targetEnemyUnit.unitActionHandler.isMoving)
                        yield return null;

                    if (GetAction<MeleeAction>().IsInAttackRange(targetEnemyUnit, unit.GridPosition, targetEnemyUnit.GridPosition) == false) // Check if they're now out of attack range
                    {
                        moveAction.QueueAction(selectedAction.BaseAttackAction.GetNearestAttackPosition(unit.GridPosition, targetEnemyUnit)); // Move to the nearest valid attack position
                        yield break;
                    }
                }

                GetAction<MeleeAction>().QueueAction(targetEnemyUnit);
            }
        }

        public bool IsInAttackRange(Unit targetUnit, bool defaultCombatActionsOnly)
        {
            if (defaultCombatActionsOnly)
            {
                if (unit.UnitEquipment.RangedWeaponEquipped() && GetAction<ShootAction>().IsValidAction() && unit.SelectedAction is MeleeAction == false && GetAction<ShootAction>().IsInAttackRange(targetUnit, unit.GridPosition, targetUnit.GridPosition))
                    return true;

                if ((unit.UnitEquipment.MeleeWeaponEquipped() || unit.stats.CanFightUnarmed) && GetAction<MeleeAction>().IsValidAction() && GetAction<MeleeAction>().IsInAttackRange(targetUnit, unit.GridPosition, targetUnit.GridPosition))
                    return true;
            }
            else
            {
                for (int i = 0; i < availableCombatActions.Count; i++)
                {
                    BaseAttackAction action = availableCombatActions[i];
                    if (action.IsValidAction() && unit.stats.HasEnoughEnergy(action.GetEnergyCost()) && action.IsInAttackRange(targetUnit, unit.GridPosition, targetUnit.GridPosition))
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
                if (turnAction.AttackerInFrontOfUnit(attackingUnit))
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
                else if (turnAction.AttackerBesideUnit(attackingUnit))
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
            if (turnAction.AttackerInFrontOfUnit(attackingUnit))
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
            else if (turnAction.AttackerBesideUnit(attackingUnit))
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
                    return ActionsPool.GetAction(targetType, actionType, unit) as T;
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

        public void SetQueuedAttack(BaseAttackAction attackAction, GridPosition targetAttackGridPosition)
        {
            if (attackAction != null)
            {
                queuedAttack = attackAction;
                queuedAttack.SetTargetGridPosition(targetAttackGridPosition);
            }
            else
                queuedAttack = null;
        }

        public void ClearQueuedAttack() => queuedAttack = null;

        public BaseAction SelectedAction => selectedActionType.GetAction(unit);

        public virtual void SetSelectedActionType(ActionType actionType) => selectedActionType = actionType;

        public void SetDefaultSelectedAction()
        {
            SetSelectedActionType(FindActionTypeByName("MoveAction"));
            ActionSystemUI.SetSelectedActionSlot(null);
        }

        public bool DefaultActionIsSelected => SelectedAction is MoveAction;

        public void SetIsMoving(bool isMoving) => this.isMoving = isMoving;

        public void SetIsAttacking(bool isAttacking) => this.isAttacking = isAttacking;

        public void SetIsRotating(bool isRotating) => this.isRotating = isRotating;

        public void SetCanPerformActions(bool canPerformActions) => this.canPerformActions = canPerformActions;

        public NPCActionHandler NPCActionHandler => this as NPCActionHandler;

        public LayerMask AttackObstacleMask => attackObstacleMask;

        public LayerMask MoveObstacleMask => moveObstacleMask;

        public List<ActionType> AvailableActionTypes => availableActionTypes;

        public List<BaseAction> AvailableActions => availableActions;

        public List<BaseAttackAction> AvailableCombatActions => availableCombatActions;
    }
}