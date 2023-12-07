using GridSystem;
using System;
using System.Collections.Generic;
using UnitSystem.ActionSystem.GOAP.Goals;
using UnityEngine;

namespace UnitSystem.ActionSystem.GOAP.GoalActions
{
    public class GoalAction_Flee : GoalAction_Base
    {
        [SerializeField] int defaultFleeDistance = 20;
        [SerializeField] bool shouldAlwaysFleeCombat;
        Unit unitToFleeFrom;
        GridPosition unitToFleeFrom_PreviousGridPosition;
        float unitToFleeFrom_PreviousDistance;
        int fleeDistance;
        bool needsNewFleeDestination = true;

        readonly List<Type> supportedGoals = new(new Type[] { typeof(Goal_Flee) });

        public override void OnActivated(Goal_Base linkedGoal)
        {
            base.OnActivated(linkedGoal);
            Flee();
        }

        public override void OnDeactivated()
        {
            base.OnDeactivated();
            ResetToDefaults();
        }

        public override List<Type> SupportedGoals() => supportedGoals;

        #region Flee
        void Flee()
        {
            // If there's no Unit to flee from or if the Unit to flee from died
            if (unitToFleeFrom == null || unitToFleeFrom.Health.IsDead)
            {
                unit.StateController.SetToDefaultState(); // Variables are reset in this method
                npcActionHandler.DetermineAction();
                return;
            }

            float distanceFromUnitToFleeFrom = Vector3.Distance(unitToFleeFrom.WorldPosition, unit.WorldPosition);

            // If the Unit has fled far enough
            if (distanceFromUnitToFleeFrom >= fleeDistance)
            {
                unit.StateController.SetToDefaultState(); // Variables are also reset in this method
                npcActionHandler.DetermineAction();
                return;
            }

            // The enemy this Unit is fleeing from has moved closer or they have arrived at their flee destination, but are still too close to the enemy, so get a new flee destination
            if (unit.GridPosition == npcActionHandler.MoveAction.TargetGridPosition || (unitToFleeFrom.GridPosition != unitToFleeFrom_PreviousGridPosition && (unitToFleeFrom_PreviousDistance == 0f || distanceFromUnitToFleeFrom + 2f <= unitToFleeFrom_PreviousDistance)))
                needsNewFleeDestination = true;

            GridPosition targetGridPosition = npcActionHandler.MoveAction.TargetGridPosition;
            if (needsNewFleeDestination)
            {
                needsNewFleeDestination = false;
                unitToFleeFrom_PreviousDistance = Vector3.Distance(unitToFleeFrom.WorldPosition, unit.WorldPosition);
                targetGridPosition = GetFleeDestination();
            }

            // If there was no valid flee position, just grab a random position within range
            if (npcActionHandler.MoveAction.TargetGridPosition == unit.GridPosition)
                targetGridPosition = LevelGrid.Instance.GetRandomGridPositionInRange(unitToFleeFrom.GridPosition, unit, fleeDistance, fleeDistance + 15);

            npcActionHandler.MoveAction.QueueAction(targetGridPosition);
        }

        public void StartFlee(Unit unitToFleeFrom, int fleeDistance)
        {
            if (unitToFleeFrom == null)
                return;

            unit.StateController.SetCurrentState(GoalState.Flee);
            this.unitToFleeFrom = unitToFleeFrom;
            this.fleeDistance = fleeDistance;
            npcActionHandler.ClearActionQueue(false);
        }

        public void ResetToDefaults()
        {
            needsNewFleeDestination = true;
            unitToFleeFrom = null;
            unitToFleeFrom_PreviousDistance = 0f;
            fleeDistance = 0;
        }

        GridPosition GetFleeDestination() => LevelGrid.Instance.GetRandomFleeGridPosition(unit, unitToFleeFrom, fleeDistance, fleeDistance + 15);

        public void SetUnitToFleeFrom(Unit unitToFleeFrom) => this.unitToFleeFrom = unitToFleeFrom;

        public bool FledFarEnough => unitToFleeFrom == null || Vector3.Distance(unitToFleeFrom.WorldPosition, unit.WorldPosition) >= fleeDistance;

        public int DefaultFleeDistance => defaultFleeDistance;

        public bool ShouldAlwaysFleeCombat => shouldAlwaysFleeCombat;

        public Unit UnitToFleeFrom => unitToFleeFrom;
        #endregion
    }
}
