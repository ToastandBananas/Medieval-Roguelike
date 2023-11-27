using System;
using System.Collections;
using GridSystem;
using InventorySystem;
using Utilities;
using UnitSystem.ActionSystem.UI;
using ContextMenu = GeneralUI.ContextMenu;
using UnityEngine;

namespace UnitSystem.ActionSystem
{
    public class PlayerActionHandler : UnitActionHandler
    {
        public event EventHandler OnSelectedActionChanged;

        public ActionType selectedActionType { get; private set; }

        float onClickActionBarCooldown;
        float onClickActionBarCooldownTime = 0.25f;

        public override void Awake()
        {
            base.Awake();

            canPerformActions = true;

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

            if (selectedActionType.GetAction(unit).IsDefaultAttackAction == false)
                SetDefaultSelectedAction();
        }

        public override void TakeTurn()
        {
            if (unit.IsMyTurn && !isPerformingAction && !unit.health.IsDead)
            {
                unit.vision.FindVisibleUnitsAndObjects();

                if (canPerformActions == false)
                {
                    TurnManager.Instance.FinishTurn(unit);
                    return;
                }
                else if (interactAction.targetInteractable != null)
                {
                    if (Vector3.Distance(unit.WorldPosition, interactAction.targetInteractable.GridPosition().WorldPosition) <= LevelGrid.diaganolDistance)
                        interactAction.QueueAction(interactAction.targetInteractable);
                }
                else if (queuedActions.Count == 0)
                {
                    // If the queued attack is not a default attack
                    if (queuedAttack != null && queuedAttack.IsDefaultAttackAction == false)
                    {
                        // If the target attack position is in range and there are valid units within the attack area
                        if (queuedAttack.IsInAttackRange(null, unit.GridPosition, queuedAttack.targetGridPosition) && queuedAttack.IsValidUnitInActionArea(queuedAttack.targetGridPosition))
                        {
                            if (queuedAttack.IsRangedAttackAction())
                            {
                                // If the target attack position is too close, cancel the Player's current action
                                if (Vector3.Distance(unit.WorldPosition, queuedAttack.targetGridPosition.WorldPosition) < unit.unitMeshManager.GetHeldRangedWeapon().ItemData.Item.Weapon.MinRange)
                                {
                                    CancelActions();
                                    return;
                                }
                            }

                            // Queue the attack action (target grid position has already been set in this case)
                            queuedAttack.QueueAction();
                        }
                        else // If there's no unit in the attack area or the target attack position is out of range, cancel the action
                        {
                            CancelActions();
                            return;
                        }
                    }
                    // If there's a target enemy and either an attack wasn't queued, or the queued attack is a default attack
                    else if (targetEnemyUnit != null)
                    {
                        // If the target enemy is dead, cancel the action
                        if (targetEnemyUnit.health.IsDead)
                        {
                            CancelActions();
                            return;
                        }

                        // Handle default ranged attack
                        if (unit.UnitEquipment.RangedWeaponEquipped && unit.UnitEquipment.HasValidAmmunitionEquipped())
                        {
                            // If the target enemy is too close, cancel the Player's current action
                            if (Vector3.Distance(unit.WorldPosition, targetEnemyUnit.WorldPosition) < unit.unitMeshManager.GetHeldRangedWeapon().ItemData.Item.Weapon.MinRange)
                            {
                                CancelActions();
                                return;
                            }
                            else if (GetAction<ShootAction>().IsInAttackRange(targetEnemyUnit, unit.GridPosition, targetEnemyUnit.GridPosition))
                            {
                                // Shoot the target enemy
                                ClearActionQueue(true);
                                if (unit.unitMeshManager.GetHeldRangedWeapon().IsLoaded)
                                    GetAction<ShootAction>().QueueAction(targetEnemyUnit);
                                else
                                    GetAction<ReloadAction>().QueueAction();
                            }
                            else // If they're out of the shoot range, move towards the enemy
                                moveAction.QueueAction(GetAction<ShootAction>().GetNearestAttackPosition(unit.GridPosition, targetEnemyUnit));
                        }
                        // Handle default melee attack
                        else if (unit.UnitEquipment.MeleeWeaponEquipped || unit.stats.CanFightUnarmed)
                        {
                            if (GetAction<MeleeAction>().IsInAttackRange(targetEnemyUnit, unit.GridPosition, targetEnemyUnit.GridPosition))
                            {
                                // Melee attack the target enemy
                                ClearActionQueue(false);
                                GetAction<MeleeAction>().QueueAction(targetEnemyUnit);
                            }
                            else // If they're out of melee range, move towards the enemy
                                moveAction.QueueAction(GetAction<MeleeAction>().GetNearestAttackPosition(unit.GridPosition, targetEnemyUnit));
                        }
                    }
                }

                if (queuedActions.Count > 0)
                    StartCoroutine(GetNextQueuedAction());
                else
                {
                    unit.UnblockCurrentPosition();
                    GridSystemVisual.UpdateAttackGridVisual();
                }
            }
        }

        public override void SkipTurn()
        {
            base.SkipTurn();

            lastQueuedAction = null;
            unit.stats.UseAP(unit.stats.APUntilTimeTick);
            TurnManager.Instance.FinishTurn(unit);
        }

        public override IEnumerator GetNextQueuedAction()
        {
            if (unit.health.IsDead)
            {
                ClearActionQueue(true, true);
                GridSystemVisual.HideGridVisual();
                yield break;
            }

            if (queuedActions.Count > 0 && queuedAPs.Count > 0 && !isPerformingAction)
            {
                ContextMenu.DisableContextMenu(true);

                while (isAttacking || unit.unitAnimator.beingKnockedBack)
                    yield return null;

                unit.stats.UseAP(queuedAPs[0]);

                if (queuedActions.Count > 0 && queuedAPs.Count > 0) // This can become cleared out after a time tick update
                {
                    isPerformingAction = true;
                    lastQueuedAction = queuedActions[0];
                    queuedActions[0].TakeAction();
                }
                else
                    CancelActions();
            }
        }

        public void AttackTarget() => StartCoroutine(AttackTarget_Coroutine());

        IEnumerator AttackTarget_Coroutine()
        {
            if (targetEnemyUnit == null)
                yield break;

            BaseAction selectedAction = selectedActionType.GetAction(unit);
            if (selectedAction is BaseAttackAction)
            {
                if (targetEnemyUnit.unitActionHandler.moveAction.isMoving) // Wait for the targetEnemyUnit to stop moving
                {
                    while (targetEnemyUnit != null && targetEnemyUnit.unitActionHandler.moveAction.isMoving)
                        yield return null;

                    if (targetEnemyUnit == null)
                    {
                        Debug.LogWarning("Target Unit became null during AttackTarget");
                        yield break;
                    }

                    if (selectedAction.BaseAttackAction.IsInAttackRange(targetEnemyUnit, unit.GridPosition, targetEnemyUnit.GridPosition) == false) // Check if they're now out of attack range
                    {
                        moveAction.QueueAction(selectedAction.BaseAttackAction.GetNearestAttackPosition(unit.GridPosition, targetEnemyUnit)); // Move to the nearest valid attack position
                        yield break;
                    }
                }

                selectedAction.BaseAttackAction.QueueAction(targetEnemyUnit);
            }
            else if (unit.UnitEquipment.RangedWeaponEquipped && unit.UnitEquipment.HasValidAmmunitionEquipped())
            {
                if (unit.unitMeshManager.GetHeldRangedWeapon().IsLoaded)
                {
                    if (targetEnemyUnit.unitActionHandler.moveAction.isMoving) // Wait for the targetEnemyUnit to stop moving
                    {
                        while (targetEnemyUnit != null && targetEnemyUnit.unitActionHandler.moveAction.isMoving)
                            yield return null;

                        if (targetEnemyUnit == null)
                        {
                            Debug.LogWarning("Target Unit became null during AttackTarget");
                            yield break;
                        }

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
            else if (unit.UnitEquipment.MeleeWeaponEquipped || unit.stats.CanFightUnarmed)
            {
                if (targetEnemyUnit.unitActionHandler.moveAction.isMoving) // Wait for the targetEnemyUnit to stop moving
                {
                    while (targetEnemyUnit != null && targetEnemyUnit.unitActionHandler.moveAction.isMoving)
                        yield return null;

                    if (targetEnemyUnit == null)
                    {
                        Debug.LogWarning("Target Unit became null during AttackTarget");
                        yield break;
                    }

                    if (GetAction<MeleeAction>().IsInAttackRange(targetEnemyUnit, unit.GridPosition, targetEnemyUnit.GridPosition) == false) // Check if they're now out of attack range
                    {
                        moveAction.QueueAction(selectedAction.BaseAttackAction.GetNearestAttackPosition(unit.GridPosition, targetEnemyUnit)); // Move to the nearest valid attack position
                        yield break;
                    }
                }

                GetAction<MeleeAction>().QueueAction(targetEnemyUnit);
            }
        }

        public BaseAction SelectedAction => selectedActionType.GetAction(unit);

        public void SetSelectedActionType(ActionType actionType)
        {
            selectedActionType = actionType;
            OnSelectedActionChanged?.Invoke(this, EventArgs.Empty);
        }

        public void SetDefaultSelectedAction()
        {
            SetSelectedActionType(FindActionTypeByName("MoveAction"));
            ActionSystemUI.SetSelectedActionSlot(null);
        }

        public bool DefaultActionIsSelected => SelectedAction is MoveAction;

        public void OnClick_ActionBarSlot(ActionType actionType)
        {
            if (InventoryUI.isDraggingItem || onClickActionBarCooldown < onClickActionBarCooldownTime)
                return;

            onClickActionBarCooldown = 0f;
            SetSelectedActionType(actionType);
            ActionSystemUI.SetSelectedActionSlot(ActionSystemUI.highlightedActionSlot);
        }
    }
}
