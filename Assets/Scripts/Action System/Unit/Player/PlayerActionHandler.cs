using System;
using System.Collections;
using GridSystem;
using InventorySystem;
using UnitSystem.ActionSystem.UI;
using ContextMenu = GeneralUI.ContextMenu;
using UnityEngine;
using UnitSystem.ActionSystem.Actions;

namespace UnitSystem.ActionSystem
{
    public class PlayerActionHandler : UnitActionHandler
    {
        public event EventHandler OnSelectedActionChanged;

        public ActionType SelectedActionType { get; private set; }

        float onClickActionBarCooldown;
        readonly float onClickActionBarCooldownTime = 0.25f;

        public override void Awake()
        {
            base.Awake();

            CanPerformActions = true;

            // Default to the MoveAction
            SetDefaultSelectedAction();
        }

        void Update()
        {
            if (onClickActionBarCooldown < onClickActionBarCooldownTime)
                onClickActionBarCooldown += Time.deltaTime;
        }

        public override void QueueAction(Action_Base action, bool addToFrontOfQueue = false)
        {
            base.QueueAction(action, addToFrontOfQueue);

            if (SelectedActionType.GetAction(Unit).IsDefaultAttackAction == false)
                SetDefaultSelectedAction();
        }

        public override void TakeTurn()
        {
            if (Unit.IsMyTurn && !Unit.Health.IsDead)
            {
                Unit.Vision.FindVisibleUnitsAndObjects();

                if (CanPerformActions == false)
                {
                    TurnManager.Instance.FinishTurn(Unit);
                    return;
                }
                else if (InteractAction.TargetInteractable != null)
                {
                    if (Vector3.Distance(Unit.WorldPosition, InteractAction.TargetInteractable.GridPosition().WorldPosition) <= LevelGrid.diaganolDistance)
                        InteractAction.QueueAction(InteractAction.TargetInteractable);
                }
                else if (QueuedActions.Count == 0)
                {
                    // If the queued attack is not a default attack
                    if (QueuedAttack != null && QueuedAttack.IsDefaultAttackAction == false)
                    {
                        // If the target attack position is in range and there are valid units within the attack area
                        if (QueuedAttack.IsInAttackRange(null, Unit.GridPosition, QueuedAttack.TargetGridPosition) && QueuedAttack.IsValidUnitInActionArea(QueuedAttack.TargetGridPosition))
                        {
                            if (QueuedAttack.IsRangedAttackAction())
                            {
                                // If the target attack position is too close, cancel the Player's current action
                                if (Vector3.Distance(Unit.WorldPosition, QueuedAttack.TargetGridPosition.WorldPosition) < Unit.UnitMeshManager.GetHeldRangedWeapon().ItemData.Item.Weapon.MinRange)
                                {
                                    CancelActions();
                                    return;
                                }
                            }

                            // Queue the attack action (target grid position has already been set in this case)
                            QueuedAttack.QueueAction();
                        }
                        else // If there's no unit in the attack area or the target attack position is out of range, cancel the action
                        {
                            CancelActions();
                            return;
                        }
                    }
                    // If there's a target enemy and either an attack wasn't queued, or the queued attack is a default attack
                    else if (TargetEnemyUnit != null)
                    {
                        // If the target enemy is dead, cancel the action
                        if (TargetEnemyUnit.Health.IsDead)
                        {
                            CancelActions();
                            return;
                        }

                        // Handle default ranged attack
                        if (Unit.UnitEquipment.RangedWeaponEquipped && Unit.UnitEquipment.HasValidAmmunitionEquipped())
                        {
                            // If the target enemy is too close, cancel the Player's current action
                            if (Vector3.Distance(Unit.WorldPosition, TargetEnemyUnit.WorldPosition) < Unit.UnitMeshManager.GetHeldRangedWeapon().ItemData.Item.Weapon.MinRange)
                            {
                                CancelActions();
                                return;
                            }
                            else if (GetAction<Action_Shoot>().IsInAttackRange(TargetEnemyUnit, Unit.GridPosition, TargetEnemyUnit.GridPosition))
                            {
                                // Shoot the target enemy
                                ClearActionQueue(true);
                                if (Unit.UnitMeshManager.GetHeldRangedWeapon().IsLoaded)
                                    GetAction<Action_Shoot>().QueueAction(TargetEnemyUnit);
                                else
                                    GetAction<Action_Reload>().QueueAction();
                            }
                            else // If they're out of the shoot range, move towards the enemy
                                MoveAction.QueueAction(GetAction<Action_Shoot>().GetNearestAttackPosition(Unit.GridPosition, TargetEnemyUnit));
                        }
                        // Handle default melee attack
                        else if (Unit.UnitEquipment.MeleeWeaponEquipped || Unit.Stats.CanFightUnarmed)
                        {
                            if (GetAction<Action_Melee>().IsInAttackRange(TargetEnemyUnit, Unit.GridPosition, TargetEnemyUnit.GridPosition))
                            {
                                // Melee attack the target enemy
                                ClearActionQueue(false);
                                GetAction<Action_Melee>().QueueAction(TargetEnemyUnit);
                            }
                            else // If they're out of melee range, move towards the enemy
                                MoveAction.QueueAction(GetAction<Action_Melee>().GetNearestAttackPosition(Unit.GridPosition, TargetEnemyUnit));
                        }
                    }
                }

                if (QueuedActions.Count > 0)
                    StartCoroutine(GetNextQueuedAction());
                else
                {
                    Unit.UnblockCurrentPosition();
                    GridSystemVisual.UpdateAttackGridVisual();
                }
            }
        }

        public override void SkipTurn()
        {
            base.SkipTurn();

            LastQueuedAction = null;
            Unit.Stats.UseAP(Unit.Stats.APUntilTimeTick);
            TurnManager.Instance.FinishTurn(Unit);
        }

        public override IEnumerator GetNextQueuedAction()
        {
            if (Unit.Health.IsDead)
            {
                ClearActionQueue(true, true);
                GridSystemVisual.HideGridVisual();
                yield break;
            }

            if (QueuedActions.Count > 0 && QueuedAPs.Count > 0 && !IsPerformingAction)
            {
                ContextMenu.DisableContextMenu(true);

                while (IsAttacking || Unit.UnitAnimator.beingKnockedBack)
                    yield return null;

                if (QueuedActions.Count > 0 && QueuedAPs.Count > 0) // This can become cleared out after a time tick update
                {
                    Unit.Stats.UseAP(QueuedAPs[0]);
                    IsPerformingAction = true;
                    LastQueuedAction = QueuedActions[0];
                    QueuedActions[0].TakeAction();
                }
                else
                    CancelActions();
            }
        }

        public void AttackTarget() => StartCoroutine(AttackTarget_Coroutine());

        IEnumerator AttackTarget_Coroutine()
        {
            if (TargetEnemyUnit == null)
                yield break;

            Action_Base selectedAction = SelectedActionType.GetAction(Unit);
            if (selectedAction is Action_BaseAttack)
            {
                if (TargetEnemyUnit.UnitActionHandler.MoveAction.IsMoving) // Wait for the targetEnemyUnit to stop moving
                {
                    while (TargetEnemyUnit != null && TargetEnemyUnit.UnitActionHandler.MoveAction.IsMoving)
                        yield return null;

                    if (TargetEnemyUnit == null)
                    {
                        Debug.LogWarning("Target Unit became null during AttackTarget");
                        yield break;
                    }

                    if (selectedAction.BaseAttackAction.IsInAttackRange(TargetEnemyUnit, Unit.GridPosition, TargetEnemyUnit.GridPosition) == false) // Check if they're now out of attack range
                    {
                        MoveAction.QueueAction(selectedAction.BaseAttackAction.GetNearestAttackPosition(Unit.GridPosition, TargetEnemyUnit)); // Move to the nearest valid attack position
                        yield break;
                    }
                }

                selectedAction.BaseAttackAction.QueueAction(TargetEnemyUnit);
            }
            else if (Unit.UnitEquipment.RangedWeaponEquipped && Unit.UnitEquipment.HasValidAmmunitionEquipped())
            {
                if (Unit.UnitMeshManager.GetHeldRangedWeapon().IsLoaded)
                {
                    if (TargetEnemyUnit.UnitActionHandler.MoveAction.IsMoving) // Wait for the targetEnemyUnit to stop moving
                    {
                        while (TargetEnemyUnit != null && TargetEnemyUnit.UnitActionHandler.MoveAction.IsMoving)
                            yield return null;

                        if (TargetEnemyUnit == null)
                        {
                            Debug.LogWarning("Target Unit became null during AttackTarget");
                            yield break;
                        }

                        if (GetAction<Action_Shoot>().IsInAttackRange(TargetEnemyUnit, Unit.GridPosition, TargetEnemyUnit.GridPosition) == false) // Check if they're now out of attack range
                        {
                            MoveAction.QueueAction(selectedAction.BaseAttackAction.GetNearestAttackPosition(Unit.GridPosition, TargetEnemyUnit)); // Move to the nearest valid attack position
                            yield break;
                        }
                    }

                    GetAction<Action_Shoot>().QueueAction(TargetEnemyUnit);
                }
                else
                    GetAction<Action_Reload>().QueueAction();
            }
            else if (Unit.UnitEquipment.MeleeWeaponEquipped || Unit.Stats.CanFightUnarmed)
            {
                if (TargetEnemyUnit.UnitActionHandler.MoveAction.IsMoving) // Wait for the targetEnemyUnit to stop moving
                {
                    while (TargetEnemyUnit != null && TargetEnemyUnit.UnitActionHandler.MoveAction.IsMoving)
                        yield return null;

                    if (TargetEnemyUnit == null)
                    {
                        Debug.LogWarning("Target Unit became null during AttackTarget");
                        yield break;
                    }

                    if (GetAction<Action_Melee>().IsInAttackRange(TargetEnemyUnit, Unit.GridPosition, TargetEnemyUnit.GridPosition) == false) // Check if they're now out of attack range
                    {
                        MoveAction.QueueAction(selectedAction.BaseAttackAction.GetNearestAttackPosition(Unit.GridPosition, TargetEnemyUnit)); // Move to the nearest valid attack position
                        yield break;
                    }
                }

                GetAction<Action_Melee>().QueueAction(TargetEnemyUnit);
            }
        }

        public Action_Base SelectedAction => SelectedActionType != null ? SelectedActionType.GetAction(Unit) : null;

        public void SetSelectedActionType(ActionType actionType, bool invokeActionChangedDelegate)
        {
            SelectedActionType = actionType;
            if (invokeActionChangedDelegate)
                OnSelectedActionChanged?.Invoke(this, EventArgs.Empty);
        }

        public void SetDefaultSelectedAction()
        {
            SetSelectedActionType(MoveAction.ActionType, true);
            ActionSystemUI.SetSelectedActionSlot(null);
            ContextMenu.DisableContextMenu(true);
        }

        public bool DefaultActionIsSelected => SelectedAction is Action_Move;

        public void OnClick_ActionBarSlot(ActionType actionType)
        {
            if (InventoryUI.isDraggingItem || onClickActionBarCooldown < onClickActionBarCooldownTime)
                return;

            onClickActionBarCooldown = 0f;
            ActionSystemUI.SetSelectedActionSlot(ActionSystemUI.HighlightedActionSlot);
            SetSelectedActionType(actionType, true);
        }
    }
}
