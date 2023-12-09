using GridSystem;
using System;
using System.Collections.Generic;
using UnitSystem.ActionSystem.GOAP.Goals;
using UnityEngine;

namespace UnitSystem.ActionSystem.GOAP.GoalActions
{
    public class GoalAction_Patrol : GoalAction_Base
    {
        [SerializeField] Vector3[] patrolPoints;
        public int CurrentPatrolPointIndex { get; private set; }
        bool initialPatrolPointSet, hasAlternativePatrolPoint;
        int patrolIterationCount;
        readonly int maxPatrolIterations = 5;

        public override void OnTick()
        {
            Patrol();
        }

        public override void OnDeactivated()
        {
            base.OnDeactivated();
            hasAlternativePatrolPoint = false;
            initialPatrolPointSet = false;
            patrolIterationCount = 0;
        }

        #region Patrol
        void Patrol()
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
                    // Get the closest Patrol Point to the unit as the first Patrol Point to move to
                    CurrentPatrolPointIndex = GetNearestPatrolPointIndex();
                    initialPatrolPointSet = true;
                }

                GridPosition patrolPointGridPosition = LevelGrid.GetGridPosition(patrolPoints[CurrentPatrolPointIndex]);

                // If the Patrol Point is set to an invalid Grid Position
                if (LevelGrid.IsValidGridPosition(patrolPointGridPosition) == false)
                {
                    // Debug.LogWarning(patrolPointGridPosition + " is not a valid grid position...");
                    IncreasePatrolPointIndex();
                    return;
                }
                // If there's another unit currently on the Patrol Point or Alternative Patrol Point
                else if ((hasAlternativePatrolPoint == false && LevelGrid.GridPositionObstructed(patrolPointGridPosition) && LevelGrid.GetUnitAtGridPosition(patrolPointGridPosition) != unit)
                    || (hasAlternativePatrolPoint && LevelGrid.GridPositionObstructed(npcActionHandler.MoveAction.TargetGridPosition) && LevelGrid.GetUnitAtGridPosition(npcActionHandler.MoveAction.TargetGridPosition) != unit))
                {
                    // Increase the iteration count just in case we had to look for an Alternative Patrol Point due to something obstructing the current Target Grid Position
                    patrolIterationCount++;

                    // Find the nearest Grid Position to the Patrol Point
                    GridPosition nearestGridPositionToPatrolPoint = LevelGrid.FindNearestValidGridPosition(patrolPointGridPosition, unit, 5);
                    if (patrolPointGridPosition == nearestGridPositionToPatrolPoint)
                        IncreasePatrolPointIndex();

                    hasAlternativePatrolPoint = true;
                    npcActionHandler.MoveAction.SetTargetGridPosition(nearestGridPositionToPatrolPoint);

                    if (nearestGridPositionToPatrolPoint != patrolPointGridPosition && LevelGrid.GridPositionObstructed(nearestGridPositionToPatrolPoint) == false)
                        patrolIterationCount = 0;
                }

                // If the unit has arrived at their current Patrol Point or Alternative Patrol Point position
                if (Vector3.Distance(npcActionHandler.MoveAction.TargetGridPosition.WorldPosition, transform.position) <= 0.1f)
                {
                    if (hasAlternativePatrolPoint)
                        hasAlternativePatrolPoint = false;

                    // Set the unit's Target Grid Position as the next Patrol Point
                    IncreasePatrolPointIndex();
                    patrolPointGridPosition = LevelGrid.GetGridPosition(patrolPoints[CurrentPatrolPointIndex]);
                    npcActionHandler.MoveAction.SetTargetGridPosition(patrolPointGridPosition);
                }
                // Otherwise, assign their target position to the Patrol Point if it's not already set
                else if (hasAlternativePatrolPoint == false && npcActionHandler.MoveAction.TargetGridPosition.WorldPosition != patrolPoints[CurrentPatrolPointIndex])
                {
                    npcActionHandler.MoveAction.SetTargetGridPosition(patrolPointGridPosition);

                    // Don't reset the patrol iteration count if the next target position is the unit's current position, because we'll need to iterate through Patrol again
                    if (npcActionHandler.MoveAction.TargetGridPosition != unit.GridPosition)
                        patrolIterationCount = 0;
                }

                // Queue the Move Action if the unit isn't already moving
                if (npcActionHandler.MoveAction.IsMoving == false)
                    npcActionHandler.MoveAction.QueueAction(npcActionHandler.MoveAction.TargetGridPosition);
            }
            else // If no Patrol Points set
            {
                Debug.LogWarning("No patrol points set for " + name);
                patrolIterationCount = 0;

                if (unit.StateController.DefaultState == GoalState.Patrol)
                    unit.StateController.ChangeDefaultState(GoalState.Idle);

                npcActionHandler.DetermineAction();
            }
        }

        public void IncreasePatrolPointIndex()
        {
            if (CurrentPatrolPointIndex == patrolPoints.Length - 1)
                CurrentPatrolPointIndex = 0;
            else
                CurrentPatrolPointIndex++;
        }

        public void AssignNextPatrolTargetPosition()
        {
            IncreasePatrolPointIndex();
            GridPosition patrolPointGridPosition = LevelGrid.GetGridPosition(patrolPoints[CurrentPatrolPointIndex]);
            npcActionHandler.MoveAction.SetTargetGridPosition(patrolPointGridPosition);
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

        public Vector3[] PatrolPoints => patrolPoints;

        public int PatrolPointCount => patrolPoints.Length;
        #endregion
    }
}
