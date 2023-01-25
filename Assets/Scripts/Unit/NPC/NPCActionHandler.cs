using UnityEngine;

public class NPCActionHandler : UnitActionHandler
{
    [Header("Flee State Variables")]
    [SerializeField] float fleeDistance = 20f;
    [SerializeField] bool shouldAlwaysFleeCombat;
    Vector3 fleeDestination;
    float distToFleeDestination = 0;
    bool needsFleeDestination = true;

    [Header("Follow State Variables")]
    [SerializeField] float startFollowingDistance = 3f;
    [SerializeField] float slowDownDistance = 4f;
    [SerializeField] public Unit leader { get; private set; }
    [SerializeField] public bool shouldFollowLeader { get; private set; }

    [Header("Patrol State Variables")]
    [SerializeField] Vector3[] patrolPoints;
    public int currentPatrolPointIndex { get; private set; }
    bool initialPatrolPointSet, hasAlternativePatrolPoint;
    int patrolIterationCount;
    readonly int maxPatrolIterations = 5;

    [Header("Pursue State Variables")]
    [SerializeField] float maxChaseDistance = 15f;

    [Header("Wandering State Variables")]
    [SerializeField] Vector3 defaultPosition;
    [SerializeField] int minWanderDistance = 5;
    [SerializeField] int maxWanderDistance = 20;
    GridPosition wanderGridPosition;
    bool wanderPositionSet;

    public override void TakeTurn()
    {
        if (unit.isMyTurn && unit.isDead == false)
        {
            if (unit.stats.CurrentAP() <= 0)
                TurnManager.Instance.FinishTurn(unit);
            else
            {
                // unit.vision.CheckEnemyVisibility();

                if (queuedAction != null)
                    GetNextQueuedAction();
                else
                    DetermineAction();
            }
        }
    }

    public override void FinishAction()
    {
        base.FinishAction();

        if (unit.stats.CurrentAP() > 0 && unit.isMyTurn) // Take another action if this is the last NPC who hasn't finished their turn
            StartCoroutine(TurnManager.Instance.StartNextUnitsTurn(unit));
    }

    public void DetermineAction()
    {
        switch (unit.stateController.CurrentState())
        {
            case State.Idle:
                TurnManager.Instance.FinishTurn(unit);
                break;
            case State.Patrol:
                Patrol();
                break;
            case State.Wander:
                Wander();
                break;
            case State.Follow:
                break;
            case State.MoveToTarget:
                break;
            case State.Fight:
                break;
            case State.Flee:
                break;
            case State.Hunt:
                break;
            case State.FindFood:
                break;
            default:
                break;
        }
    }

    #region Patrol
    public void Patrol()
    {
        if (patrolIterationCount >= maxPatrolIterations)
        {
            // Debug.Log("Max patrol iterations reached...");
            patrolIterationCount = 0;
            TurnManager.Instance.FinishTurn(unit);
            return;
        }
        else if (patrolPoints.Length > 0)
        {
            if (initialPatrolPointSet == false)
            {
                // Get the closest Patrol Point to the Unit as the first Patrol Point to move to
                currentPatrolPointIndex = GetNearestPatrolPointIndex();
                initialPatrolPointSet = true;
            }

            GridPosition patrolPointGridPosition = LevelGrid.Instance.GetGridPosition(patrolPoints[currentPatrolPointIndex]);

            // If the Patrol Point is set to an invalid Grid Position
            if (LevelGrid.Instance.IsValidGridPosition(patrolPointGridPosition) == false)
            {
                // Debug.LogWarning(patrolPointGridPosition + " is not a valid grid position...");
                IncreasePatrolPointIndex();
                return;
            }
            // If there's another Unit currently on the Patrol Point or Alternative Patrol Point
            else if ((hasAlternativePatrolPoint == false && LevelGrid.Instance.HasAnyUnitOnGridPosition(patrolPointGridPosition) && LevelGrid.Instance.GetUnitAtGridPosition(patrolPointGridPosition) != unit)
                || (hasAlternativePatrolPoint && LevelGrid.Instance.HasAnyUnitOnGridPosition(targetGridPosition) && LevelGrid.Instance.GetUnitAtGridPosition(targetGridPosition) != unit))
            {
                // Increase the iteration count just in case we had to look for an Alternative Patrol Point due to something obstructing the current Target Grid Position
                patrolIterationCount++;

                // Find the nearest Grid Position to the Patrol Point
                GridPosition nearestGridPositionToPatrolPoint = LevelGrid.Instance.FindNearestValidGridPosition(patrolPointGridPosition, unit);
                if (patrolPointGridPosition == nearestGridPositionToPatrolPoint)
                    IncreasePatrolPointIndex();

                hasAlternativePatrolPoint = true;
                SetTargetGridPosition(nearestGridPositionToPatrolPoint);

                if (nearestGridPositionToPatrolPoint != patrolPointGridPosition && LevelGrid.Instance.HasAnyUnitOnGridPosition(nearestGridPositionToPatrolPoint) == false)
                    patrolIterationCount = 0;
            }

            // If the Unit has arrived at their current Patrol Point or Alternative Patrol Point position
            if (Vector3.Distance(targetGridPosition.WorldPosition(), transform.position) <= 0.1f)
            {
                if (hasAlternativePatrolPoint)
                    hasAlternativePatrolPoint = false;

                // Set the Unit's Target Grid Position as the next Patrol Point
                IncreasePatrolPointIndex();
                patrolPointGridPosition = LevelGrid.Instance.GetGridPosition(patrolPoints[currentPatrolPointIndex]);
                SetTargetGridPosition(patrolPointGridPosition);
            }
            // Otherwise, assign their target position to the Patrol Point if it's not already set
            else if (hasAlternativePatrolPoint == false && targetGridPosition.WorldPosition() != patrolPoints[currentPatrolPointIndex])
            {
                SetTargetGridPosition(patrolPointGridPosition); 
                
                // Debug.Log("Unit Grid Position: " + unit.gridPosition + " / " + "Target Grid Position: " + targetGridPosition);

                // Don't reset the patrol iteration count if the next target position is the Unit's current position, because we'll need to iterate through Patrol again
                if (targetGridPosition != unit.gridPosition)
                    patrolIterationCount = 0;
            }

            // Queue the Move Action if the Unit isn't already moving
            if (GetAction<MoveAction>().isMoving == false)
                QueueAction(GetAction<MoveAction>(), GetAction<MoveAction>().GetActionPointsCost(targetGridPosition));
        }
        else // If no Patrol Points set
        {
            Debug.LogWarning("No patrol points set for " + name);
            patrolIterationCount = 0;
            unit.stateController.SetCurrentState(State.Idle);
            DetermineAction();
        }
    }

    public void IncreasePatrolPointIndex()
    {
        if (currentPatrolPointIndex == patrolPoints.Length - 1)
            currentPatrolPointIndex = 0;
        else
            currentPatrolPointIndex++;
    }

    public void AssignNextPatrolTargetPosition()
    {
        IncreasePatrolPointIndex();
        GridPosition patrolPointGridPosition = LevelGrid.Instance.GetGridPosition(patrolPoints[currentPatrolPointIndex]);
        SetTargetGridPosition(patrolPointGridPosition);
    }

    int GetNearestPatrolPointIndex()
    {
        int nearestPatrolPointIndex = 0;
        float nearestPatrolPointDistance = 0;

        for (int i = 0; i < patrolPoints.Length; i++)
        {
            if (i == 0)
                nearestPatrolPointDistance = Vector3.Distance(patrolPoints[i], transform.position);
            else
            {
                float dist = Vector3.Distance(patrolPoints[i], transform.position);
                if (dist < nearestPatrolPointDistance)
                {
                    nearestPatrolPointIndex = i;
                    nearestPatrolPointDistance = dist;
                }
            }
        }

        return nearestPatrolPointIndex;
    }

    public void SetHasAlternativePatrolPoint(bool hasAlternativePatrolPoint) => this.hasAlternativePatrolPoint = hasAlternativePatrolPoint;
    #endregion

    #region Wander
    public void Wander()
    {
        if (wanderPositionSet == false)
        {
            wanderGridPosition = GetNewWanderPosition();
            if (wanderGridPosition == unit.gridPosition)
                TurnManager.Instance.FinishTurn(unit);
            else
            {
                wanderPositionSet = true;
                SetTargetGridPosition(wanderGridPosition);
            }

            // Queue the Move Action if the Unit isn't already moving
            if (GetAction<MoveAction>().isMoving == false)
                QueueAction(GetAction<MoveAction>(), GetAction<MoveAction>().GetActionPointsCost(targetGridPosition));
        }
        // If the NPC has arrived at their destination
        else if (Vector3.Distance(wanderGridPosition.WorldPosition(), transform.position) <= 0.1f)
        {
            // Get a new Wander Position when the current one is reached
            wanderPositionSet = false;
            Wander();
            return;
        }
        else if (GetAction<MoveAction>().isMoving == false)
        {
            // Get a new Wander Position if there's now another Unit there
            if (LevelGrid.Instance.HasAnyUnitOnGridPosition(wanderGridPosition) && LevelGrid.Instance.GetUnitAtGridPosition(wanderGridPosition) != unit)
            {
                wanderGridPosition = GetNewWanderPosition();
                SetTargetGridPosition(wanderGridPosition);
            }

            QueueAction(GetAction<MoveAction>(), GetAction<MoveAction>().GetActionPointsCost(targetGridPosition));
        }
    }

    GridPosition GetNewWanderPosition() => LevelGrid.Instance.GetRandomGridPositionInRange(LevelGrid.Instance.GetGridPosition(defaultPosition), unit, minWanderDistance, maxWanderDistance); 
    #endregion

    public void SetLeader(Unit newLeader) => leader = newLeader;

    public void SetShouldFollowLeader(bool shouldFollowLeader) => this.shouldFollowLeader = shouldFollowLeader;

    public void ResetToDefaults()
    {
        hasAlternativePatrolPoint = false;
        wanderPositionSet = false;
        needsFleeDestination = true;
        initialPatrolPointSet = false;
        patrolIterationCount = 0;

        fleeDestination = Vector3.zero;
        distToFleeDestination = 0;
    }

    public Vector3[] PatrolPoints() => patrolPoints;
}
