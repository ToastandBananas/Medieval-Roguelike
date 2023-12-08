using UnityEngine;

namespace UnitSystem.ActionSystem.GOAP.Goals
{
    public class Goal_FindWeapon : Goal_Base
    {
        [Tooltip("Needs to remain higher than fight goal priority")]
        [SerializeField] int priority = 65;

        public override int CalculatePriority()
        {
            if (unit.Vision.knownEnemies.Count == 0 && unit.UnitActionHandler.TargetEnemyUnit == null)
                return 0;

            if (unit.Stats.CanFightUnarmed)
                return priority - Mathf.RoundToInt(unit.Stats.UnarmedSkill.GetValue() / 10f);

            return priority;
        }

        public override bool CanRun()
        {
            if (!unit.UnitEquipment.IsUnarmed)
                return false;
            return true;
        }
    }
}
