using UnitSystem.ActionSystem.Actions;
using UnityEngine;

namespace UnitSystem.ActionSystem.GOAP.GoalActions
{
    public class GoalAction_SwapWeaponSet : GoalAction_Base
    {
        [SerializeField] float distanceToPreferMeleeCombat = 3f;

        public override float Cost()
        {
            if (unit.UnitActionHandler.TargetEnemyUnit == null)
                return 100f;

            float distanceToTargetEnemy = Vector3.Distance(unit.WorldPosition, unit.UnitActionHandler.TargetEnemyUnit.WorldPosition);
            if (unit.UnitEquipment.RangedWeaponEquipped) 
            {
                if (distanceToTargetEnemy <= distanceToPreferMeleeCombat || !unit.UnitEquipment.HasValidAmmunitionEquipped())
                    return 0f; // Swap to melee weapon
            }
            else // If melee weapon equipped or unarmed
            {
                if (distanceToTargetEnemy > distanceToPreferMeleeCombat && unit.UnitEquipment.OtherWeaponSet_IsRanged() && unit.UnitEquipment.HasValidAmmunitionEquipped())
                    return 0f; // Swap to ranged weapon
            }
            
            // Don't swap
            return 100f;
        }

        public override void OnTick()
        {
            SwapWeaponSet();
        }

        void SwapWeaponSet() => unit.UnitActionHandler.GetAction<Action_SwapWeaponSet>().QueueAction();
    }
}
