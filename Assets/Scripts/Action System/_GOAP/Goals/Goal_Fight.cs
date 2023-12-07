using UnitSystem.ActionSystem.GOAP.GoalActions;
using UnityEngine;

namespace UnitSystem.ActionSystem.GOAP.Goals
{
    public class Goal_Fight : Goal_Base
    {
        [SerializeField] int fightPriority = 60;

        int currentPriority = 0; 
        
        GoalAction_Fight fightAction;

        void Start()
        {
            fightAction = (GoalAction_Fight)goalPlanner.GetGoalAction(typeof(GoalAction_Fight));
        }

        public override void OnTickGoal()
        {
            if (unit.Vision.knownEnemies.Count == 0 || fightAction == null)
                return;

            if (unit.UnitActionHandler.TargetEnemyUnit != null)
            {
                // Check if the current target is still within the max chase distance (from the start chase position)
                for (int i = 0; i < unit.Vision.knownEnemies.Count; i++)
                {
                    if (unit.Vision.knownEnemies[i] == unit.UnitActionHandler.TargetEnemyUnit)
                    {
                        if (Vector3.Distance(fightAction.StartChaseGridPosition.WorldPosition, unit.WorldPosition) > fightAction.MaxChaseDistance)
                            currentPriority = 0;
                        else
                            currentPriority = fightPriority;
                        return;
                    }
                }

                // If not, clear our current target
                unit.UnitActionHandler.SetTargetEnemyUnit(null);
            }

            // Acquire a new target if possible
            for (int i = 0; i < unit.Vision.knownEnemies.Count; i++)
            {
                // Found a new target
                if (Vector3.Distance(fightAction.StartChaseGridPosition.WorldPosition, unit.WorldPosition) <= fightAction.MaxChaseDistance)
                {
                    currentPriority = fightPriority;
                    return;
                }
            }

            currentPriority = 0;
        }

        public override void OnGoalDeactivated()
        {
            base.OnGoalDeactivated();
            unit.UnitActionHandler.SetTargetEnemyUnit(null);
        }

        public override int CalculatePriority() => currentPriority;

        public override bool CanRun()
        {
            if (unit.Vision.knownEnemies.Count == 0 || fightAction == null)
                return false;

            if (unit.UnitEquipment.IsUnarmed && !unit.Stats.CanFightUnarmed)
                return false;

            if (Vector3.Distance(fightAction.StartChaseGridPosition.WorldPosition, unit.WorldPosition) <= fightAction.MaxChaseDistance || unit.Vision.knownEnemies.Count > 1)
                return true;
            return false;
        }
    }
}
