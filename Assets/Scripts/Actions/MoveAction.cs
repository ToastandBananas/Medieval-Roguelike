using Pathfinding;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MoveAction : BaseAction
{
    public event EventHandler OnStartMoving;
    public event EventHandler OnStopMoving;

    public Seeker seeker { get; private set; }
    public GridPosition finalTargetGridPosition { get; private set; }
    public GridPosition nextTargetGridPosition { get; private set; }
    Vector3 nextTargetPosition;

    public bool isMoving { get; private set; }

    [SerializeField] LayerMask moveObstaclesMask;

    List<Vector3> positionList;
    int positionIndex;

    readonly float defaultMoveSpeed = 3.5f;
    float moveSpeed;

    readonly int defaultTileMoveCost = 200;

    public override void Awake()
    {
        base.Awake();

        seeker = GetComponent<Seeker>();

        positionList = new List<Vector3>();
    }

    void Start() => moveSpeed = defaultMoveSpeed;

    public override void TakeAction(GridPosition targetGridPosition)
    {
        if (isMoving) return;

        StartAction();
        StartCoroutine(Move());
    }

    IEnumerator Move()
    {
        // If there's no path
        if (positionList.Count == 0 || finalTargetGridPosition == unit.gridPosition)
        {
            CompleteAction();
            StartCoroutine(TurnManager.Instance.StartNextUnitsTurn(unit));
            yield break;
        }

        // If the next position is obstructed
        if (LevelGrid.Instance.GridPositionObstructed(nextTargetGridPosition))
        {
            // Get a new path to the target position
            GetPathToTargetPosition(finalTargetGridPosition);

            // If we still can't find a path, just finish the action
            if (positionList.Count == 0) 
            {
                CompleteAction();
                StartCoroutine(TurnManager.Instance.StartNextUnitsTurn(unit));
                yield break;
            }

            nextTargetPosition = GetNextTargetPosition();
            nextTargetGridPosition = LevelGrid.Instance.GetGridPosition(nextTargetPosition);
        }

        if (nextTargetPosition == unit.WorldPosition() || LevelGrid.Instance.GridPositionObstructed(LevelGrid.Instance.GetGridPosition(nextTargetPosition)))
        {
            //if (unit.IsPlayer()) Debug.Log(unit.name + "'s next position is not walkable or is the same as the their current position...");
            CompleteAction();
            StartCoroutine(TurnManager.Instance.StartNextUnitsTurn(unit));
            yield break;
        }

        // Unblock the Unit's current position since they're about to move
        isMoving = true;
        unit.UnblockCurrentPosition();

        // Block the Next Position so that NPCs who are also currently looking for a path don't try to use the Next Position's tile
        unit.BlockAtPosition(nextTargetPosition);

        // Remove the Unit reference from it's current Grid Position and add the Unit to its next Grid Position
        LevelGrid.Instance.RemoveUnitAtGridPosition(unit.gridPosition);
        LevelGrid.Instance.AddUnitAtGridPosition(LevelGrid.Instance.GetGridPosition(nextTargetPosition), unit);

        // Start the next Unit's action before moving, that way their actions play out at the same time as this Unit's
        StartCoroutine(TurnManager.Instance.StartNextUnitsTurn(unit));

        ActionLineRenderer.Instance.HideLineRenderers();

        Vector3 nextPointOnPath = positionList[positionIndex];
        Vector3 nextPathPosition = unit.transform.position;
        Direction directionToNextPosition;

        if (unit.IsPlayer() || unit.IsVisibleOnScreen())
        {
            directionToNextPosition = GetDirectionToNextTargetPosition(nextPointOnPath);

            unit.unitActionHandler.GetAction<TurnAction>().SetTargetPosition(directionToNextPosition);
            StartCoroutine(unit.unitActionHandler.GetAction<TurnAction>().RotateTowards_CurrentTargetPosition(false));

            // Get the next path position, not including the Y coordinate
            if (Mathf.RoundToInt(nextPointOnPath.x) == Mathf.RoundToInt(unit.transform.position.x) && Mathf.RoundToInt(nextPointOnPath.z) > Mathf.RoundToInt(unit.transform.position.z))
                nextPathPosition = new Vector3(unit.transform.position.x, unit.transform.position.y, unit.transform.position.z + 1);
            else if (Mathf.RoundToInt(nextPointOnPath.x) == Mathf.RoundToInt(unit.transform.position.x) && Mathf.RoundToInt(nextPointOnPath.z) < Mathf.RoundToInt(unit.transform.position.z))
                nextPathPosition = new Vector3(unit.transform.position.x, unit.transform.position.y, unit.transform.position.z - 1);
            else if (Mathf.RoundToInt(nextPointOnPath.x) > Mathf.RoundToInt(unit.transform.position.x) && Mathf.RoundToInt(nextPointOnPath.z) == Mathf.RoundToInt(unit.transform.position.z))
                nextPathPosition = new Vector3(unit.transform.position.x + 1, unit.transform.position.y, unit.transform.position.z);
            else if (Mathf.RoundToInt(nextPointOnPath.x) < Mathf.RoundToInt(unit.transform.position.x) && Mathf.RoundToInt(nextPointOnPath.z) == Mathf.RoundToInt(unit.transform.position.z))
                nextPathPosition = new Vector3(unit.transform.position.x - 1, unit.transform.position.y, unit.transform.position.z);
            else if (Mathf.RoundToInt(nextPointOnPath.x) > Mathf.RoundToInt(unit.transform.position.x) && Mathf.RoundToInt(nextPointOnPath.z) > Mathf.RoundToInt(unit.transform.position.z))
                nextPathPosition = new Vector3(unit.transform.position.x + 1, unit.transform.position.y, unit.transform.position.z + 1);
            else if (Mathf.RoundToInt(nextPointOnPath.x) < Mathf.RoundToInt(unit.transform.position.x) && Mathf.RoundToInt(nextPointOnPath.z) < Mathf.RoundToInt(unit.transform.position.z))
                nextPathPosition = new Vector3(unit.transform.position.x - 1, unit.transform.position.y, unit.transform.position.z - 1);
            else if (Mathf.RoundToInt(nextPointOnPath.x) > Mathf.RoundToInt(unit.transform.position.x) && Mathf.RoundToInt(nextPointOnPath.z) < Mathf.RoundToInt(unit.transform.position.z))
                nextPathPosition = new Vector3(unit.transform.position.x + 1, unit.transform.position.y, unit.transform.position.z - 1);
            else if (Mathf.RoundToInt(nextPointOnPath.x) < Mathf.RoundToInt(unit.transform.position.x) && Mathf.RoundToInt(nextPointOnPath.z) > Mathf.RoundToInt(unit.transform.position.z))
                nextPathPosition = new Vector3(unit.transform.position.x - 1, unit.transform.position.y, unit.transform.position.z + 1);

            float moveSpeedMultiplier = 1f;
            if (unit.IsNPC())
            {
                if (LevelGrid.IsDiagonal(unit.transform.position, nextTargetPosition))
                    moveSpeedMultiplier = 1.4f;

                if (nextTargetPosition.y - unit.transform.position.y > 0f || nextTargetPosition.y - unit.transform.position.y < 0f)
                    moveSpeedMultiplier *= 2f;
            }

            bool moveUpchecked = false;
            bool moveDownChecked = false;
            bool moveAboveChecked = false;
            float stoppingDistance = 0.0125f;
            Vector3 unitPosition = unit.transform.position;
            Vector3 targetPosition = unitPosition;
            while (Vector3.Distance(unit.transform.position, nextPathPosition) > stoppingDistance)
            {
                unit.unitAnimator.StartMovingForward(); // Move animation

                unitPosition = unit.transform.position;

                // If the next point on the path is above or below the Unit
                if (Mathf.Abs(Mathf.Abs(nextPointOnPath.y) - Mathf.Abs(unitPosition.y)) > stoppingDistance)
                {
                    // If the next path position is above the unit's current position
                    if (nextPointOnPath.y - unitPosition.y > 0f)
                    {
                        if (moveUpchecked == false)
                        {
                            moveUpchecked = true;
                            targetPosition = new Vector3(unitPosition.x, nextPointOnPath.y, unitPosition.z);
                            nextPathPosition = new Vector3(nextPathPosition.x, nextPointOnPath.y, nextPathPosition.z);
                        }
                    }
                    // If the Unit is directly above the next path position
                    else if (nextPointOnPath.y - unitPosition.y < 0f && Mathf.Abs(nextPathPosition.x - unitPosition.x) < stoppingDistance && Mathf.Abs(nextPathPosition.z - unitPosition.z) < stoppingDistance)
                    {
                        if (moveDownChecked == false)
                        {
                            moveDownChecked = true;
                            targetPosition = nextPathPosition;
                        }
                    }
                    // If the next path position is below the unit's current position
                    else if (moveAboveChecked == false && nextPointOnPath.y - unitPosition.y < 0f && (Mathf.Approximately(nextPathPosition.x, unitPosition.x) == false || Mathf.Approximately(nextPathPosition.z, unitPosition.z) == false))
                    {
                        moveAboveChecked = true;
                        targetPosition = new Vector3(nextPointOnPath.x, unitPosition.y, nextPointOnPath.z);
                        nextPathPosition = new Vector3(nextPathPosition.x, nextPointOnPath.y, nextPathPosition.z);
                    }
                }
                else // Otherwise, the target position is simply the next position on the Path
                    targetPosition = nextPathPosition;

                // Move to the target position
                //Vector3 moveDirection = (targetPosition - unitPosition).normalized;
                float distanceToTargetPosition = Vector3.Distance(unitPosition, targetPosition);
                if (distanceToTargetPosition > stoppingDistance)
                {
                    float distanceToFinalPosition = Vector3.Distance(unitPosition, LevelGrid.Instance.GetWorldPosition(finalTargetGridPosition));
                    float distanceToTriggerStopAnimation = 1f;
                    if (distanceToFinalPosition <= distanceToTriggerStopAnimation)
                        unit.unitAnimator.StopMovingForward();

                    unit.transform.position = Vector3.MoveTowards(unit.transform.position, targetPosition, moveSpeed * moveSpeedMultiplier * Time.deltaTime);
                    //unit.transform.position += moveDirection * moveSpeed * moveSpeedMultiplier * Time.deltaTime;
                }

                yield return null;
            }
        }
        else // Move and rotate instantly while NPC is offscreen
        {
            directionToNextPosition = GetDirectionToNextTargetPosition(nextPointOnPath);
            unit.unitActionHandler.GetAction<TurnAction>().RotateTowards_Direction(directionToNextPosition, true);

            nextPathPosition = nextTargetPosition;
            unit.UpdateGridPosition();

            StartCoroutine(TurnManager.Instance.StartNextUnitsTurn(unit));
        }

        nextPathPosition = new Vector3(Mathf.RoundToInt(nextPathPosition.x), nextPathPosition.y, Mathf.RoundToInt(nextPathPosition.z));
        unit.transform.position = nextPathPosition;

        // If the Unit has reached the next point in the Path's position list, but hasn't reached the final position, increase the index
        if (positionIndex < positionList.Count && unit.transform.position == positionList[positionIndex] && unit.transform.position != finalTargetGridPosition.WorldPosition())
            positionIndex++;

        CompleteAction();
        OnStopMoving?.Invoke(this, EventArgs.Empty);

        if (unit.IsPlayer())
        {
            // If the target enemy Unit died
            if (unit.unitActionHandler.targetEnemyUnit != null && unit.unitActionHandler.targetEnemyUnit.health.IsDead())
                unit.unitActionHandler.CancelAction();
            // If the Player is trying to attack an enemy and they are in range, stop moving and attack
            else if (unit.unitActionHandler.targetEnemyUnit != null && (((unit.MeleeWeaponEquipped() || (unit.RangedWeaponEquipped() == false && unit.unitActionHandler.GetAction<MeleeAction>().CanFightUnarmed())) && unit.unitActionHandler.GetAction<MeleeAction>().IsInAttackRange(unit.unitActionHandler.targetEnemyUnit))
                || (unit.RangedWeaponEquipped() && unit.unitActionHandler.GetAction<ShootAction>().IsInAttackRange(unit.unitActionHandler.targetEnemyUnit))))
            {
                unit.unitAnimator.StopMovingForward();
                unit.unitActionHandler.AttackTargetEnemy();
            }
            // If the enemy moved positions, set the target position to the nearest possible attack position
            else if (unit.unitActionHandler.targetEnemyUnit != null && unit.unitActionHandler.previousTargetEnemyGridPosition != unit.unitActionHandler.targetEnemyUnit.gridPosition)
            {
                unit.unitActionHandler.SetPreviousTargetEnemyGridPosition(unit.unitActionHandler.targetEnemyUnit.gridPosition);
                if (unit.RangedWeaponEquipped())
                    unit.unitActionHandler.SetTargetGridPosition(unit.unitActionHandler.GetAction<ShootAction>().GetNearestShootPosition(unit.gridPosition, unit.unitActionHandler.targetEnemyUnit.gridPosition));
                else
                    unit.unitActionHandler.SetTargetGridPosition(unit.unitActionHandler.GetAction<MeleeAction>().GetNearestMeleePosition(unit.gridPosition, unit.unitActionHandler.targetEnemyUnit.gridPosition));

                unit.unitActionHandler.QueueAction(this);
            }
            // If the Player hasn't reached their destination, add the next move to the queue
            else if (unit.gridPosition != finalTargetGridPosition)
                unit.unitActionHandler.QueueAction(this);
        }
        else // If NPC
        {
            // If they're trying to attack
            if (unit.stateController.currentState == State.Fight && unit.unitActionHandler.targetEnemyUnit != null)
            {
                // If they're in range, stop moving and attack
                if (((unit.MeleeWeaponEquipped() || (unit.RangedWeaponEquipped() == false && unit.unitActionHandler.GetAction<MeleeAction>().CanFightUnarmed())) && unit.unitActionHandler.GetAction<MeleeAction>().IsInAttackRange(unit.unitActionHandler.targetEnemyUnit))
                || (unit.RangedWeaponEquipped() && unit.unitActionHandler.GetAction<ShootAction>().IsInAttackRange(unit.unitActionHandler.targetEnemyUnit)))
                {
                    unit.unitAnimator.StopMovingForward();
                    unit.unitActionHandler.AttackTargetEnemy();
                }
            }
        }

        // Check for newly visible Units
        UnitManager.Instance.player.vision.FindVisibleUnits();
    }

    void GetPathToTargetPosition(GridPosition targetGridPosition)
    {
        Unit unitAtTargetGridPosition = null;
        if (LevelGrid.Instance.HasAnyUnitOnGridPosition(targetGridPosition))
        {
            unitAtTargetGridPosition = LevelGrid.Instance.GetUnitAtGridPosition(targetGridPosition);
            unitAtTargetGridPosition.UnblockCurrentPosition();
            targetGridPosition = LevelGrid.Instance.GetNearestSurroundingGridPosition(targetGridPosition, unit.gridPosition);
        }

        SetFinalTargetGridPosition(targetGridPosition);

        unit.UnblockCurrentPosition();

        ABPath path = ABPath.Construct(unit.transform.position, LevelGrid.Instance.GetWorldPosition(targetGridPosition));
        path.traversalProvider = LevelGrid.Instance.DefaultTraversalProvider();

        // Schedule the path for calculation
        seeker.StartPath(path);

        // Force the path request to complete immediately. This assumes the graph is small enough that this will not cause any lag
        path.BlockUntilCalculated();

        if (unit.IsNPC() && path.vectorPath.Count == 0)
        {
            NPCActionHandler npcActionHandler = unit.unitActionHandler as NPCActionHandler;
            if (unit.stateController.currentState == State.Patrol)
            {
                GridPosition patrolPointGridPosition = LevelGrid.Instance.GetGridPosition(npcActionHandler.PatrolPoints()[npcActionHandler.currentPatrolPointIndex]);
                npcActionHandler.IncreasePatrolPointIndex();
                npcActionHandler.SetTargetGridPosition(patrolPointGridPosition);
                SetFinalTargetGridPosition(patrolPointGridPosition);
            }

            TurnManager.Instance.FinishTurn(unit);
            unit.unitActionHandler.FinishAction();
            return;
        }

        positionList.Clear();
        positionIndex = 1;

        for (int i = 0; i < path.vectorPath.Count; i++)
        {
            positionList.Add(path.vectorPath[i]);
        }

        unit.BlockCurrentPosition();
        if (unitAtTargetGridPosition != null)
            unitAtTargetGridPosition.BlockCurrentPosition();
    }

    void RotateTowardsTargetPosition(Vector3 targetPosition)
    {
        float rotateSpeed = 10f;
        Vector3 lookPos = (new Vector3(targetPosition.x, transform.position.y, targetPosition.z) - transform.position).normalized;
        Quaternion targetRotation = Quaternion.LookRotation(lookPos);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotateSpeed * Time.deltaTime);
    }

    public override bool IsValidAction()
    {
        // TODO: Test if the unit is immobile for whatever reason (broken legs, some sort of spell effect, etc.)
        return true;
    }

    public override int GetActionPointsCost(GridPosition targetGridPosition)
    {
        // TODO: Cost 600 (6 seconds) per square (or more depending on terrain type)
        int cost = defaultTileMoveCost;
        float floatCost = cost;

        if (positionIndex >= positionList.Count)
            positionIndex = positionList.Count - 1;

        // Only calculate a new path if the Unit's target position changed or if their path becomes obstructed
        if (unit.unitActionHandler.targetGridPosition != finalTargetGridPosition || (positionList.Count > 0 && LevelGrid.Instance.GridPositionObstructed(LevelGrid.Instance.GetGridPosition(positionList[positionIndex]))))
            GetPathToTargetPosition(unit.unitActionHandler.targetGridPosition);

        if (positionList.Count == 0)
            return cost;

        // Get the next Move position
        nextTargetPosition = GetNextTargetPosition();
        nextTargetGridPosition = LevelGrid.Instance.GetGridPosition(nextTargetPosition);

        float tileCostMultiplier = GetTileMoveCostMultiplier(nextTargetPosition);
        // if (unit.IsPlayer()) Debug.Log(tileCostMultiplier);

        floatCost += floatCost * tileCostMultiplier;

        if (LevelGrid.IsDiagonal(unit.WorldPosition(), nextTargetPosition))
            floatCost *= 1.4f;

        cost = Mathf.RoundToInt(floatCost);

        if (nextTargetPosition == transform.position)
        {
            unit.unitActionHandler.SetTargetGridPosition(unit.gridPosition);

            if (unit.IsNPC())
            {
                if (unit.stateController.currentState == State.Patrol)
                {
                    NPCActionHandler npcActionHandler = unit.unitActionHandler as NPCActionHandler;
                    npcActionHandler.AssignNextPatrolTargetPosition();
                }
            }

            CompleteAction();
        }

        unit.BlockCurrentPosition();

        // if (unit.IsNPC()) Debug.Log("Move Cost (" + nextTargetPosition + "): " + cost);
        return cost;
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
            nextTargetPosition = transform.position;
        nextTargetPosition = new Vector3(Mathf.RoundToInt(nextTargetPosition.x), nextTargetPosition.y, Mathf.RoundToInt(nextTargetPosition.z));
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
        // if (unit.IsPlayer()) Debug.Log("Tag #" + node.Tag + " penalty is: ");

        for (int i = 0; i < seeker.tagPenalties.Length; i++)
        {
            if (node.Tag == i)
            {
                if (seeker.tagPenalties[i] == 0)
                    return 0f;
                else
                    return seeker.tagPenalties[i] / 1000f;
            }
        }

        return 1f;
    }

    public override void CompleteAction()
    {
        base.CompleteAction();

        // Unblock the Unit's postion, in case it's still their turn after this action ( so that the ActionLineRenderer will work). If not, it will be blocked again in the TurnManager's finish turn methods
        if (unit.IsPlayer())
            unit.UnblockCurrentPosition();
        else
            unit.BlockCurrentPosition();

        unit.UpdateGridPosition();

        isMoving = false;
        unit.unitActionHandler.FinishAction();
    }

    public void SetMoveSpeed(int pooledAP)
    {
        if (unit.IsPlayer() || ((float)pooledAP) / defaultTileMoveCost <= 1f)
            moveSpeed = defaultMoveSpeed;
        else
            moveSpeed = Mathf.FloorToInt((((float)pooledAP) / defaultTileMoveCost) * defaultMoveSpeed) + 1f;
    }

    public void SetIsMoving(bool isMoving) => this.isMoving = isMoving;

    public void SetFinalTargetGridPosition(GridPosition finalTargetGridPosition) => this.finalTargetGridPosition = finalTargetGridPosition;

    public void SetNextTargetGridPosition(GridPosition nextTargetGridPosition) => this.nextTargetGridPosition = nextTargetGridPosition;

    public override string GetActionName() => "Move";

    public override bool ActionIsUsedInstantly() => false;

    public LayerMask MoveObstaclesMask() => moveObstaclesMask;

    public override EnemyAIAction GetEnemyAIAction(GridPosition gridPosition)
    {
        throw new NotImplementedException();
    }
}
