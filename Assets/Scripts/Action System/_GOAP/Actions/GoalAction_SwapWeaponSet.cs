using UnitSystem.ActionSystem.Actions;
using UnityEngine;

namespace UnitSystem.ActionSystem.GOAP.GoalActions
{
    public class GoalAction_SwapWeaponSet : GoalAction_Base
    {
        public override float Cost()
        {
            if (unit.UnitActionHandler.TargetEnemyUnit == null)
                return 100f;

            float distanceToTargetEnemy = Vector3.Distance(unit.WorldPosition, unit.UnitActionHandler.TargetEnemyUnit.WorldPosition);
            if (unit.UnitEquipment.RangedWeaponEquipped) 
            {
                if (distanceToTargetEnemy <= npcActionHandler.GoalPlanner.FightAction.DistanceToPreferMeleeCombat || !unit.UnitEquipment.HumanoidEquipment.HasValidAmmunitionEquipped())
                    return 0f; // Swap to melee weapon
            }
            else // If melee weapon equipped or unarmed
            {
                if (distanceToTargetEnemy > npcActionHandler.GoalPlanner.FightAction.DistanceToPreferMeleeCombat && unit.UnitEquipment.HumanoidEquipment.OtherWeaponSet_IsRanged() && unit.UnitEquipment.HumanoidEquipment.HasValidAmmunitionEquipped())
                    return 0f; // Swap to ranged weapon
            }
            
            // Don't swap
            return 100f;
        }

        public override void PerformAction()
        {
            SwapWeaponSet();
        }

        void SwapWeaponSet() => unit.UnitActionHandler.GetAction<Action_SwapWeaponSet>().QueueAction();
    }
}
