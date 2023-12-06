using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;
using GridSystem;
using InventorySystem;
using UnitSystem.ActionSystem.Actions;

namespace UnitSystem.ActionSystem
{
    public abstract class UnitActionHandler : MonoBehaviour
    {
        /// <summary>The Units targeted by the last attack and the item they blocked with (if they successfully blocked).</summary>
        public Dictionary<Unit, HeldItem> TargetUnits { get; private set; }

        public List<BaseAction> QueuedActions { get; private set; }
        public BaseAttackAction QueuedAttack { get; private set; }
        public BaseAction LastQueuedAction { get; protected set; }
        public List<int> QueuedAPs { get; protected set; }

        [Header("Actions")]
        [SerializeField] Transform actionsParent;
        [SerializeField] List<ActionType> availableActionTypes = new();
        protected List<BaseAction> availableActions = new();
        protected List<BaseAttackAction> availableCombatActions = new();

        // Cached Actions (actions that every Unit will have)
        public InteractAction InteractAction { get; private set; }
        public MoveAction MoveAction { get; private set; }
        public TurnAction TurnAction { get; private set; }

        public Unit Unit { get; private set; }
        public Unit TargetEnemyUnit { get; protected set; }
        public GridPosition PreviousTargetEnemyGridPosition { get; private set; }

        [Header("Layer Masks")]
        [SerializeField] LayerMask attackObstacleMask;
        [SerializeField] LayerMask moveObstacleMask;

        public bool IsAttacking { get; private set; }
        public bool IsPerformingAction { get; protected set; }
        public bool CanPerformActions { get; protected set; }

        public virtual void Awake()
        {
            Unit = GetComponent<Unit>();
            QueuedActions = new List<BaseAction>();
            QueuedAPs = new List<int>();

            // Determine which BaseActions are combat actions and add them to the combatActions list
            for (int i = 0; i < availableActionTypes.Count; i++)
            {
                availableActionTypes[i].GetAction(Unit);
            }

            InteractAction = GetAction<InteractAction>();
            MoveAction = GetAction<MoveAction>();
            TurnAction = GetAction<TurnAction>();

            TargetUnits = new Dictionary<Unit, HeldItem>();
        }

        #region Action Queue
        public virtual void QueueAction(BaseAction action, bool addToFrontOfQueue = false)
        {
            //if (Unit.IsPlayer)
                //Debug.Log(Unit.name + " queues: " + action.name + " for " + action.ActionPointsCost() + " AP");

            GridSystemVisual.HideGridVisual();

            if (Unit.Health.IsDead)
            {
                ClearActionQueue(true, true);
                return;
            }

            QueuedAttack = null;

            for (int i = QueuedActions.Count - 1; i >= 0; i--)
            {
                if ((action is MoveAction && QueuedActions[i] is BaseAttackAction) || (action is BaseAttackAction && QueuedActions[i] is MoveAction))
                {
                    int actionIndex = QueuedActions.IndexOf(QueuedActions[i]);
                    QueuedActions.RemoveAt(actionIndex);
                    QueuedAPs.RemoveAt(actionIndex);
                }
            }

            // Make sure not to queue multiple of certain types of actions:
            LastQueuedAction = action;
            if (QueuedActions.Contains(action) == false || action.CanQueueMultiple())
            {
                if (addToFrontOfQueue)
                {
                    QueuedActions.Insert(0, action);
                    QueuedAPs.Insert(0, action.ActionPointsCost());
                }
                else // Add to end of queue
                {
                    QueuedActions.Add(action);
                    QueuedAPs.Add(action.ActionPointsCost());
                }
            }

            if (action is BaseAttackAction)
                Unit.UnitAnimator.StopMovingForward();

            StartCoroutine(TryTakeTurn());
        }

        public void RemoveActionFromQueue(BaseAction action)
        {
            if (QueuedActions.Contains(action))
            {
                for (int i = QueuedActions.Count - 1; i >= 0; i--)
                {
                    if (QueuedActions[i] != action)
                        continue;

                    QueuedActions.Remove(action);
                    if (QueuedAPs.Count >= i + 1)
                        QueuedAPs.RemoveAt(i);
                }
            }
        }

        IEnumerator TryTakeTurn()
        {
            if (Unit.IsMyTurn)
            {
                while (IsAttacking || Unit.UnitAnimator.beingKnockedBack) // Wait in case the Unit is performing an opportunity attack or is being knocked back
                    yield return null;

                if (CanPerformActions == false)
                    TurnManager.Instance.FinishTurn(Unit);
                else if (MoveAction.IsMoving == false)
                    StartCoroutine(GetNextQueuedAction());
            }
        }

        public void ForceQueueAP(int amountAP)
        {
            QueuedActions.Add(GetAction<InventoryAction>()); // Use InventoryAction because it doesn't actually do anything in its TakeAction method
            QueuedAPs.Add(amountAP);
            StartCoroutine(TryTakeTurn());
        }

        public abstract IEnumerator GetNextQueuedAction();

        public abstract void TakeTurn();

        public virtual void SkipTurn()
        {
            if (MoveAction.IsMoving == false)
                Unit.UnitAnimator.StopMovingForward();
        }

        public virtual void FinishAction()
        {
            if (QueuedActions.Count == 0)
                return;
            
            QueuedActions.RemoveAt(0);
            if (QueuedAPs.Count > 0)
                QueuedAPs.RemoveAt(0);

            IsPerformingAction = false;

            GridSystemVisual.UpdateAttackGridVisual();
        }

        public void CancelActions() => StartCoroutine(CancelActions_Coroutine());

        IEnumerator CancelActions_Coroutine()
        {
            if (QueuedActions.Count > 0 && QueuedActions[0] is MoveAction == false)
            {
                while (IsPerformingAction)
                    yield return null;
            }
            else
            {
                if (MoveAction.FinalTargetGridPosition != Unit.GridPosition)
                    MoveAction.SetFinalTargetGridPosition(MoveAction.NextTargetGridPosition);
            }

            ClearActionQueue(true);
            SetTargetEnemyUnit(null);
            QueuedAttack = null;

            if (Unit.IsPlayer)
                Unit.UnitActionHandler.PlayerActionHandler.SetDefaultSelectedAction();
        }

        public void InterruptActions()
        {
            for (int i = QueuedActions.Count - 1; i >= 0; i--)
            {
                if (QueuedActions[i].IsInterruptable())
                {
                    QueuedActions.RemoveAt(i);
                    if (QueuedAPs.Count >= i + 1)
                        QueuedAPs.RemoveAt(i);
                }
            }
        }

        public void ClearActionQueue(bool stopMoveAnimation, bool forceClearAll = false)
        {
            for (int i = QueuedActions.Count - 1; i >= 0; i--)
            {
                // Certain actions, such as InventoryActions, are not meant to be cleared from the queue (unless forceClearAll is true, which happens when the Unit dies)
                if (forceClearAll || QueuedActions[i].CanBeClearedFromActionQueue())
                {
                    if (QueuedAttack == QueuedActions[i])
                        QueuedAttack = null;

                    QueuedActions.RemoveAt(i);
                    if (QueuedAPs.Count >= i + 1)
                        QueuedAPs.RemoveAt(i);
                }
            }

            IsPerformingAction = false;

            // If the Unit isn't moving, they might still be in a move animation, so cancel that
            if (stopMoveAnimation && MoveAction.IsMoving == false)
                Unit.UnitAnimator.StopMovingForward();
        }

        public void ClearAttackActions()
        {
            QueuedAttack = null;
            for (int i = QueuedActions.Count - 1; i >= 0; i--)
            {
                if (QueuedActions[i] is BaseAttackAction)
                {
                    QueuedActions.RemoveAt(i);
                    if (QueuedAPs.Count >= i + 1)
                        QueuedAPs.RemoveAt(i);
                }
            }
        }

        public bool AttackQueuedNext() => QueuedActions.Count > 0 && QueuedActions[0] is BaseAttackAction;
        #endregion

        #region Combat
        public bool IsInAttackRange(Unit targetUnit, bool defaultCombatActionsOnly)
        {
            if (defaultCombatActionsOnly)
            {
                if (Unit.UnitEquipment.RangedWeaponEquipped && GetAction<ShootAction>().IsValidAction() && Unit.SelectedAction is MeleeAction == false && GetAction<ShootAction>().IsInAttackRange(targetUnit, Unit.GridPosition, targetUnit.GridPosition))
                    return true;

                if ((Unit.SelectedAction is MeleeAction || Unit.UnitEquipment.MeleeWeaponEquipped || (Unit.Stats.CanFightUnarmed && (Unit.UnitEquipment.RangedWeaponEquipped == false || Unit.UnitEquipment.HasValidAmmunitionEquipped() == false))) && GetAction<MeleeAction>().IsValidAction() && GetAction<MeleeAction>().IsInAttackRange(targetUnit, Unit.GridPosition, targetUnit.GridPosition))
                    return true;
            }
            else
            {
                for (int i = 0; i < availableCombatActions.Count; i++)
                {
                    BaseAttackAction action = availableCombatActions[i];
                    if (action.IsValidAction() && Unit.Stats.HasEnoughEnergy(action.InitialEnergyCost()) && action.IsInAttackRange(targetUnit, Unit.GridPosition, targetUnit.GridPosition))
                        return true;
                }
            }
            return false;
        }

        public bool TryDodgeAttack(Unit attackingUnit, HeldItem weaponAttackingWith, BaseAttackAction attackAction, bool attackerUsingOffhand)
        {
            // If the attacker is in front of this Unit (greater chance to block)
            if (TurnAction.AttackerInFrontOfUnit(attackingUnit))
            {
                float random = Random.Range(0f, 1f);
                bool attackDodged = random <= Unit.Stats.DodgeChance(attackingUnit, weaponAttackingWith, attackAction, attackerUsingOffhand, false);
                if (attackDodged && weaponAttackingWith != null && weaponAttackingWith.CurrentHeldItemStance == HeldItemStance.SpearWall && Vector3.Distance(Unit.WorldPosition, attackingUnit.WorldPosition) <= LevelGrid.diaganolDistance)
                {
                    SpearWallAction spearWallAction = attackingUnit.UnitActionHandler.GetAction<SpearWallAction>();
                    if (spearWallAction != null)
                        spearWallAction.CancelAction();
                }
                return attackDodged;
            }
            // If the attacker is beside this Unit (less of a chance to block)
            else if (TurnAction.AttackerBesideUnit(attackingUnit))
            {
                float random = Random.Range(0f, 1f);
                bool attackDodged = random <= Unit.Stats.DodgeChance(attackingUnit, weaponAttackingWith, attackAction, attackerUsingOffhand, true);
                return attackDodged;
            }
            return false;
        }

        public bool TryBlockRangedAttack(Unit attackingUnit, HeldItem weaponAttackingWith, bool attackerUsingOffhand)
        {
            if (Unit.UnitEquipment.ShieldEquipped)
            {
                // If the attacker is in front of this Unit (greater chance to block)
                if (TurnAction.AttackerInFrontOfUnit(attackingUnit))
                {
                    float random = Random.Range(0f, 1f);
                    if (random <= Unit.Stats.ShieldBlockChance(Unit.UnitMeshManager.GetHeldShield(), attackingUnit, weaponAttackingWith, attackerUsingOffhand, false))
                    {
                        if (attackingUnit.UnitActionHandler.TargetUnits.ContainsKey(Unit) == false)
                            attackingUnit.UnitActionHandler.TargetUnits.Add(Unit, Unit.UnitMeshManager.GetHeldShield());
                        return true;
                    }
                }
                // If the attacker is beside this Unit (less of a chance to block)
                else if (TurnAction.AttackerBesideUnit(attackingUnit))
                {
                    float random = Random.Range(0f, 1f);
                    if (random <= Unit.Stats.ShieldBlockChance(Unit.UnitMeshManager.GetHeldShield(), attackingUnit, weaponAttackingWith, attackerUsingOffhand, true))
                    {
                        if (attackingUnit.UnitActionHandler.TargetUnits.ContainsKey(Unit) == false)
                            attackingUnit.UnitActionHandler.TargetUnits.Add(Unit, Unit.UnitMeshManager.GetHeldShield());
                        return true;
                    }
                }
            }

            if (attackingUnit.UnitActionHandler.TargetUnits.ContainsKey(Unit) == false)
                attackingUnit.UnitActionHandler.TargetUnits.Add(Unit, null);
            return false;
        }

        public bool TryBlockMeleeAttack(Unit attackingUnit, HeldItem weaponAttackingWith, bool attackerUsingOffhand)
        {
            float random;
            if (TurnAction.AttackerInFrontOfUnit(attackingUnit))
            {
                if (Unit.UnitEquipment.ShieldEquipped)
                {
                    // Try blocking with shield
                    random = Random.Range(0f, 1f);
                    if (random <= Unit.Stats.ShieldBlockChance(Unit.UnitMeshManager.GetHeldShield(), attackingUnit, weaponAttackingWith, attackerUsingOffhand, false))
                    {
                        if (attackingUnit.UnitActionHandler.TargetUnits.ContainsKey(Unit) == false)
                            attackingUnit.UnitActionHandler.TargetUnits.Add(Unit, Unit.UnitMeshManager.GetHeldShield());
                        return true;
                    }

                    // Still have a chance to block with weapon
                    if (Unit.UnitEquipment.MeleeWeaponEquipped)
                    {
                        random = Random.Range(0f, 1f);
                        if (random <= Unit.Stats.WeaponBlockChance(Unit.UnitMeshManager.GetPrimaryHeldMeleeWeapon(), attackingUnit, weaponAttackingWith, attackerUsingOffhand, false, true))
                        {
                            if (attackingUnit.UnitActionHandler.TargetUnits.ContainsKey(Unit) == false)
                                attackingUnit.UnitActionHandler.TargetUnits.Add(Unit, Unit.UnitMeshManager.GetPrimaryHeldMeleeWeapon());
                            return true;
                        }
                    }
                }
                else if (Unit.UnitEquipment.MeleeWeaponEquipped)
                {
                    if (Unit.UnitEquipment.IsDualWielding)
                    {
                        // Try blocking with right weapon
                        random = Random.Range(0f, 1f);
                        if (random <= Unit.Stats.WeaponBlockChance(Unit.UnitMeshManager.GetPrimaryHeldMeleeWeapon(), attackingUnit, weaponAttackingWith, attackerUsingOffhand, false, false) * Weapon.dualWieldPrimaryEfficiency)
                        {
                            if (attackingUnit.UnitActionHandler.TargetUnits.ContainsKey(Unit) == false)
                                attackingUnit.UnitActionHandler.TargetUnits.Add(Unit, Unit.UnitMeshManager.GetPrimaryHeldMeleeWeapon());
                            return true;
                        }

                        // Try blocking with left weapon
                        random = Random.Range(0f, 1f);
                        if (random <= Unit.Stats.WeaponBlockChance(Unit.UnitMeshManager.GetLeftHeldMeleeWeapon(), attackingUnit, weaponAttackingWith, attackerUsingOffhand, false, false) * Weapon.dualWieldSecondaryEfficiency)
                        {
                            if (attackingUnit.UnitActionHandler.TargetUnits.ContainsKey(Unit) == false)
                                attackingUnit.UnitActionHandler.TargetUnits.Add(Unit, Unit.UnitMeshManager.GetLeftHeldMeleeWeapon());
                            return true;
                        }
                    }
                    else
                    {
                        // Try blocking with only weapon
                        random = Random.Range(0f, 1f);
                        if (random <= Unit.Stats.WeaponBlockChance(Unit.UnitMeshManager.GetPrimaryHeldMeleeWeapon(), attackingUnit, weaponAttackingWith, attackerUsingOffhand, false, false))
                        {
                            if (attackingUnit.UnitActionHandler.TargetUnits.ContainsKey(Unit) == false)
                                attackingUnit.UnitActionHandler.TargetUnits.Add(Unit, Unit.UnitMeshManager.GetPrimaryHeldMeleeWeapon());
                            return true;
                        }
                    }
                }
            }
            else if (TurnAction.AttackerBesideUnit(attackingUnit))
            {
                if (Unit.UnitEquipment.ShieldEquipped)
                {
                    // Try blocking with shield
                    random = Random.Range(0f, 1f);
                    if (random <= Unit.Stats.ShieldBlockChance(Unit.UnitMeshManager.GetHeldShield(), attackingUnit, weaponAttackingWith, attackerUsingOffhand, true))
                    {
                        if (attackingUnit.UnitActionHandler.TargetUnits.ContainsKey(Unit) == false)
                            attackingUnit.UnitActionHandler.TargetUnits.Add(Unit, Unit.UnitMeshManager.GetHeldShield());
                        return true;
                    }

                    // Still have a chance to block with weapon
                    if (Unit.UnitEquipment.MeleeWeaponEquipped)
                    {
                        random = Random.Range(0f, 1f);
                        if (random <= Unit.Stats.WeaponBlockChance(Unit.UnitMeshManager.GetPrimaryHeldMeleeWeapon(), attackingUnit, weaponAttackingWith, attackerUsingOffhand, true, true))
                        {
                            if (attackingUnit.UnitActionHandler.TargetUnits.ContainsKey(Unit) == false)
                                attackingUnit.UnitActionHandler.TargetUnits.Add(Unit, Unit.UnitMeshManager.GetPrimaryHeldMeleeWeapon());
                            return true;
                        }
                    }
                }
                else if (Unit.UnitEquipment.MeleeWeaponEquipped)
                {
                    if (Unit.UnitEquipment.IsDualWielding)
                    {
                        // Try blocking with right weapon
                        random = Random.Range(0f, 1f);
                        if (random <= Unit.Stats.WeaponBlockChance(Unit.UnitMeshManager.GetPrimaryHeldMeleeWeapon(), attackingUnit, weaponAttackingWith, attackerUsingOffhand, true, false) * Weapon.dualWieldPrimaryEfficiency)
                        {
                            if (attackingUnit.UnitActionHandler.TargetUnits.ContainsKey(Unit) == false)
                                attackingUnit.UnitActionHandler.TargetUnits.Add(Unit, Unit.UnitMeshManager.GetPrimaryHeldMeleeWeapon());
                            return true;
                        }

                        // Try blocking with left weapon
                        random = Random.Range(0f, 1f);
                        if (random <= Unit.Stats.WeaponBlockChance(Unit.UnitMeshManager.GetLeftHeldMeleeWeapon(), attackingUnit, weaponAttackingWith, attackerUsingOffhand, true, false) * Weapon.dualWieldSecondaryEfficiency)
                        {
                            if (attackingUnit.UnitActionHandler.TargetUnits.ContainsKey(Unit) == false)
                                attackingUnit.UnitActionHandler.TargetUnits.Add(Unit, Unit.UnitMeshManager.GetLeftHeldMeleeWeapon());
                            return true;
                        }
                    }
                    else
                    {
                        // Try blocking with only weapon
                        random = Random.Range(0f, 1f);
                        if (random <= Unit.Stats.WeaponBlockChance(Unit.UnitMeshManager.GetPrimaryHeldMeleeWeapon(), attackingUnit, weaponAttackingWith, attackerUsingOffhand, true, false))
                        {
                            if (attackingUnit.UnitActionHandler.TargetUnits.ContainsKey(Unit) == false)
                                attackingUnit.UnitActionHandler.TargetUnits.Add(Unit, Unit.UnitMeshManager.GetPrimaryHeldMeleeWeapon());
                            return true;
                        }
                    }
                }
            }

            if (attackingUnit.UnitActionHandler.TargetUnits.ContainsKey(Unit) == false)
                attackingUnit.UnitActionHandler.TargetUnits.Add(Unit, null);
            return false;
        }
        #endregion

        public T GetAction<T>() where T : BaseAction
        {
            foreach (ActionType actionType in availableActionTypes)
            {
                Type targetType = actionType.GetActionType();
                if (typeof(T) == targetType)
                    return ActionsPool.GetAction(targetType, actionType, Unit) as T;
            }
            return null;
        }

        public BaseAction GetActionFromType(ActionType actionType)
        {
            for (int i = 0; i < availableActions.Count; i++)
            {
                if (availableActions[i].GetType() == actionType.GetActionType())
                    return availableActions[i];
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

        public bool ActionTypeIsAvailable(string actionName)
        {
            foreach (ActionType actionType in availableActionTypes)
            {
                if (actionType.ActionTypeName == actionName)
                    return true;
            }
            return false;
        }

        public void SetPreviousTargetEnemyGridPosition(GridPosition newGridPosition) => PreviousTargetEnemyGridPosition = newGridPosition;

        public virtual void SetTargetEnemyUnit(Unit target)
        {
            if (target != null && target.Health.IsDead)
            {
                TargetEnemyUnit = null;
                return;
            }

            TargetEnemyUnit = target;
            if (target != null)
                PreviousTargetEnemyGridPosition = target.GridPosition;
        }

        public void SetQueuedAttack(BaseAttackAction attackAction, GridPosition targetAttackGridPosition)
        {
            if (attackAction != null)
            {
                QueuedAttack = attackAction;
                QueuedAttack.SetTargetGridPosition(targetAttackGridPosition);
            }
            else
                QueuedAttack = null;
        }

        public void ClearQueuedAttack() => QueuedAttack = null;

        public void SetIsAttacking(bool isAttacking)
        {
            // Debug.Log(unit.name + " start attacking");
            this.IsAttacking = isAttacking;
        }

        public void SetCanPerformActions(bool canPerformActions) => this.CanPerformActions = canPerformActions;

        public NPCActionHandler NPCActionHandler => this as NPCActionHandler;
        public PlayerActionHandler PlayerActionHandler => this as PlayerActionHandler;

        public LayerMask AttackObstacleMask => attackObstacleMask;
        public LayerMask MoveObstacleMask => moveObstacleMask;

        public Transform ActionsParent => actionsParent;

        public List<ActionType> AvailableActionTypes => availableActionTypes;
        public List<BaseAction> AvailableActions => availableActions;
        public List<BaseAttackAction> AvailableCombatActions => availableCombatActions;
    }
}