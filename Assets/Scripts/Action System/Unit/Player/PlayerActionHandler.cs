using System;
using System.Collections;
using GridSystem;
using InventorySystem;
using UnitSystem.ActionSystem.UI;
using ContextMenu = GeneralUI.ContextMenu;
using UnityEngine;

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

        public override void QueueAction(BaseAction action, bool addToFrontOfQueue = false)
        {
            base.QueueAction(action, addToFrontOfQueue);

            if (SelectedActionType.GetAction(Unit).IsDefaultAttackAction == false)
                SetDefaultSelectedAction();
        }

        public override void TakeTurn()
        {
            if (Unit.IsMyTurn && !Unit.health.IsDead)
            {
                Unit.vision.FindVisibleUnitsAndObjects();

                if (CanPerformActions == false)
                {
                    TurnManager.Instance.FinishTurn(Unit);
                    return;
                }
                else if (InteractAction.targetInteractable != null)
                {
                    if (Vector3.Distance(Unit.WorldPosition, InteractAction.targetInteractable.GridPosition().WorldPosition) <= LevelGrid.diaganolDistance)
                        InteractAction.QueueAction(InteractAction.targetInteractable);
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
                                if (Vector3.Distance(Unit.WorldPosition, QueuedAttack.TargetGridPosition.WorldPosition) < Unit.unitMeshManager.GetHeldRangedWeapon().ItemData.Item.Weapon.MinRange)
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
                        if (TargetEnemyUnit.health.IsDead)
                        {
                            CancelActions();
                            return;
                        }

                        // Handle default ranged attack
                        if (Unit.UnitEquipment.RangedWeaponEquipped && Unit.UnitEquipment.HasValidAmmunitionEquipped())
                        {
                            // If the target enemy is too close, cancel the Player's current action
                            if (Vector3.Distance(Unit.WorldPosition, TargetEnemyUnit.WorldPosition) < Unit.unitMeshManager.GetHeldRangedWeapon().ItemData.Item.Weapon.MinRange)
                            {
                                CancelActions();
                                return;
                            }
                            else if (GetAction<ShootAction>().IsInAttackRange(TargetEnemyUnit, Unit.GridPosition, TargetEnemyUnit.GridPosition))
                            {
                                // Shoot the target enemy
                                ClearActionQueue(true);
                                if (Unit.unitMeshManager.GetHeldRangedWeapon().IsLoaded)
                                    GetAction<ShootAction>().QueueAction(TargetEnemyUnit);
                                else
                                    GetAction<ReloadAction>().QueueAction();
                            }
                            else // If they're out of the shoot range, move towards the enemy
                                MoveAction.QueueAction(GetAction<ShootAction>().GetNearestAttackPosition(Unit.GridPosition, TargetEnemyUnit));
                        }
                        // Handle default melee attack
                        else if (Unit.UnitEquipment.MeleeWeaponEquipped || Unit.stats.CanFightUnarmed)
                        {
                            if (GetAction<MeleeAction>().IsInAttackRange(TargetEnemyUnit, Unit.GridPosition, TargetEnemyUnit.GridPosition))
                            {
                                // Melee attack the target enemy
                                ClearActionQueue(false);
                                GetAction<MeleeAction>().QueueAction(TargetEnemyUnit);
                            }
                            else // If they're out of melee range, move towards the enemy
                                MoveAction.QueueAction(GetAction<MeleeAction>().GetNearestAttackPosition(Unit.GridPosition, TargetEnemyUnit));
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
            Unit.stats.UseAP(Unit.stats.APUntilTimeTick);
            TurnManager.Instance.FinishTurn(Unit);
        }

        public override IEnumerator GetNextQueuedAction()
        {
            if (Unit.health.IsDead)
            {
                ClearActionQueue(true, true);
                GridSystemVisual.HideGridVisual();
                yield break;
            }

            if (QueuedActions.Count > 0 && QueuedAPs.Count > 0 && !IsPerformingAction)
            {
                ContextMenu.DisableContextMenu(true);

                while (IsAttacking || Unit.unitAnimator.beingKnockedBack)
                    yield return null;

                if (QueuedActions.Count > 0 && QueuedAPs.Count > 0) // This can become cleared out after a time tick update
                {
                    Unit.stats.UseAP(QueuedAPs[0]);
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

            BaseAction selectedAction = SelectedActionType.GetAction(Unit);
            if (selectedAction is BaseAttackAction)
            {
                if (TargetEnemyUnit.unitActionHandler.MoveAction.IsMoving) // Wait for the targetEnemyUnit to stop moving
                {
                    while (TargetEnemyUnit != null && TargetEnemyUnit.unitActionHandler.MoveAction.IsMoving)
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
                if (Unit.unitMeshManager.GetHeldRangedWeapon().IsLoaded)
                {
                    if (TargetEnemyUnit.unitActionHandler.MoveAction.IsMoving) // Wait for the targetEnemyUnit to stop moving
                    {
                        while (TargetEnemyUnit != null && TargetEnemyUnit.unitActionHandler.MoveAction.IsMoving)
                            yield return null;

                        if (TargetEnemyUnit == null)
                        {
                            Debug.LogWarning("Target Unit became null during AttackTarget");
                            yield break;
                        }

                        if (GetAction<ShootAction>().IsInAttackRange(TargetEnemyUnit, Unit.GridPosition, TargetEnemyUnit.GridPosition) == false) // Check if they're now out of attack range
                        {
                            MoveAction.QueueAction(selectedAction.BaseAttackAction.GetNearestAttackPosition(Unit.GridPosition, TargetEnemyUnit)); // Move to the nearest valid attack position
                            yield break;
                        }
                    }

                    GetAction<ShootAction>().QueueAction(TargetEnemyUnit);
                }
                else
                    GetAction<ReloadAction>().QueueAction();
            }
            else if (Unit.UnitEquipment.MeleeWeaponEquipped || Unit.stats.CanFightUnarmed)
            {
                if (TargetEnemyUnit.unitActionHandler.MoveAction.IsMoving) // Wait for the targetEnemyUnit to stop moving
                {
                    while (TargetEnemyUnit != null && TargetEnemyUnit.unitActionHandler.MoveAction.IsMoving)
                        yield return null;

                    if (TargetEnemyUnit == null)
                    {
                        Debug.LogWarning("Target Unit became null during AttackTarget");
                        yield break;
                    }

                    if (GetAction<MeleeAction>().IsInAttackRange(TargetEnemyUnit, Unit.GridPosition, TargetEnemyUnit.GridPosition) == false) // Check if they're now out of attack range
                    {
                        MoveAction.QueueAction(selectedAction.BaseAttackAction.GetNearestAttackPosition(Unit.GridPosition, TargetEnemyUnit)); // Move to the nearest valid attack position
                        yield break;
                    }
                }

                GetAction<MeleeAction>().QueueAction(TargetEnemyUnit);
            }
        }

        public BaseAction SelectedAction => SelectedActionType != null ? SelectedActionType.GetAction(Unit) : null;

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

        public bool DefaultActionIsSelected => SelectedAction is MoveAction;

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
