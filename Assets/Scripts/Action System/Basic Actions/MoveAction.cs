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

        public GridPosition FinalTargetGridPosition { get; private set; }
        public GridPosition NextTargetGridPosition { get; private set; }
        public GridPosition LastGridPosition { get; private set; }
        Vector3 nextTargetPosition;

        public bool AboutToMove { get; private set; }
        public bool IsMoving { get; private set; }
        public bool CanMove { get; private set; }

        List<Vector3> positionList = new();
        int positionIndex;

        readonly float defaultMoveSpeed = 3.5f;
        readonly float defaultParabolaMoveSpeed = 20f;
        float moveSpeedMultiplier = 1f;
        float travelDistanceMultiplier = 1f;
        float moveSpeed;

        readonly int defaultTileMoveCost = 200;

        void Start()
        {
            CanMove = true;
            moveSpeed = defaultMoveSpeed;
            TargetGridPosition = Unit.GridPosition;
            LastGridPosition = Unit.GridPosition;
        }

        public override void QueueAction(GridPosition finalTargetGridPosition)
        {
            TargetGridPosition = finalTargetGridPosition;
            Unit.unitActionHandler.QueueAction(this);
        }

        public override void TakeAction()
        {
            if (IsMoving) return;

            StartAction();
            StartCoroutine(Move());
        }

        protected override void StartAction()
        {
            base.StartAction();

            if (Unit.IsPlayer)
            {
                InventoryUI.CloseAllContainerUI();
                if (InventoryUI.npcInventoryActive)
                    InventoryUI.ToggleNPCInventory();
            }
        }

        IEnumerator Move()
        {
            // If there's no path
            if (positionList.Count == 0 || FinalTargetGridPosition == Unit.GridPosition)
            {
                CompleteAction();
                TurnManager.Instance.StartNextUnitsTurn(Unit);
                yield break;
            }

            if (CanMove == false)
            {
                Debug.Log($"{Unit.name} tries to move, but they can't.");
                CompleteAction();
                TurnManager.Instance.StartNextUnitsTurn(Unit);
                yield break;
            }

            // If the next position is obstructed
            if (LevelGrid.GridPositionObstructed(NextTargetGridPosition))
            {
                // Get a new path to the target position
                GetPathToTargetPosition(FinalTargetGridPosition);

                // If we still can't find a path, just finish the action
                if (positionList.Count == 0)
                {
                    CompleteAction();
                    TurnManager.Instance.StartNextUnitsTurn(Unit);
                    yield break;
                }

                nextTargetPosition = GetNextTargetPosition();
                NextTargetGridPosition.Set(nextTargetPosition);
            }

            if (nextTargetPosition == Unit.WorldPosition || LevelGrid.GridPositionObstructed(NextTargetGridPosition))
            {
                CompleteAction();
                TurnManager.Instance.StartNextUnitsTurn(Unit);
                yield break;
            }

            LastGridPosition = Unit.GridPosition;
            OnMove?.Invoke();

            AboutToMove = true;

            while (Unit.unitAnimator.beingKnockedBack)
                yield return null;

            // Check for Opportunity Attacks & Spear Wall attacks
            for (int i = Unit.unitsWhoCouldOpportunityAttackMe.Count - 1; i >= 0; i--)
            {
                Unit opportunityAttackingUnit = Unit.unitsWhoCouldOpportunityAttackMe[i];
                if (opportunityAttackingUnit.health.IsDead)
                {
                    Unit.unitsWhoCouldOpportunityAttackMe.RemoveAt(i);
                    continue;
                }

                if (Unit.alliance.IsEnemy(opportunityAttackingUnit) == false)
                    continue;

                // Only melee Unit's can do an opportunity attack
                if (opportunityAttackingUnit.UnitEquipment.RangedWeaponEquipped || (opportunityAttackingUnit.UnitEquipment.MeleeWeaponEquipped == false && opportunityAttackingUnit.stats.CanFightUnarmed == false))
                    continue;

                // The enemy must be at least somewhat facing this Unit
                if (opportunityAttackingUnit.vision.IsDirectlyVisible(Unit) == false || opportunityAttackingUnit.vision.TargetInOpportunityAttackViewAngle(Unit.transform) == false)
                    continue;

                // Check if the Unit is starting out within the nearbyUnit's attack range
                MeleeAction opponentsMeleeAction = opportunityAttackingUnit.unitActionHandler.GetAction<MeleeAction>();
                if (opponentsMeleeAction.IsInAttackRange(Unit) == false)
                    continue;

                // Check if the Unit is moving to a position outside of the nearbyUnit's attack range
                if (opponentsMeleeAction.IsInAttackRange(Unit, opportunityAttackingUnit.GridPosition, NextTargetGridPosition))
                {
                    opportunityAttackingUnit.opportunityAttackTrigger.OnEnemyUnitMoved(Unit, NextTargetGridPosition);
                    continue;
                }

                opponentsMeleeAction.DoOpportunityAttack(Unit);

                while (opportunityAttackingUnit.unitActionHandler.IsAttacking)
                    yield return null;
            }

            // This Unit can die during an opportunity attack
            if (Unit.health.IsDead)
            {
                CompleteAction();
                yield break;
            }

            // Unblock the Unit's current position since they're about to move
            IsMoving = true;
            Unit.UnblockCurrentPosition();

            // Block the Next Position so that NPCs who are also currently looking for a path don't try to use the Next Position's tile
            Unit.BlockAtPosition(nextTargetPosition);
            
            // Remove the Unit reference from it's current Grid Position and add the Unit to its next Grid Position
            LevelGrid.RemoveUnitAtGridPosition(Unit.GridPosition);
            LevelGrid.AddUnitAtGridPosition(NextTargetGridPosition, Unit);

            // Set the Unit's new grid position before they move so that other Unit's use that grid position when checking attack ranges and such
            Unit.SetGridPosition(NextTargetGridPosition);

            // Start the next Unit's action before moving, that way their actions play out at the same time as this Unit's
            TurnManager.Instance.StartNextUnitsTurn(Unit);

            ActionLineRenderer.Instance.HideLineRenderers();

            Vector3 nextPointOnPath = positionList[positionIndex];
            Direction directionToNextPosition;

            if (Unit.IsPlayer || Unit.unitMeshManager.IsVisibleOnScreen)
            {
                Vector3 unitStartPosition = Unit.transform.position;
                directionToNextPosition = GetDirectionToNextTargetPosition(nextPointOnPath);

                // Start rotating towards the target position
                Unit.unitActionHandler.TurnAction.SetTargetPosition(directionToNextPosition);
                Unit.unitActionHandler.TurnAction.RotateTowards_CurrentTargetPosition(false);

                float heightDifference = nextTargetPosition.y - unitStartPosition.y;
                if (heightDifference == 0f) moveSpeed = defaultMoveSpeed;
                else moveSpeed = defaultParabolaMoveSpeed;

                float finalMoveSpeed = moveSpeed * travelDistanceMultiplier;
                if (Unit.IsNPC)
                {
                    moveSpeedMultiplier = 1.1f;
                    finalMoveSpeed *= moveSpeedMultiplier;
                    if (LevelGrid.IsDiagonal(unitStartPosition, nextTargetPosition))
                        finalMoveSpeed *= 1.4f;

                    if (heightDifference != 0f)
                        finalMoveSpeed += finalMoveSpeed * Mathf.Abs(heightDifference);
                }

                Unit.unitAnimator.StartMovingForward(); // Move animation

                float stoppingDistance = 0.00625f;
                float distanceToTriggerStopAnimation = 0.75f;

                float arcMultiplier = 1.6f;
                if (heightDifference < 0f) // If moving down
                    arcMultiplier *= 2f;
                else // If moving up
                    arcMultiplier += arcMultiplier * heightDifference;

                float arcHeight = MathParabola.CalculateParabolaArcHeight(unitStartPosition, nextTargetPosition) * arcMultiplier;
                float animationTime = 0f;
                
                while (!Unit.unitAnimator.beingKnockedBack && Vector3.Distance(Unit.transform.position, nextTargetPosition) > stoppingDistance)
                {
                    while (Unit.unitAnimator.isDodging)
                        yield return null;

                    // If the target position is on the same Y coordinate, just move directly towards it
                    if (heightDifference == 0f)
                        Unit.transform.position = Vector3.MoveTowards(Unit.transform.position, nextTargetPosition, finalMoveSpeed * Time.deltaTime);
                    else // If the target position is higher or lower than the Unit, use a parabola movement animation
                    {
                        animationTime += finalMoveSpeed * Time.deltaTime;
                        Unit.transform.position = MathParabola.Parabola(unitStartPosition, nextTargetPosition, arcHeight, animationTime / 5f);

                        if (heightDifference < 0f && Unit.transform.position.y < nextTargetPosition.y)
                            break;
                        else if (heightDifference > 0f && Mathf.Approximately(Unit.transform.position.x, nextTargetPosition.x) && Mathf.Approximately(Unit.transform.position.z, nextTargetPosition.z) && Unit.transform.position.y < nextTargetPosition.y)
                            break;
                    }

                    // Determine if the Unit should stop their move animation
                    if (Unit.unitActionHandler.TargetEnemyUnit == null)
                    {
                        float distanceToFinalPosition = Vector3.Distance(Unit.transform.position, LevelGrid.GetWorldPosition(FinalTargetGridPosition));
                        if (distanceToFinalPosition <= distanceToTriggerStopAnimation)
                            Unit.unitAnimator.StopMovingForward();
                    }

                    yield return null;
                }
            }
            else // Move and rotate instantly while NPC is offscreen
            {
                directionToNextPosition = GetDirectionToNextTargetPosition(nextPointOnPath);
                Unit.unitActionHandler.TurnAction.RotateTowards_Direction(directionToNextPosition, true);

                TurnManager.Instance.StartNextUnitsTurn(Unit);
            }

            while (Unit.unitAnimator.beingKnockedBack)
                yield return null;

            Unit.transform.position = nextTargetPosition;
            Unit.UpdateGridPosition();

            // If the Unit has reached the next point in the Path's position list, but hasn't reached the final position, increase the index
            if (positionIndex < positionList.Count && Unit.transform.position == positionList[positionIndex] && Unit.transform.position != FinalTargetGridPosition.WorldPosition)
                positionIndex++;

            CompleteAction();
            TryQueueNextAction();

            // Check for newly visible Units
            Unit.vision.FindVisibleUnitsAndObjects();
        }

        void TryQueueNextAction()
        {
            if (Unit.IsPlayer)
            {
                // If the Player has a target Interactable
                InteractAction interactAction = Unit.unitActionHandler.InteractAction;
                if (interactAction.targetInteractable != null && Vector3.Distance(Unit.WorldPosition, interactAction.targetInteractable.GridPosition().WorldPosition) <= LevelGrid.diaganolDistance)
                    interactAction.QueueAction();
                // If the target enemy Unit died
                else if (Unit.unitActionHandler.TargetEnemyUnit != null && Unit.unitActionHandler.TargetEnemyUnit.health.IsDead)
                    Unit.unitActionHandler.CancelActions();
                // If the Player is trying to attack an enemy and they are in range, stop moving and attack
                else if (Unit.unitActionHandler.TargetEnemyUnit != null && Unit.unitActionHandler.IsInAttackRange(Unit.unitActionHandler.TargetEnemyUnit, true))
                {
                    Unit.unitAnimator.StopMovingForward();
                    Unit.unitActionHandler.PlayerActionHandler.AttackTarget();
                }
                // If the enemy moved positions, set the target position to the nearest possible attack position
                else if (Unit.unitActionHandler.TargetEnemyUnit != null && Unit.unitActionHandler.PreviousTargetEnemyGridPosition != Unit.unitActionHandler.TargetEnemyUnit.GridPosition)
                    QueueMoveToTargetEnemy();
                // If the Player hasn't reached their destination, add the next move to the queue
                else if (Unit.GridPosition != FinalTargetGridPosition)
                    Unit.unitActionHandler.QueueAction(this);
            }
            else // If NPC
            {
                // If they're trying to attack
                if (Unit.stateController.currentState == State.Fight && Unit.unitActionHandler.TargetEnemyUnit != null)
                {
                    // If they're in range, stop moving and attack
                    if (Unit.unitActionHandler.IsInAttackRange(Unit.unitActionHandler.TargetEnemyUnit, false))
                    {
                        Unit.unitAnimator.StopMovingForward();
                        NPCActionHandler npcActionHandler = Unit.unitActionHandler as NPCActionHandler;
                        npcActionHandler.ChooseCombatAction();
                    }
                    // If the enemy moved positions, set the target position to the nearest possible attack position
                    else if (Unit.unitActionHandler.TargetEnemyUnit != null && Unit.unitActionHandler.TargetEnemyUnit.health.IsDead == false && Unit.unitActionHandler.PreviousTargetEnemyGridPosition != Unit.unitActionHandler.TargetEnemyUnit.GridPosition)
                        QueueMoveToTargetEnemy();
                }
            }
        }

        void QueueMoveToTargetEnemy()
        {
            Unit.unitActionHandler.SetPreviousTargetEnemyGridPosition(Unit.unitActionHandler.TargetEnemyUnit.GridPosition);

            if (Unit.IsPlayer && Unit.unitActionHandler.PlayerActionHandler.SelectedAction is BaseAttackAction)
                Unit.unitActionHandler.MoveAction.QueueAction(Unit.unitActionHandler.PlayerActionHandler.SelectedAction.BaseAttackAction.GetNearestAttackPosition(Unit.GridPosition, Unit.unitActionHandler.TargetEnemyUnit));
            else if (Unit.UnitEquipment.RangedWeaponEquipped && Unit.UnitEquipment.HasValidAmmunitionEquipped())
                Unit.unitActionHandler.MoveAction.QueueAction(Unit.unitActionHandler.GetAction<ShootAction>().GetNearestAttackPosition(Unit.GridPosition, Unit.unitActionHandler.TargetEnemyUnit));
            else if (Unit.UnitEquipment.MeleeWeaponEquipped || Unit.stats.CanFightUnarmed)
                Unit.unitActionHandler.MoveAction.QueueAction(Unit.unitActionHandler.GetAction<MeleeAction>().GetNearestAttackPosition(Unit.GridPosition, Unit.unitActionHandler.TargetEnemyUnit));
            else
            {
                Unit.unitActionHandler.SetTargetEnemyUnit(null);
                Unit.unitActionHandler.SkipTurn();
                return;
            }
        }

        void GetPathToTargetPosition(GridPosition targetGridPosition)
        {
            Unit unitAtTargetGridPosition = LevelGrid.GetUnitAtGridPosition(targetGridPosition);
            if (unitAtTargetGridPosition != null && unitAtTargetGridPosition.health.IsDead == false)
            {
                unitAtTargetGridPosition.UnblockCurrentPosition();
                targetGridPosition = LevelGrid.GetNearestSurroundingGridPosition(targetGridPosition, Unit.GridPosition, LevelGrid.diaganolDistance, false);
            }

            FinalTargetGridPosition = targetGridPosition;

            Unit.UnblockCurrentPosition();

            ABPath path = ABPath.Construct(Unit.transform.position, LevelGrid.GetWorldPosition(targetGridPosition));
            path.traversalProvider = LevelGrid.DefaultTraversalProvider;

            // Schedule the path for calculation
            Unit.seeker.StartPath(path);

            // Force the path request to complete immediately. This assumes the graph is small enough that this will not cause any lag
            path.BlockUntilCalculated();

            if (Unit.IsNPC && path.vectorPath.Count == 0)
            {
                NPCActionHandler npcActionHandler = Unit.unitActionHandler as NPCActionHandler;
                if (Unit.stateController.currentState == State.Patrol)
                {
                    GridPosition patrolPointGridPosition = LevelGrid.GetGridPosition(npcActionHandler.PatrolPoints()[npcActionHandler.currentPatrolPointIndex]);
                    npcActionHandler.IncreasePatrolPointIndex();
                    FinalTargetGridPosition = patrolPointGridPosition;
                }

                TurnManager.Instance.FinishTurn(Unit);
                npcActionHandler.FinishAction();
                return;
            }

            positionList.Clear();
            positionIndex = 1;

            for (int i = 0; i < path.vectorPath.Count; i++)
            {
                positionList.Add(path.vectorPath[i]);
            }

            Unit.BlockCurrentPosition();
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
            if (TargetGridPosition != FinalTargetGridPosition || (positionList.Count > 0 && LevelGrid.GridPositionObstructed(LevelGrid.GetGridPosition(positionList[positionIndex]))))
                GetPathToTargetPosition(TargetGridPosition);

            if (positionList.Count == 0)
                return cost;

            // Check for an Interactable on the next move position
            Vector3 nextPointOnPath = positionList[positionIndex];
            Vector3 unitPosition = Unit.transform.position;
            Vector3 nextPathPosition = GetNextPathPosition_XZ(nextPointOnPath);

            // If the next path position is above the unit's current position
            if (nextPointOnPath.y - unitPosition.y > 0f)
                nextPathPosition.Set(nextPathPosition.x, nextPointOnPath.y, nextPathPosition.z);
            // If the next path position is below the unit's current position
            else if (nextPointOnPath.y - unitPosition.y < 0f && (Mathf.Approximately(nextPathPosition.x, unitPosition.x) == false || Mathf.Approximately(nextPathPosition.z, unitPosition.z) == false))
                nextPathPosition.Set(nextPathPosition.x, nextPointOnPath.y, nextPathPosition.z);

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
                        Unit.unitActionHandler.InteractAction.QueueActionImmediately(door);
                        return 0;
                    }
                }
            }

            // Get the next Move position
            nextTargetPosition = GetNextTargetPosition();
            NextTargetGridPosition = LevelGrid.GetGridPosition(nextTargetPosition);

            float tileCostMultiplier = GetTileMoveCostMultiplier(nextTargetPosition);

            floatCost += floatCost * tileCostMultiplier;
            if (LevelGrid.IsDiagonal(Unit.WorldPosition, nextTargetPosition))
                floatCost *= 1.4f;

            cost = Mathf.RoundToInt(floatCost);

            if (nextTargetPosition == Unit.transform.position)
            {
                TargetGridPosition = Unit.GridPosition;

                if (Unit.IsNPC)
                {
                    if (Unit.stateController.currentState == State.Patrol)
                    {
                        NPCActionHandler npcActionHandler = Unit.unitActionHandler as NPCActionHandler;
                        npcActionHandler.AssignNextPatrolTargetPosition();
                    }
                }
            }

            Unit.BlockCurrentPosition();

            // if (unit.IsPlayer) Debug.Log("Move Cost (" + nextTargetPosition + "): " + Mathf.RoundToInt(cost * unit.stats.EncumbranceMoveCostModifier()));
            return Mathf.RoundToInt(cost * Unit.stats.EncumbranceMoveCostModifier());
        }

        Vector3 GetNextPathPosition_XZ(Vector3 nextPointOnPath)
        {
            // Get the next path position, not including the Y coordinate
            if (Mathf.RoundToInt(nextPointOnPath.x) == Mathf.RoundToInt(Unit.transform.position.x) && Mathf.RoundToInt(nextPointOnPath.z) > Mathf.RoundToInt(Unit.transform.position.z))
                return new Vector3(Unit.transform.position.x, Unit.transform.position.y, Unit.transform.position.z + 1);
            else if (Mathf.RoundToInt(nextPointOnPath.x) == Mathf.RoundToInt(Unit.transform.position.x) && Mathf.RoundToInt(nextPointOnPath.z) < Mathf.RoundToInt(Unit.transform.position.z))
                return new Vector3(Unit.transform.position.x, Unit.transform.position.y, Unit.transform.position.z - 1);
            else if (Mathf.RoundToInt(nextPointOnPath.x) > Mathf.RoundToInt(Unit.transform.position.x) && Mathf.RoundToInt(nextPointOnPath.z) == Mathf.RoundToInt(Unit.transform.position.z))
                return new Vector3(Unit.transform.position.x + 1, Unit.transform.position.y, Unit.transform.position.z);
            else if (Mathf.RoundToInt(nextPointOnPath.x) < Mathf.RoundToInt(Unit.transform.position.x) && Mathf.RoundToInt(nextPointOnPath.z) == Mathf.RoundToInt(Unit.transform.position.z))
                return new Vector3(Unit.transform.position.x - 1, Unit.transform.position.y, Unit.transform.position.z);
            else if (Mathf.RoundToInt(nextPointOnPath.x) > Mathf.RoundToInt(Unit.transform.position.x) && Mathf.RoundToInt(nextPointOnPath.z) > Mathf.RoundToInt(Unit.transform.position.z))
                return new Vector3(Unit.transform.position.x + 1, Unit.transform.position.y, Unit.transform.position.z + 1);
            else if (Mathf.RoundToInt(nextPointOnPath.x) < Mathf.RoundToInt(Unit.transform.position.x) && Mathf.RoundToInt(nextPointOnPath.z) < Mathf.RoundToInt(Unit.transform.position.z))
                return new Vector3(Unit.transform.position.x - 1, Unit.transform.position.y, Unit.transform.position.z - 1);
            else if (Mathf.RoundToInt(nextPointOnPath.x) > Mathf.RoundToInt(Unit.transform.position.x) && Mathf.RoundToInt(nextPointOnPath.z) < Mathf.RoundToInt(Unit.transform.position.z))
                return new Vector3(Unit.transform.position.x + 1, Unit.transform.position.y, Unit.transform.position.z - 1);
            else if (Mathf.RoundToInt(nextPointOnPath.x) < Mathf.RoundToInt(Unit.transform.position.x) && Mathf.RoundToInt(nextPointOnPath.z) > Mathf.RoundToInt(Unit.transform.position.z))
                return new Vector3(Unit.transform.position.x - 1, Unit.transform.position.y, Unit.transform.position.z + 1);
            else
                return Unit.transform.position;
        }

        Vector3 GetNextTargetPosition()
        {
            Vector3 nextPointOnPath = positionList[positionIndex];
            Vector3 nextTargetPosition;
            if (Mathf.Approximately(nextPointOnPath.y, Unit.transform.position.y) == false)
                nextTargetPosition = nextPointOnPath;
            else if (Mathf.RoundToInt(nextPointOnPath.x) == Mathf.RoundToInt(Unit.transform.position.x) && Mathf.RoundToInt(nextPointOnPath.z) > Mathf.RoundToInt(Unit.transform.position.z)) // North
                nextTargetPosition = new Vector3(Unit.transform.position.x, Unit.transform.position.y, Unit.transform.position.z + 1);
            else if (Mathf.RoundToInt(nextPointOnPath.x) == Mathf.RoundToInt(Unit.transform.position.x) && Mathf.RoundToInt(nextPointOnPath.z) < Mathf.RoundToInt(Unit.transform.position.z)) // South
                nextTargetPosition = new Vector3(Unit.transform.position.x, Unit.transform.position.y, Unit.transform.position.z - 1);
            else if (Mathf.RoundToInt(nextPointOnPath.x) > Mathf.RoundToInt(Unit.transform.position.x) && Mathf.RoundToInt(nextPointOnPath.z) == Mathf.RoundToInt(Unit.transform.position.z)) // East
                nextTargetPosition = new Vector3(Unit.transform.position.x + 1, Unit.transform.position.y, Unit.transform.position.z);
            else if (Mathf.RoundToInt(nextPointOnPath.x) < Mathf.RoundToInt(Unit.transform.position.x) && Mathf.RoundToInt(nextPointOnPath.z) == Mathf.RoundToInt(Unit.transform.position.z)) // West
                nextTargetPosition = new Vector3(Unit.transform.position.x - 1, Unit.transform.position.y, Unit.transform.position.z);
            else if (Mathf.RoundToInt(nextPointOnPath.x) > Mathf.RoundToInt(Unit.transform.position.x) && Mathf.RoundToInt(nextPointOnPath.z) > Mathf.RoundToInt(Unit.transform.position.z)) // NorthEast
                nextTargetPosition = new Vector3(Unit.transform.position.x + 1, Unit.transform.position.y, Unit.transform.position.z + 1);
            else if (Mathf.RoundToInt(nextPointOnPath.x) < Mathf.RoundToInt(Unit.transform.position.x) && Mathf.RoundToInt(nextPointOnPath.z) < Mathf.RoundToInt(Unit.transform.position.z)) // SouthWest
                nextTargetPosition = new Vector3(Unit.transform.position.x - 1, Unit.transform.position.y, Unit.transform.position.z - 1);
            else if (Mathf.RoundToInt(nextPointOnPath.x) > Mathf.RoundToInt(Unit.transform.position.x) && Mathf.RoundToInt(nextPointOnPath.z) < Mathf.RoundToInt(Unit.transform.position.z)) // SouthEast
                nextTargetPosition = new Vector3(Unit.transform.position.x + 1, Unit.transform.position.y, Unit.transform.position.z - 1);
            else if (Mathf.RoundToInt(nextPointOnPath.x) < Mathf.RoundToInt(Unit.transform.position.x) && Mathf.RoundToInt(nextPointOnPath.z) > Mathf.RoundToInt(Unit.transform.position.z)) // NorthWest
                nextTargetPosition = new Vector3(Unit.transform.position.x - 1, Unit.transform.position.y, Unit.transform.position.z + 1);
            else // Debug.LogWarning("Next Position is " + unit.name + "'s current position...");
                nextTargetPosition = Unit.transform.position;
            nextTargetPosition.Set(Mathf.RoundToInt(nextTargetPosition.x), nextTargetPosition.y, Mathf.RoundToInt(nextTargetPosition.z));
            return nextTargetPosition;
        }

        Direction GetDirectionToNextTargetPosition(Vector3 targetPosition)
        {
            if (Mathf.RoundToInt(targetPosition.x) == Mathf.RoundToInt(Unit.transform.position.x) && Mathf.RoundToInt(targetPosition.z) > Mathf.RoundToInt(Unit.transform.position.z))
                return Direction.North;
            else if (Mathf.RoundToInt(targetPosition.x) == Mathf.RoundToInt(Unit.transform.position.x) && Mathf.RoundToInt(targetPosition.z) < Mathf.RoundToInt(Unit.transform.position.z))
                return Direction.South;
            else if (Mathf.RoundToInt(targetPosition.x) > Mathf.RoundToInt(Unit.transform.position.x) && Mathf.RoundToInt(targetPosition.z) == Mathf.RoundToInt(Unit.transform.position.z))
                return Direction.East;
            else if (Mathf.RoundToInt(targetPosition.x) < Mathf.RoundToInt(Unit.transform.position.x) && Mathf.RoundToInt(targetPosition.z) == Mathf.RoundToInt(Unit.transform.position.z))
                return Direction.West;
            else if (Mathf.RoundToInt(targetPosition.x) > Mathf.RoundToInt(Unit.transform.position.x) && Mathf.RoundToInt(targetPosition.z) > Mathf.RoundToInt(Unit.transform.position.z))
                return Direction.NorthEast;
            else if (Mathf.RoundToInt(targetPosition.x) < Mathf.RoundToInt(Unit.transform.position.x) && Mathf.RoundToInt(targetPosition.z) < Mathf.RoundToInt(Unit.transform.position.z))
                return Direction.SouthWest;
            else if (Mathf.RoundToInt(targetPosition.x) > Mathf.RoundToInt(Unit.transform.position.x) && Mathf.RoundToInt(targetPosition.z) < Mathf.RoundToInt(Unit.transform.position.z))
                return Direction.SouthEast;
            else if (Mathf.RoundToInt(targetPosition.x) < Mathf.RoundToInt(Unit.transform.position.x) && Mathf.RoundToInt(targetPosition.z) > Mathf.RoundToInt(Unit.transform.position.z))
                return Direction.NorthWest;
            else
                return Direction.Center;
        }

        float GetTileMoveCostMultiplier(Vector3 tilePosition)
        {
            GraphNode node = AstarPath.active.GetNearest(tilePosition).node;
            // if (unit.IsPlayer) Debug.Log("Tag #" + node.Tag + " penalty is: ");

            for (int i = 0; i < Unit.seeker.tagPenalties.Length; i++)
            {
                if (node.Tag == i)
                {
                    if (Unit.seeker.tagPenalties[i] == 0)
                        return 0f;
                    else
                        return Unit.seeker.tagPenalties[i] / 1000f;
                }
            }

            return 1f;
        }

        public override void CompleteAction()
        {
            base.CompleteAction();

            // Unblock the Unit's position, in case it's still their turn after this action ( so that the ActionLineRenderer will work). If not, it will be blocked again in the TurnManager's finish turn methods
            if (Unit.IsPlayer)
                Unit.UnblockCurrentPosition();
            else if (Unit.health.IsDead == false)
                Unit.BlockCurrentPosition();

            IsMoving = false;
            AboutToMove = false;
            Unit.unitActionHandler.FinishAction();
        }

        public void SetTravelDistanceSpeedMultiplier()
        {
            if (Unit.IsPlayer)
                return;

            if ((float)Unit.stats.lastPooledAP / defaultTileMoveCost <= 1f)
                travelDistanceMultiplier = 1f;
            else
                travelDistanceMultiplier = Mathf.FloorToInt((float)Unit.stats.lastPooledAP / defaultTileMoveCost);
        }

        public override bool IsValidAction()
        {
            // TODO: Test if the unit is immobile for whatever reason (broken legs, some sort of spell effect, etc.)
            return true;
        }

        public void SetCanMove(bool canMove) => this.CanMove = canMove;

        public void SetFinalTargetGridPosition(GridPosition finalGridPosition) => FinalTargetGridPosition = finalGridPosition;

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
