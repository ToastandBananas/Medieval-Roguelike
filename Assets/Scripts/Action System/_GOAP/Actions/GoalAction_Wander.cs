using GridSystem;
using System;
using System.Collections.Generic;
using UnitSystem.ActionSystem.GOAP.Goals;
using UnityEngine;
using Random = UnityEngine.Random;

namespace UnitSystem.ActionSystem.GOAP.GoalActions
{
    public class GoalAction_Wander : GoalAction_Base
    {
        [SerializeField] int minWanderDistance = 5;
        [SerializeField] int maxWanderDistance = 20;
        GridPosition wanderGridPosition;
        bool wanderPositionSet;

        readonly List<Type> supportedGoals = new(new Type[] { typeof(Goal_Wander) });

        public override List<Type> SupportedGoals() => supportedGoals;

        public override float Cost() => 0f;

        public override void OnTick()
        {
            Wander();
        }

        public override void OnDeactivated()
        {
            base.OnDeactivated();
            wanderPositionSet = false;
        }

        #region Wander
        void Wander()
        {
            if (!wanderPositionSet)
            {
                wanderGridPosition = GetNewWanderPosition();
                if (wanderGridPosition == unit.GridPosition)
                    TurnManager.Instance.FinishTurn(unit);
                else
                {
                    wanderPositionSet = true;
                    unit.UnitActionHandler.MoveAction.SetTargetGridPosition(wanderGridPosition);
                }

                // Queue the Move Action if the Unit isn't already moving
                if (!unit.UnitActionHandler.MoveAction.IsMoving)
                    unit.UnitActionHandler.MoveAction.QueueAction(wanderGridPosition);
            }
            // If the NPC has arrived at their destination
            else if (Vector3.Distance(wanderGridPosition.WorldPosition, transform.position) <= 0.1f)
            {
                // Get a new Wander Position when the current one is reached
                wanderPositionSet = false;
                Wander();
            }
            else if (!unit.UnitActionHandler.MoveAction.IsMoving)
            {
                // Get a new Wander Position if there's now another Unit or obstruction there
                if (LevelGrid.GridPositionObstructed(wanderGridPosition))
                    wanderGridPosition = GetNewWanderPosition();

                unit.UnitActionHandler.MoveAction.QueueAction(wanderGridPosition);
            }
        }

        GridPosition GetNewWanderPosition()
        {
            float distance = Random.Range(minWanderDistance, maxWanderDistance);
            Vector3 randomDirection = Random.insideUnitSphere;
            randomDirection.y = 0;
            Vector3 randomPosition = randomDirection * distance + transform.position;
            return LevelGrid.GetGridPosition((Vector3)AstarPath.active.GetNearest(randomPosition).node.position);
        }
        #endregion
    }
}
