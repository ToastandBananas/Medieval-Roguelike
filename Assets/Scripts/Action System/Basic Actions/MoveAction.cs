using Pathfinding;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using InteractableObjects;
using GridSystem;
using InventorySystem;
using UnitSystem.ActionSystem.UI;
using Utilities;

namespace UnitSystem.ActionSystem
{
    public class MoveAction : BaseAction
    {
        public delegate void OnMoveHandler();
        public event OnMoveHandler OnMove;

        public GridPosition finalTargetGridPosition { get; private set; }
        public GridPosition nextTargetGridPosition { get; private set; }
        public GridPosition lastGridPosition { get; private set; }
        Vector3 nextTargetPosition;
        Vector3 nextPathPosition;

        public bool isMoving { get; private set; }
        public bool canMove { get; private set; }

        List<Vector3> positionList = new List<Vector3>();
        int positionIndex;

        readonly float defaultMoveSpeed = 3.5f;
        float moveSpeed;

        readonly int defaultTileMoveCost = 200;

        void Start()
        {
            canMove = true;
            moveSpeed = defaultMoveSpeed;
            targetGridPosition = unit.GridPosition;
            lastGridPosition = unit.GridPosition;
        }

        public override void QueueAction(GridPosition finalTargetGridPosition)
        {
            targetGridPosition = finalTargetGridPosition;
            unit.unitActionHandler.QueueAction(this);
        }

        public override void TakeAction()
        {
            if (isMoving) return;

            StartAction();
            StartCoroutine(Move());
        }

        protected override void StartAction()
        {
            base.StartAction();

            if (unit.IsPlayer)
            {
                InventoryUI.CloseAllContainerUI();
                if (InventoryUI.npcInventoryActive)
                    InventoryUI.ToggleNPCInventory();
            }
        }

        IEnumerator Move()
        {
            // If there's no path
            if (positionList.Count == 0 || finalTargetGridPosition == unit.GridPosition)
            {
                CompleteAction();
                TurnManager.Instance.StartNextUnitsTurn(unit);
                yield break;
            }

            if (canMove == false)
            {
                Debug.Log($"{unit.name} tries to move, but they can't.");
                CompleteAction();
                TurnManager.Instance.StartNextUnitsTurn(unit);
                yield break;
            }

            // If the next position is obstructed
            if (LevelGrid.GridPositionObstructed(nextTargetGridPosition))
            {
                // Get a new path to the target position
                GetPathToTargetPosition(finalTargetGridPosition);

                // If we still can't find a path, just finish the action
                if (positionList.Count == 0)
                {
                    CompleteAction();
                    TurnManager.Instance.StartNextUnitsTurn(unit);
                    yield break;
                }

                nextTargetPosition = GetNextTargetPosition();
                nextTargetGridPosition.Set(nextTargetPosition);
            }

            if (nextTargetPosition == unit.WorldPosition || LevelGrid.GridPositionObstructed(nextTargetGridPosition))
            {
                CompleteAction();
                TurnManager.Instance.StartNextUnitsTurn(unit);
                yield break;
            }

            lastGridPosition = unit.GridPosition;
            OnMove?.Invoke();

            while (unit.unitAnimator.beingKnockedBack)
                yield return null;

            // Check for Opportunity Attacks
            for (int i = unit.unitsWhoCouldOpportunityAttackMe.Count - 1; i >= 0; i--)
            {
                Unit opportunityAttackingUnit = unit.unitsWhoCouldOpportunityAttackMe[i];
                if (opportunityAttackingUnit.health.IsDead)
                {
                    unit.unitsWhoCouldOpportunityAttackMe.RemoveAt(i);
                    continue;
                }

                if (unit.alliance.IsEnemy(opportunityAttackingUnit) == false)
                    continue;

                // Only melee Unit's can do an opportunity attack
                if (opportunityAttackingUnit.UnitEquipment.RangedWeaponEquipped || (opportunityAttackingUnit.UnitEquipment.MeleeWeaponEquipped == false && opportunityAttackingUnit.stats.CanFightUnarmed == false))
                    continue;

                // The enemy must be at least somewhat facing this Unit
                if (opportunityAttackingUnit.vision.IsDirectlyVisible(unit) == false || opportunityAttackingUnit.vision.TargetInOpportunityAttackViewAngle(unit.transform) == false)
                    continue;

                // Check if the Unit is starting out within the nearbyUnit's attack range
                MeleeAction opponentsMeleeAction = opportunityAttackingUnit.unitActionHandler.GetAction<MeleeAction>();
                if (opponentsMeleeAction.IsInAttackRange(unit) == false)
                    continue;

                // Check if the Unit is moving to a position outside of the nearbyUnit's attack range
                if (opponentsMeleeAction.IsInAttackRange(unit, opportunityAttackingUnit.GridPosition, nextTargetGridPosition))
                    continue;

                opponentsMeleeAction.DoOpportunityAttack(unit);

                while (opportunityAttackingUnit.unitActionHandler.isAttacking)
                    yield return null;
            }

            // This Unit can die during an opportunity attack
            if (unit.health.IsDead)
            {
                CompleteAction();
                yield break;
            }

            // Unblock the Unit's current position since they're about to move
            unit.UnblockCurrentPosition();
            isMoving = true;

            // Block the Next Position so that NPCs who are also currently looking for a path don't try to use the Next Position's tile
            unit.BlockAtPosition(nextTargetPosition);
            
            // Remove the Unit reference from it's current Grid Position and add the Unit to its next Grid Position
            LevelGrid.RemoveUnitAtGridPosition(unit.GridPosition);
            LevelGrid.AddUnitAtGridPosition(nextTargetGridPosition, unit);

            // Set the Unit's new grid position before they move so that other Unit's use that grid position when checking attack ranges and such
            unit.SetGridPosition(nextTargetGridPosition);

            // Start the next Unit's action before moving, that way their actions play out at the same time as this Unit's
            TurnManager.Instance.StartNextUnitsTurn(unit);

            ActionLineRenderer.Instance.HideLineRenderers();

            Vector3 nextPointOnPath = positionList[positionIndex];
            Direction directionToNextPosition;

            if (unit.IsPlayer || unit.unitMeshManager.IsVisibleOnScreen)
            {
                directionToNextPosition = GetDirectionToNextTargetPosition(nextPointOnPath);

                // Start rotating towards the target position
                unit.unitActionHandler.turnAction.SetTargetPosition(directionToNextPosition);
                unit.unitActionHandler.turnAction.RotateTowards_CurrentTargetPosition(false);

                // Get the next path position, not including the Y coordinate
                nextPathPosition = GetNextPathPosition_XZ(nextPointOnPath);

                float moveSpeedMultiplier = 1f;

                if (unit.IsNPC)
                {
                    moveSpeedMultiplier = 1.1f;
                    if (LevelGrid.IsDiagonal(unit.transform.position, nextTargetPosition))
                        moveSpeedMultiplier *= 1.4f;
                    if (nextTargetPosition.y != unit.transform.position.y)
                        moveSpeedMultiplier *= 2f;
                }

                unit.unitAnimator.StartMovingForward(); // Move animation

                bool moveUpchecked = false;
                bool moveDownChecked = false;
                bool moveAboveChecked = false;
                float stoppingDistance = 0.00625f;
                float distanceToTriggerStopAnimation = 0.75f;

                Vector3 unitPosition = unit.transform.position;
                Vector3 targetPosition = unitPosition;

                while (unit.unitAnimator.beingKnockedBack == false && Vector3.Distance(unit.transform.position, nextPathPosition) > stoppingDistance)
                {
                    unitPosition = unit.transform.position;

                    if (unit.unitAnimator.isDodging)
                    {
                        while (unit.unitAnimator.isDodging)
                            yield return null;
                    }

                    // If the next point on the path is above or below the Unit
                    if (Mathf.Abs(Mathf.Abs(nextPointOnPath.y) - Mathf.Abs(unitPosition.y)) > stoppingDistance)
                    {
                        // If the next path position is above the unit's current position
                        if (moveUpchecked == false && nextPointOnPath.y - unitPosition.y > 0f)
                        {
                            moveUpchecked = true;
                            targetPosition.Set(unitPosition.x, nextPointOnPath.y, unitPosition.z);
                            nextPathPosition.Set(nextPathPosition.x, nextPointOnPath.y, nextPathPosition.z);
                        }
                        // If the Unit is directly above the next path position
                        else if (moveDownChecked == false && nextPointOnPath.y - unitPosition.y < 0f && Mathf.Abs(nextPathPosition.x - unitPosition.x) < stoppingDistance && Mathf.Abs(nextPathPosition.z - unitPosition.z) < stoppingDistance)
                        {
                            moveDownChecked = true;
                            targetPosition = nextPathPosition;
                        }
                        // If the next path position is below the unit's current position
                        else if (moveAboveChecked == false && nextPointOnPath.y - unitPosition.y < 0f && (Mathf.Approximately(nextPathPosition.x, unitPosition.x) == false || Mathf.Approximately(nextPathPosition.z, unitPosition.z) == false))
                        {
                            moveAboveChecked = true;
                            targetPosition.Set(nextPointOnPath.x, unitPosition.y, nextPointOnPath.z);
                            nextPathPosition.Set(nextPathPosition.x, nextPointOnPath.y, nextPathPosition.z);
                        }
                    }
                    else // Otherwise, the target position is simply the next position on the Path
                        targetPosition = nextPathPosition;

                    // Determine if the Unit should stop their move animation
                    if (unit.unitActionHandler.targetEnemyUnit == null)
                    {
                        float distanceToFinalPosition = Vector3.Distance(unitPosition, LevelGrid.GetWorldPosition(finalTargetGridPosition));
                        if (distanceToFinalPosition <= distanceToTriggerStopAnimation)
                            unit.unitAnimator.StopMovingForward();
                    }

                    // Move to the target position
                    unit.transform.position = Vector3.MoveTowards(unit.transform.position, targetPosition, moveSpeed * moveSpeedMultiplier * Time.deltaTime);

                    yield return null;
                }
            }
            else // Move and rotate instantly while NPC is offscreen
            {
                directionToNextPosition = GetDirectionToNextTargetPosition(nextPointOnPath);
                unit.unitActionHandler.turnAction.RotateTowards_Direction(directionToNextPosition, true);

                nextPathPosition = nextTargetPosition;

                TurnManager.Instance.StartNextUnitsTurn(unit);
            }

            while (unit.unitAnimator.beingKnockedBack)
                yield return null;

            nextPathPosition.Set(Mathf.RoundToInt(nextPathPosition.x), nextPathPosition.y, Mathf.RoundToInt(nextPathPosition.z));
            unit.transform.position = nextPathPosition;
            unit.UpdateGridPosition();

            // If the Unit has reached the next point in the Path's position list, but hasn't reached the final position, increase the index
            if (positionIndex < positionList.Count && unit.transform.position == positionList[positionIndex] && unit.transform.position != finalTargetGridPosition.WorldPosition)
                positionIndex++;

            CompleteAction();
            TryQueueNextAction();

            // Check for newly visible Units
            unit.vision.FindVisibleUnitsAndObjects();
        }

        void TryQueueNextAction()
        {
            if (unit.IsPlayer)
            {
                // If the Player has a target Interactable
                InteractAction interactAction = unit.unitActionHandler.interactAction;
                if (interactAction.targetInteractable != null && TacticsUtilities.CalculateDistance_XYZ(unit.GridPosition, interactAction.targetInteractable.GridPosition()) <= 1.4f)
                    interactAction.QueueAction();
                // If the target enemy Unit died
                else if (unit.unitActionHandler.targetEnemyUnit != null && unit.unitActionHandler.targetEnemyUnit.health.IsDead)
                    unit.unitActionHandler.CancelActions();
                // If the Player is trying to attack an enemy and they are in range, stop moving and attack
                else if (unit.unitActionHandler.targetEnemyUnit != null && unit.unitActionHandler.IsInAttackRange(unit.unitActionHandler.targetEnemyUnit, true))
                {
                    unit.unitAnimator.StopMovingForward();
                    unit.unitActionHandler.PlayerActionHandler.AttackTarget();
                }
                // If the enemy moved positions, set the target position to the nearest possible attack position
                else if (unit.unitActionHandler.targetEnemyUnit != null && unit.unitActionHandler.previousTargetEnemyGridPosition != unit.unitActionHandler.targetEnemyUnit.GridPosition)
                    QueueMoveToTargetEnemy();
                // If the Player hasn't reached their destination, add the next move to the queue
                else if (unit.GridPosition != finalTargetGridPosition)
                    unit.unitActionHandler.QueueAction(this);
            }
            else // If NPC
            {
                // If they're trying to attack
                if (unit.stateController.currentState == State.Fight && unit.unitActionHandler.targetEnemyUnit != null)
                {
                    // If they're in range, stop moving and attack
                    if (unit.unitActionHandler.IsInAttackRange(unit.unitActionHandler.targetEnemyUnit, false))
                    {
                        unit.unitAnimator.StopMovingForward();
                        NPCActionHandler npcActionHandler = unit.unitActionHandler as NPCActionHandler;
                        npcActionHandler.ChooseCombatAction();
                    }
                    // If the enemy moved positions, set the target position to the nearest possible attack position
                    else if (unit.unitActionHandler.targetEnemyUnit != null && unit.unitActionHandler.targetEnemyUnit.health.IsDead == false && unit.unitActionHandler.previousTargetEnemyGridPosition != unit.unitActionHandler.targetEnemyUnit.GridPosition)
                        QueueMoveToTargetEnemy();
                }
            }
        }

        void QueueMoveToTargetEnemy()
        {
            unit.unitActionHandler.SetPreviousTargetEnemyGridPosition(unit.unitActionHandler.targetEnemyUnit.GridPosition);

            if (unit.IsPlayer && unit.unitActionHandler.PlayerActionHandler.SelectedAction is BaseAttackAction)
                unit.unitActionHandler.moveAction.QueueAction(unit.unitActionHandler.PlayerActionHandler.SelectedAction.BaseAttackAction.GetNearestAttackPosition(unit.GridPosition, unit.unitActionHandler.targetEnemyUnit));
            else if (unit.UnitEquipment.RangedWeaponEquipped && unit.UnitEquipment.HasValidAmmunitionEquipped())
                unit.unitActionHandler.moveAction.QueueAction(unit.unitActionHandler.GetAction<ShootAction>().GetNearestAttackPosition(unit.GridPosition, unit.unitActionHandler.targetEnemyUnit));
            else if (unit.UnitEquipment.MeleeWeaponEquipped || unit.stats.CanFightUnarmed)
                unit.unitActionHandler.moveAction.QueueAction(unit.unitActionHandler.GetAction<MeleeAction>().GetNearestAttackPosition(unit.GridPosition, unit.unitActionHandler.targetEnemyUnit));
            else
            {
                unit.unitActionHandler.SetTargetEnemyUnit(null);
                unit.unitActionHandler.SkipTurn();
                return;
            }
        }

        void GetPathToTargetPosition(GridPosition targetGridPosition)
        {
            Unit unitAtTargetGridPosition = LevelGrid.GetUnitAtGridPosition(targetGridPosition);
            if (unitAtTargetGridPosition != null && unitAtTargetGridPosition.health.IsDead == false)
            {
                unitAtTargetGridPosition.UnblockCurrentPosition();
                targetGridPosition = LevelGrid.GetNearestSurroundingGridPosition(targetGridPosition, unit.GridPosition, LevelGrid.diaganolDistance, false);
            }

            finalTargetGridPosition = targetGridPosition;

            unit.UnblockCurrentPosition();

            ABPath path = ABPath.Construct(unit.transform.position, LevelGrid.GetWorldPosition(targetGridPosition));
            path.traversalProvider = LevelGrid.DefaultTraversalProvider;

            // Schedule the path for calculation
            unit.seeker.StartPath(path);

            // Force the path request to complete immediately. This assumes the graph is small enough that this will not cause any lag
            path.BlockUntilCalculated();

            if (unit.IsNPC && path.vectorPath.Count == 0)
            {
                NPCActionHandler npcActionHandler = unit.unitActionHandler as NPCActionHandler;
                if (unit.stateController.currentState == State.Patrol)
                {
                    GridPosition patrolPointGridPosition = LevelGrid.GetGridPosition(npcActionHandler.PatrolPoints()[npcActionHandler.currentPatrolPointIndex]);
                    npcActionHandler.IncreasePatrolPointIndex();
                    finalTargetGridPosition = patrolPointGridPosition;
                }

                TurnManager.Instance.FinishTurn(unit);
                npcActionHandler.FinishAction();
                return;
            }

            positionList.Clear();
            positionIndex = 1;

            for (int i = 0; i < path.vectorPath.Count; i++)
            {
                positionList.Add(path.vectorPath[i]);
            }

            unit.BlockCurrentPosition();
            if (unitAtTargetGridPosition != null && unitAtTargetGridPosition.health.IsDead == false)
                unitAtTargetGridPosition.BlockCurrentPosition();
        }

        public override int ActionPointsCost()
        {
            int cost = defaultTileMoveCost;
            float floatCost = cost;

            if (positionIndex >= positionList.Count)
                positionIndex = positionList.Count - 1;

            // Only calculate a new path if the Unit's target position changed or if their path becomes obstructed
            if (targetGridPosition != finalTargetGridPosition || (positionList.Count > 0 && LevelGrid.GridPositionObstructed(LevelGrid.GetGridPosition(positionList[positionIndex]))))
                GetPathToTargetPosition(targetGridPosition);

            if (positionList.Count == 0)
                return cost;

            // Check for an Interactable on the next move position
            Vector3 nextPointOnPath = positionList[positionIndex];
            Vector3 unitPosition = unit.transform.position;
            Vector3 nextPathPosition = GetNextPathPosition_XZ(nextPointOnPath);

            // If the next path position is above the unit's current position
            if (nextPointOnPath.y - unitPosition.y > 0f)
                nextPathPosition = new Vector3(nextPathPosition.x, nextPointOnPath.y, nextPathPosition.z);
            // If the next path position is below the unit's current position
            else if (nextPointOnPath.y - unitPosition.y < 0f && (Mathf.Approximately(nextPathPosition.x, unitPosition.x) == false || Mathf.Approximately(nextPathPosition.z, unitPosition.z) == false))
                nextPathPosition = new Vector3(nextPathPosition.x, nextPointOnPath.y, nextPathPosition.z);

            // If there's an Interactable on the next path position
            GridPosition nextGridPosition = LevelGrid.GetGridPosition(nextPathPosition);
            if (LevelGrid.HasInteractableAtGridPosition(nextGridPosition))
            {
                Interactable interactable = LevelGrid.GetInteractableAtGridPosition(nextGridPosition);
                if (interactable is Door)
                {
                    Door door = interactable as Door;
                    if (door.isOpen == false)
                    {
                        unit.unitActionHandler.interactAction.QueueActionImmediately(door);
                        return 0;
                    }
                }
            }

            // Get the next Move position
            nextTargetPosition = GetNextTargetPosition();
            nextTargetGridPosition = LevelGrid.GetGridPosition(nextTargetPosition);

            float tileCostMultiplier = GetTileMoveCostMultiplier(nextTargetPosition);

            floatCost += floatCost * tileCostMultiplier;
            if (LevelGrid.IsDiagonal(unit.WorldPosition, nextTargetPosition))
                floatCost *= 1.4f;

            cost = Mathf.RoundToInt(floatCost);

            if (nextTargetPosition == unit.transform.position)
            {
                targetGridPosition = unit.GridPosition;

                if (unit.IsNPC)
                {
                    if (unit.stateController.currentState == State.Patrol)
                    {
                        NPCActionHandler npcActionHandler = unit.unitActionHandler as NPCActionHandler;
                        npcActionHandler.AssignNextPatrolTargetPosition();
                    }
                }
            }

            unit.BlockCurrentPosition();

            // if (unit.IsPlayer) Debug.Log("Move Cost (" + nextTargetPosition + "): " + Mathf.RoundToInt(cost * unit.stats.EncumbranceMoveCostModifier()));
            return Mathf.RoundToInt(cost * unit.stats.EncumbranceMoveCostModifier());
        }

        Vector3 GetNextPathPosition_XZ(Vector3 nextPointOnPath)
        {
            // Get the next path position, not including the Y coordinate
            if (Mathf.RoundToInt(nextPointOnPath.x) == Mathf.RoundToInt(unit.transform.position.x) && Mathf.RoundToInt(nextPointOnPath.z) > Mathf.RoundToInt(unit.transform.position.z))
                return new Vector3(unit.transform.position.x, unit.transform.position.y, unit.transform.position.z + 1);
            else if (Mathf.RoundToInt(nextPointOnPath.x) == Mathf.RoundToInt(unit.transform.position.x) && Mathf.RoundToInt(nextPointOnPath.z) < Mathf.RoundToInt(unit.transform.position.z))
                return new Vector3(unit.transform.position.x, unit.transform.position.y, unit.transform.position.z - 1);
            else if (Mathf.RoundToInt(nextPointOnPath.x) > Mathf.RoundToInt(unit.transform.position.x) && Mathf.RoundToInt(nextPointOnPath.z) == Mathf.RoundToInt(unit.transform.position.z))
                return new Vector3(unit.transform.position.x + 1, unit.transform.position.y, unit.transform.position.z);
            else if (Mathf.RoundToInt(nextPointOnPath.x) < Mathf.RoundToInt(unit.transform.position.x) && Mathf.RoundToInt(nextPointOnPath.z) == Mathf.RoundToInt(unit.transform.position.z))
                return new Vector3(unit.transform.position.x - 1, unit.transform.position.y, unit.transform.position.z);
            else if (Mathf.RoundToInt(nextPointOnPath.x) > Mathf.RoundToInt(unit.transform.position.x) && Mathf.RoundToInt(nextPointOnPath.z) > Mathf.RoundToInt(unit.transform.position.z))
                return new Vector3(unit.transform.position.x + 1, unit.transform.position.y, unit.transform.position.z + 1);
            else if (Mathf.RoundToInt(nextPointOnPath.x) < Mathf.RoundToInt(unit.transform.position.x) && Mathf.RoundToInt(nextPointOnPath.z) < Mathf.RoundToInt(unit.transform.position.z))
                return new Vector3(unit.transform.position.x - 1, unit.transform.position.y, unit.transform.position.z - 1);
            else if (Mathf.RoundToInt(nextPointOnPath.x) > Mathf.RoundToInt(unit.transform.position.x) && Mathf.RoundToInt(nextPointOnPath.z) < Mathf.RoundToInt(unit.transform.position.z))
                return new Vector3(unit.transform.position.x + 1, unit.transform.position.y, unit.transform.position.z - 1);
            else if (Mathf.RoundToInt(nextPointOnPath.x) < Mathf.RoundToInt(unit.transform.position.x) && Mathf.RoundToInt(nextPointOnPath.z) > Mathf.RoundToInt(unit.transform.position.z))
                return new Vector3(unit.transform.position.x - 1, unit.transform.position.y, unit.transform.position.z + 1);
            else
                return unit.transform.position;
        }

        Vector3 GetNextTargetPosition()
        {
            Vector3 nextPointOnPath = positionList[positionIndex];
            Vector3 nextTargetPosition;
            if (Mathf.Approximately(nextPointOnPath.y, unit.transform.position.y) == false)
                nextTargetPosition = nextPointOnPath;
            else if (Mathf.RoundToInt(nextPointOnPath.x) == Mathf.RoundToInt(unit.transform.position.x) && Mathf.RoundToInt(nextPointOnPath.z) > Mathf.RoundToInt(unit.transform.position.z)) // North
                nextTargetPosition = new Vector3(unit.transform.position.x, unit.transform.position.y, unit.transform.position.z + 1);
            else if (Mathf.RoundToInt(nextPointOnPath.x) == Mathf.RoundToInt(unit.transform.position.x) && Mathf.RoundToInt(nextPointOnPath.z) < Mathf.RoundToInt(unit.transform.position.z)) // South
                nextTargetPosition = new Vector3(unit.transform.position.x, unit.transform.position.y, unit.transform.position.z - 1);
            else if (Mathf.RoundToInt(nextPointOnPath.x) > Mathf.RoundToInt(unit.transform.position.x) && Mathf.RoundToInt(nextPointOnPath.z) == Mathf.RoundToInt(unit.transform.position.z)) // East
                nextTargetPosition = new Vector3(unit.transform.position.x + 1, unit.transform.position.y, unit.transform.position.z);
            else if (Mathf.RoundToInt(nextPointOnPath.x) < Mathf.RoundToInt(unit.transform.position.x) && Mathf.RoundToInt(nextPointOnPath.z) == Mathf.RoundToInt(unit.transform.position.z)) // West
                nextTargetPosition = new Vector3(unit.transform.position.x - 1, unit.transform.position.y, unit.transform.position.z);
            else if (Mathf.RoundToInt(nextPointOnPath.x) > Mathf.RoundToInt(unit.transform.position.x) && Mathf.RoundToInt(nextPointOnPath.z) > Mathf.RoundToInt(unit.transform.position.z)) // NorthEast
                nextTargetPosition = new Vector3(unit.transform.position.x + 1, unit.transform.position.y, unit.transform.position.z + 1);
            else if (Mathf.RoundToInt(nextPointOnPath.x) < Mathf.RoundToInt(unit.transform.position.x) && Mathf.RoundToInt(nextPointOnPath.z) < Mathf.RoundToInt(unit.transform.position.z)) // SouthWest
                nextTargetPosition = new Vector3(unit.transform.position.x - 1, unit.transform.position.y, unit.transform.position.z - 1);
            else if (Mathf.RoundToInt(nextPointOnPath.x) > Mathf.RoundToInt(unit.transform.position.x) && Mathf.RoundToInt(nextPointOnPath.z) < Mathf.RoundToInt(unit.transform.position.z)) // SouthEast
                nextTargetPosition = new Vector3(unit.transform.position.x + 1, unit.transform.position.y, unit.transform.position.z - 1);
            else if (Mathf.RoundToInt(nextPointOnPath.x) < Mathf.RoundToInt(unit.transform.position.x) && Mathf.RoundToInt(nextPointOnPath.z) > Mathf.RoundToInt(unit.transform.position.z)) // NorthWest
                nextTargetPosition = new Vector3(unit.transform.position.x - 1, unit.transform.position.y, unit.transform.position.z + 1);
            else // Debug.LogWarning("Next Position is " + unit.name + "'s current position...");
                nextTargetPosition = unit.transform.position;
            nextTargetPosition.Set(Mathf.RoundToInt(nextTargetPosition.x), nextTargetPosition.y, Mathf.RoundToInt(nextTargetPosition.z));
            return nextTargetPosition;
        }

        Direction GetDirectionToNextTargetPosition(Vector3 targetPosition)
        {
            if (Mathf.RoundToInt(targetPosition.x) == Mathf.RoundToInt(unit.transform.position.x) && Mathf.RoundToInt(targetPosition.z) > Mathf.RoundToInt(unit.transform.position.z))
                return Direction.North;
            else if (Mathf.RoundToInt(targetPosition.x) == Mathf.RoundToInt(unit.transform.position.x) && Mathf.RoundToInt(targetPosition.z) < Mathf.RoundToInt(unit.transform.position.z))
                return Direction.South;
            else if (Mathf.RoundToInt(targetPosition.x) > Mathf.RoundToInt(unit.transform.position.x) && Mathf.RoundToInt(targetPosition.z) == Mathf.RoundToInt(unit.transform.position.z))
                return Direction.East;
            else if (Mathf.RoundToInt(targetPosition.x) < Mathf.RoundToInt(unit.transform.position.x) && Mathf.RoundToInt(targetPosition.z) == Mathf.RoundToInt(unit.transform.position.z))
                return Direction.West;
            else if (Mathf.RoundToInt(targetPosition.x) > Mathf.RoundToInt(unit.transform.position.x) && Mathf.RoundToInt(targetPosition.z) > Mathf.RoundToInt(unit.transform.position.z))
                return Direction.NorthEast;
            else if (Mathf.RoundToInt(targetPosition.x) < Mathf.RoundToInt(unit.transform.position.x) && Mathf.RoundToInt(targetPosition.z) < Mathf.RoundToInt(unit.transform.position.z))
                return Direction.SouthWest;
            else if (Mathf.RoundToInt(targetPosition.x) > Mathf.RoundToInt(unit.transform.position.x) && Mathf.RoundToInt(targetPosition.z) < Mathf.RoundToInt(unit.transform.position.z))
                return Direction.SouthEast;
            else if (Mathf.RoundToInt(targetPosition.x) < Mathf.RoundToInt(unit.transform.position.x) && Mathf.RoundToInt(targetPosition.z) > Mathf.RoundToInt(unit.transform.position.z))
                return Direction.NorthWest;
            else
                return Direction.Center;
        }

        float GetTileMoveCostMultiplier(Vector3 tilePosition)
        {
            GraphNode node = AstarPath.active.GetNearest(tilePosition).node;
            // if (unit.IsPlayer) Debug.Log("Tag #" + node.Tag + " penalty is: ");

            for (int i = 0; i < unit.seeker.tagPenalties.Length; i++)
            {
                if (node.Tag == i)
                {
                    if (unit.seeker.tagPenalties[i] == 0)
                        return 0f;
                    else
                        return unit.seeker.tagPenalties[i] / 1000f;
                }
            }

            return 1f;
        }

        public override void CompleteAction()
        {
            base.CompleteAction();

            // Unblock the Unit's position, in case it's still their turn after this action ( so that the ActionLineRenderer will work). If not, it will be blocked again in the TurnManager's finish turn methods
            if (unit.IsPlayer)
                unit.UnblockCurrentPosition();
            else if (unit.health.IsDead == false)
                unit.BlockCurrentPosition();

            isMoving = false;
            unit.unitActionHandler.FinishAction();
        }

        public void SetMoveSpeed(int pooledAP)
        {
            if (unit.IsPlayer)
                return;

            if ((float)pooledAP / defaultTileMoveCost <= 1f)
                moveSpeed = defaultMoveSpeed;
            else
                moveSpeed = Mathf.FloorToInt((float)pooledAP / defaultTileMoveCost * defaultMoveSpeed);
        }

        public override bool IsValidAction()
        {
            // TODO: Test if the unit is immobile for whatever reason (broken legs, some sort of spell effect, etc.)
            return true;
        }

        public void SetNextPathPosition(Vector3 position) => nextPathPosition = position;

        public void SetIsMoving(bool isMoving) => this.isMoving = isMoving;

        public void SetCanMove(bool canMove) => this.canMove = canMove;

        public void SetFinalTargetGridPosition(GridPosition finalGridPosition) => finalTargetGridPosition = finalGridPosition;

        public override bool IsInterruptable() => false;

        public override bool CanQueueMultiple() => false;

        public override ActionBarSection ActionBarSection() => UI.ActionBarSection.None;

        public override bool ActionIsUsedInstantly() => false;

        public override bool CanBeClearedFromActionQueue() => true;

        public override int InitialEnergyCost() => 0;

        public override NPCAIAction GetNPCAIAction_ActionGridPosition(GridPosition actionGridPosition) => null;

        public override string TooltipDescription() => "";
    }
}
