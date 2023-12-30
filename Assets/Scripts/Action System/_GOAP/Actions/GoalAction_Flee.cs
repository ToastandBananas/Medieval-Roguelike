using GridSystem;
using System;
using System.Collections.Generic;
using UnitSystem.ActionSystem.Actions;
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
        Vector3 fleeFromPosition = Vector3.zero;
        int fleeDistance;
        bool needsNewFleeDestination = true;

        public override MoveMode PreferredMoveMode() => MoveMode.Sprint;

        public override void PerformAction()
        {
            Flee();
        }

        public override void OnDeactivated()
        {
            base.OnDeactivated();
            ResetToDefaults();
        }

        #region Flee
        void Flee()
        {
            // If there's no Unit to flee from or if the Unit to flee from died
            if (fleeFromPosition == Vector3.zero && (unitToFleeFrom == null || unitToFleeFrom.HealthSystem.IsDead))
            {
                unit.StateController.SetToDefaultState();
                npcActionHandler.DetermineAction();
                return;
            }

            float distanceFromFleePosition = Vector3.Distance(fleeFromPosition, unit.WorldPosition);

            // If the Unit has fled far enough
            if (distanceFromFleePosition >= fleeDistance)
            {
                unit.StateController.SetToDefaultState();
                TurnManager.Instance.FinishTurn(unit);
                return;
            }

            // The enemy this Unit is fleeing from has moved closer or they have arrived at their flee destination, but are still too close to the enemy, so get a new flee destination
            if (unit.GridPosition == npcActionHandler.MoveAction.TargetGridPosition
                || (unitToFleeFrom != null && unitToFleeFrom.GridPosition != unitToFleeFrom_PreviousGridPosition && (unitToFleeFrom_PreviousDistance == 0f || distanceFromFleePosition + 2f <= unitToFleeFrom_PreviousDistance)))
            {
                needsNewFleeDestination = true;
            }

            GridPosition targetGridPosition = npcActionHandler.MoveAction.TargetGridPosition;
            if (needsNewFleeDestination)
            {
                needsNewFleeDestination = false;
                if (unitToFleeFrom != null)
                    unitToFleeFrom_PreviousDistance = Vector3.Distance(unitToFleeFrom.WorldPosition, unit.WorldPosition);

                targetGridPosition = GetFleeDestination();
            }

            // If there was no valid flee position, just grab a random position within range
            if (npcActionHandler.MoveAction.TargetGridPosition == unit.GridPosition)
            {
                if (unitToFleeFrom != null)
                    targetGridPosition = LevelGrid.Instance.GetRandomGridPositionInRange(unitToFleeFrom.GridPosition, unit, fleeDistance, fleeDistance + 10);
                else
                    targetGridPosition = LevelGrid.Instance.GetRandomGridPositionInRange(LevelGrid.GetGridPosition(fleeFromPosition), unit, fleeDistance, fleeDistance + 10);
            }

            npcActionHandler.MoveAction.QueueAction(targetGridPosition);
        }

        public void StartFlee(Unit unitToFleeFrom, int fleeDistance = -1)
        {
            if (unitToFleeFrom == null || unitToFleeFrom.HealthSystem.IsDead)
                return;

            unit.StateController.SetCurrentState(GoalState.Flee);
            this.unitToFleeFrom = unitToFleeFrom;
            fleeFromPosition = unitToFleeFrom.transform.position;

            if (fleeDistance == -1)
                this.fleeDistance = DefaultFleeDistance;
            else
                this.fleeDistance = fleeDistance;

            npcActionHandler.ClearActionQueue(false);
        }

        public void StartFlee(Vector3 fleeFromPosition, int fleeDistance = -1)
        {
            if (fleeFromPosition == Vector3.zero)
                return;

            unit.StateController.SetCurrentState(GoalState.Flee);
            this.fleeFromPosition = fleeFromPosition;

            if (fleeDistance == -1)
                this.fleeDistance = DefaultFleeDistance;
            else
                this.fleeDistance = fleeDistance;

            npcActionHandler.ClearActionQueue(false);
        }

        public void ResetToDefaults()
        {
            needsNewFleeDestination = true;
            unitToFleeFrom = null;
            unitToFleeFrom_PreviousDistance = 0f;
            fleeFromPosition = Vector3.zero;
            fleeDistance = 0;
        }

        GridPosition GetFleeDestination()
        {
            if (unitToFleeFrom != null)
                return LevelGrid.Instance.GetRandomFleeGridPosition(unit, unitToFleeFrom.WorldPosition, fleeDistance, fleeDistance + 10);
            return LevelGrid.Instance.GetRandomFleeGridPosition(unit, fleeFromPosition, fleeDistance, fleeDistance + 10);
        }

        public void SetUnitToFleeFrom(Unit unitToFleeFrom) => this.unitToFleeFrom = unitToFleeFrom;

        public bool FledFarEnough =>
            (fleeFromPosition == Vector3.zero && unitToFleeFrom == null) 
            || (unitToFleeFrom != null && Vector3.Distance(unitToFleeFrom.WorldPosition, unit.WorldPosition) >= fleeDistance) 
            || (unitToFleeFrom == null && Vector3.Distance(fleeFromPosition, unit.WorldPosition) >= fleeDistance);

        public int DefaultFleeDistance => defaultFleeDistance;

        public bool ShouldAlwaysFleeCombat => shouldAlwaysFleeCombat;

        public Unit UnitToFleeFrom => unitToFleeFrom;

        public Vector3 FleeFromPosition => fleeFromPosition;
        #endregion
    }
}
