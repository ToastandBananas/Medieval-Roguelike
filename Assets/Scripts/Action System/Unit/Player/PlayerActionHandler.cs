using System;
using System.Collections;
using GridSystem;
using InventorySystem;
using UnitSystem;
using Utilities;
using GeneralUI;

namespace ActionSystem
{
    public class PlayerActionHandler : UnitActionHandler
    {
        public event EventHandler OnSelectedActionChanged;

        public override void Awake()
        {
            canPerformActions = true;
            base.Awake();
        }

        public override void QueueAction(BaseAction action, bool addToFrontOfQueue = false)
        {
            base.QueueAction(action, addToFrontOfQueue);

            if (selectedActionType.GetAction(unit).IsDefaultAttackAction() == false)
                SetDefaultSelectedAction();
        }

        public override void TakeTurn()
        {
            if (unit.IsMyTurn && unit.health.IsDead() == false)
            {
                unit.vision.FindVisibleUnitsAndObjects();

                if (canPerformActions == false)
                {
                    TurnManager.Instance.FinishTurn(unit);
                    return;
                }
                else if (GetAction<InteractAction>().targetInteractable != null)
                {
                    if (TacticsPathfindingUtilities.CalculateWorldSpaceDistance_XYZ(unit.GridPosition, GetAction<InteractAction>().targetInteractable.GridPosition()) <= 1.4f)
                        GetAction<InteractAction>().QueueAction(GetAction<InteractAction>().targetInteractable);
                }
                else if (queuedActions.Count == 0)
                {
                    // If the queued attack is not a default attack
                    if (queuedAttack != null && queuedAttack.IsDefaultAttackAction() == false)
                    {
                        // If the target attack position is in range and there are valid units within the attack area
                        if (queuedAttack.IsInAttackRange(null, unit.GridPosition, queuedAttack.targetGridPosition) && queuedAttack.IsValidUnitInActionArea(queuedAttack.targetGridPosition))
                        {
                            if (queuedAttack.IsRangedAttackAction())
                            {
                                Unit closestEnemy = unit.vision.GetClosestEnemy(true);

                                // If the closest enemy or target attack positions are too close, cancel the Player's current action
                                if (TacticsPathfindingUtilities.CalculateWorldSpaceDistance_XYZ(unit.GridPosition, queuedAttack.targetGridPosition) < unit.unitMeshManager.GetHeldRangedWeapon().itemData.Item.Weapon.MinRange)
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
                        if (targetEnemyUnit.health.IsDead())
                        {
                            CancelActions();
                            return;
                        }

                        // Handle default ranged attack
                        if (unit.UnitEquipment.RangedWeaponEquipped() && unit.UnitEquipment.HasValidAmmunitionEquipped())
                        {
                            // If the target enemy is too close, cancel the Player's current action
                            if (TacticsPathfindingUtilities.CalculateWorldSpaceDistance_XYZ(unit.GridPosition, targetEnemyUnit.GridPosition) < unit.unitMeshManager.GetHeldRangedWeapon().itemData.Item.Weapon.MinRange)
                            {
                                CancelActions();
                                return;
                            }
                            else if (GetAction<ShootAction>().IsInAttackRange(targetEnemyUnit))
                            {
                                // Shoot the target enemy
                                ClearActionQueue(true);
                                if (unit.unitMeshManager.GetHeldRangedWeapon().isLoaded)
                                    GetAction<ShootAction>().QueueAction(targetEnemyUnit);
                                else
                                    GetAction<ReloadAction>().QueueAction();
                            }
                            else // If they're out of the shoot range, move towards the enemy
                                moveAction.QueueAction(GetAction<ShootAction>().GetNearestAttackPosition(unit.GridPosition, targetEnemyUnit));
                        }
                        // Handle default melee attack
                        else if (unit.UnitEquipment.MeleeWeaponEquipped() || unit.stats.CanFightUnarmed)
                        {
                            if (GetAction<MeleeAction>().IsInAttackRange(targetEnemyUnit))
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
            if (unit.health.IsDead())
            {
                ClearActionQueue(true, true);
                GridSystemVisual.HideGridVisual();
                yield break;
            }

            if (queuedActions.Count > 0 && queuedAPs.Count > 0 && isPerformingAction == false)
            {
                ContextMenu.DisableContextMenu(true);

                while (isAttacking)
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

        public override void SetSelectedActionType(ActionType actionType)
        {
            base.SetSelectedActionType(actionType);
            OnSelectedActionChanged?.Invoke(this, EventArgs.Empty);
        }

        public void OnClick_SetSelectedActionType(ActionType actionType)
        {
            if (InventoryUI.isDraggingItem)
                return;

            SetSelectedActionType(actionType);
        }
    }
}
