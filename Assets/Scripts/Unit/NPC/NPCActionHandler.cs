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
    int maxPatrolIterations = 5;

    [Header("Pursue State Variables")]
    [SerializeField] float maxChaseDistance = 15f;

    [Header("Wandering State Variables")]
    [SerializeField] Vector2 defaultPosition;
    [SerializeField] public float minRoamDistance = 5f;
    [SerializeField] public float maxRoamDistance = 20f;
    Vector2 roamPosition;
    bool roamPositionSet;

    public void TakeTurn()
    {
        if (unit.isMyTurn && unit.isDead == false)
        {
            if (unit.stats.CurrentAP() <= 0)
                StartCoroutine(TurnManager.Instance.FinishTurn(unit));
            else
            {
                //vision.CheckEnemyVisibility();

                if (queuedAction != null)
                    StartCoroutine(GetNextQueuedAction());
                else
                    DetermineAction();
            }
        }
    }

    public override void FinishAction()
    {
        base.FinishAction();

        if (unit.stats.CurrentAP() > 0 && unit.isMyTurn && GetAction<MoveAction>().isMoving == false) // Take another action
            StartCoroutine(TurnManager.Instance.StartNextNPCsAction(unit));
            //TakeTurn();
    }

    public void DetermineAction()
    {
        switch (unit.stateController.CurrentState())
        {
            case State.Idle:
                StartCoroutine(TurnManager.Instance.FinishTurn(unit));
                break;
            case State.Patrol:
                Patrol();
                break;
            case State.Wander:
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
            Debug.Log("Max patrol iterations reached...");
            patrolIterationCount = 0;
            StartCoroutine(TurnManager.Instance.FinishTurn(unit));
            return;
        }
        else if (patrolPoints.Length > 0)
        {
            // Increase the iteration count just in case we had to look for an Alternative Patrol Point due to something obstructing the current Target Grid Position
            patrolIterationCount++;

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
                Debug.LogWarning(patrolPointGridPosition + " is not a valid grid position...");
                IncreasePatrolPointIndex();
                return;
            }
            // If there's another Unit currently on the Patrol Point or Alternative Patrol Point
            else if ((hasAlternativePatrolPoint == false && LevelGrid.Instance.HasAnyUnitOnGridPosition(patrolPointGridPosition) && LevelGrid.Instance.GetUnitAtGridPosition(patrolPointGridPosition) != unit)
                || (hasAlternativePatrolPoint && LevelGrid.Instance.HasAnyUnitOnGridPosition(targetGridPosition) && LevelGrid.Instance.GetUnitAtGridPosition(targetGridPosition) != unit))
            {
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

                // Reset the iteration count since we will have a new Target Grid Position
                patrolIterationCount = 0;

                // Set the Unit's Target Grid Position as the next Patrol Point
                IncreasePatrolPointIndex();
                patrolPointGridPosition = LevelGrid.Instance.GetGridPosition(patrolPoints[currentPatrolPointIndex]);
                SetTargetGridPosition(patrolPointGridPosition);
            }
            // Otherwise, assign their target position to the Patrol Point if it's not already set
            else if (hasAlternativePatrolPoint == false && targetGridPosition.WorldPosition() != patrolPoints[currentPatrolPointIndex])
                SetTargetGridPosition(patrolPointGridPosition);

            // Queue the Move Action if the Unit isn't already moving
            if (GetAction<MoveAction>().isMoving == false)
                QueueAction(GetAction<MoveAction>(), GetAction<MoveAction>().GetActionPointsCost(targetGridPosition));
        }
        else // If no Patrol Points set
        {
            Debug.LogWarning("No patrol points set for " + name);
            patrolIterationCount = 0;
            unit.stateController.SetToDefaultState(shouldFollowLeader);
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

    public void SetLeader(Unit newLeader) => leader = newLeader;

    public void SetShouldFollowLeader(bool shouldFollowLeader) => this.shouldFollowLeader = shouldFollowLeader;

    public void ResetToDefaults()
    {
        hasAlternativePatrolPoint = false;
        roamPositionSet = false;
        needsFleeDestination = true;
        initialPatrolPointSet = false;
        patrolIterationCount = 0;

        fleeDestination = Vector3.zero;
        distToFleeDestination = 0;
    }

    public Vector3[] PatrolPoints() => patrolPoints;
}
