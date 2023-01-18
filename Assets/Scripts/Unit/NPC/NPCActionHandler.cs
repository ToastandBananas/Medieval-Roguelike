using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
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

                if (queuedActions.Count > 0)
                    StartCoroutine(GetNextQueuedAction());
                else
                    DetermineAction();
            }
        }
    }

    public override void FinishAction()
    {
        base.FinishAction();

        if (unit.stats.CurrentAP() > 0 && GetAction<MoveAction>().isMoving == false) // Take another action
            TakeTurn();
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
        if (patrolIterationCount > maxPatrolIterations)
        {
            Debug.Log("Max patrol iterations reached...");
            patrolIterationCount = 0;
            StartCoroutine(TurnManager.Instance.FinishTurn(unit));
            return;
        }
        else if (patrolPoints.Length > 0)
        {
            patrolIterationCount++;

            if (initialPatrolPointSet == false)
            {
                currentPatrolPointIndex = GetNearestPatrolPointIndex();
                initialPatrolPointSet = true;
            }

            GridPosition patrolPointGridPosition = LevelGrid.Instance.GetGridPosition(patrolPoints[currentPatrolPointIndex]);
            if (LevelGrid.Instance.IsValidGridPosition(patrolPointGridPosition) == false)
            {
                IncreasePatrolPointIndex();
                Patrol();
                return;
            }
            else if ((hasAlternativePatrolPoint == false && LevelGrid.Instance.HasAnyUnitOnGridPosition(patrolPointGridPosition) && LevelGrid.Instance.GetUnitAtGridPosition(patrolPointGridPosition) != unit)
                || (hasAlternativePatrolPoint && LevelGrid.Instance.HasAnyUnitOnGridPosition(targetGridPosition) && LevelGrid.Instance.GetUnitAtGridPosition(targetGridPosition) != unit))
            {
                GridPosition nearestGridPosition = LevelGrid.Instance.FindNearestValidGridPosition(patrolPointGridPosition, unit);
                if (patrolPointGridPosition == nearestGridPosition)
                {
                    IncreasePatrolPointIndex();
                    Patrol();
                    return;
                }

                hasAlternativePatrolPoint = true;
                SetTargetGridPosition(nearestGridPosition);
                GetAction<MoveAction>().SetTargetGridPosition(nearestGridPosition);
            }

            if ((hasAlternativePatrolPoint == false && Vector3.Distance(patrolPoints[currentPatrolPointIndex], transform.position) <= 0.1f) || (hasAlternativePatrolPoint && Vector3.Distance(targetGridPosition.WorldPosition(), transform.position) <= 0.1f))
            {
                if (hasAlternativePatrolPoint)
                    hasAlternativePatrolPoint = false;

                IncreasePatrolPointIndex();
                SetTargetGridPosition(patrolPointGridPosition);
                GetAction<MoveAction>().SetTargetGridPosition(targetGridPosition);
            }
            else if (hasAlternativePatrolPoint == false && targetGridPosition.WorldPosition() != patrolPoints[currentPatrolPointIndex])
            {
                SetTargetGridPosition(patrolPointGridPosition);
                GetAction<MoveAction>().SetTargetGridPosition(targetGridPosition);
            }

            if (GetAction<MoveAction>().isMoving == false)
            {
                patrolIterationCount = 0;
                QueueAction(GetAction<MoveAction>(), GetAction<MoveAction>().GetActionPointsCost());
            }
        }
        else
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
