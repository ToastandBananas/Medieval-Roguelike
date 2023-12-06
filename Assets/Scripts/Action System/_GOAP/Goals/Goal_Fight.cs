using UnityEngine;

namespace UnitSystem.ActionSystem.GOAP.Goals
{
    public class Goal_Fight : Goal_Base
    {
        [SerializeField] int fightPriority = 60;

        int currentPriority = 0;

        public override void OnTickGoal()
        {
            if (unit.Vision.knownEnemies.Count == 0)
                return;

            if (unit.UnitActionHandler.TargetEnemyUnit != null)
            {
                // Check if the current target is still within the max chase distance (from the start chase position)
                for (int i = 0; i < unit.Vision.knownEnemies.Count; i++)
                {
                    if (unit.Vision.knownEnemies[i] == unit.UnitActionHandler.TargetEnemyUnit)
                    {
                        if (Vector3.Distance(unit.UnitActionHandler.NPCActionHandler.StartChaseGridPosition.WorldPosition, unit.WorldPosition) > unit.UnitActionHandler.NPCActionHandler.MaxChaseDistance)
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
                if (Vector3.Distance(unit.UnitActionHandler.NPCActionHandler.StartChaseGridPosition.WorldPosition, unit.WorldPosition) <= unit.UnitActionHandler.NPCActionHandler.MaxChaseDistance)
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
            if (unit.Vision.knownEnemies.Count == 0)
                return false;

            if (unit.UnitEquipment.IsUnarmed && !unit.Stats.CanFightUnarmed)
                return false;

            if (Vector3.Distance(unit.UnitActionHandler.NPCActionHandler.StartChaseGridPosition.WorldPosition, unit.WorldPosition) <= unit.UnitActionHandler.NPCActionHandler.MaxChaseDistance || unit.Vision.knownEnemies.Count > 1)
                return true;
            return false;
        }
    }
}
