using System;
using System.Collections.Generic;
using UnitSystem.ActionSystem.GOAP.GoalActions;
using UnityEngine;

namespace UnitSystem.ActionSystem.GOAP.Goals
{
    public class Goal_Fight : Goal_Base
    {
        [SerializeField] int fightPriority = 60;

        int currentPriority = 0; 
        
        GoalAction_Fight fightAction;

        readonly List<Type> supportedGoalActions = new(new Type[] { typeof(GoalAction_Fight), typeof(GoalAction_FindWeapon), typeof(GoalAction_SwitchStance), typeof(GoalAction_SwapWeaponSet) });

        void Start()
        {
            fightAction = (GoalAction_Fight)goalPlanner.GetGoalAction(typeof(GoalAction_Fight));
        }

        public override List<Type> SupportedGoalActions() => supportedGoalActions;

        public override void OnTickGoal()
        {
            if (unit.UnitActionHandler.TargetEnemyUnit != null)
            {
                if (Vector3.Distance(fightAction.StartChaseGridPosition.WorldPosition, unit.WorldPosition) >= fightAction.MaxChaseDistance)
                {
                    Unit newTargetEnemy = unit.Vision.GetClosestEnemy(false);
                    if (newTargetEnemy != null)
                    {
                        fightAction.SetTargetEnemyUnit(newTargetEnemy);
                        currentPriority = fightPriority;
                        return;
                    }

                    currentPriority = 0;
                }
                else
                    currentPriority = fightPriority;
                return;
            }

            // Acquire a new target if possible
            Unit closestEnemy = unit.Vision.GetClosestEnemy(false);
            if (closestEnemy != null)
            {
                fightAction.SetTargetEnemyUnit(closestEnemy);
                currentPriority = fightPriority;
                return;
            }

            currentPriority = 0;
        }

        public override void OnGoalActivated(GoalAction_Base linkedGoalAction)
        {
            base.OnGoalActivated(linkedGoalAction);
            unit.StateController.SetCurrentState(GoalState.Fight);
            fightAction.SetStartChaseGridPosition(unit.GridPosition);
        }

        public override void OnGoalDeactivated()
        {
            base.OnGoalDeactivated();
            unit.UnitActionHandler.SetTargetEnemyUnit(null);
        }

        public override int CalculatePriority() => currentPriority;

        public override bool CanRun()
        {
            if ((unit.Vision.knownEnemies.Count == 0 && unit.UnitActionHandler.TargetEnemyUnit == null) || fightAction == null)
                return false;

            if (unit.UnitEquipment.IsUnarmed && !unit.Stats.CanFightUnarmed)
                return false;

            if (Vector3.Distance(fightAction.StartChaseGridPosition.WorldPosition, unit.WorldPosition) >= fightAction.MaxChaseDistance)
                return false;
            return true;
        }
    }
}
