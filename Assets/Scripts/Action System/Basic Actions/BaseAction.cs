using System.Collections.Generic;
using UnityEngine;
using GridSystem;
using InventorySystem;
using UnitSystem;

namespace ActionSystem
{
    public abstract class BaseAction : MonoBehaviour
    {
        public Unit unit { get; private set; }
        public GridPosition targetGridPosition { get; protected set; }

        public abstract void TakeAction();

        protected virtual void StartAction() { }

        public virtual void SetTargetGridPosition(GridPosition gridPosition) => targetGridPosition = gridPosition;

        public virtual void CompleteAction()
        {
            if (unit.IsPlayer)
                ActionSystemUI.UpdateActionVisuals();
        }

        public NPCAIAction GetBestNPCAIActionFromList(List<NPCAIAction> npcAIActionList)
        {
            npcAIActionList.Sort((NPCAIAction a, NPCAIAction b) => b.actionValue - a.actionValue);
            return npcAIActionList[0];
        }

        public void BecomeVisibleEnemyOfTarget(Unit targetUnit)
        {
            // The target Unit becomes an enemy of this Unit's faction if they weren't already
            if (unit.alliance.IsEnemy(targetUnit) == false)
            {
                targetUnit.vision.RemoveVisibleUnit(unit);
                unit.vision.RemoveVisibleUnit(targetUnit);

                targetUnit.alliance.AddEnemy(unit);
                unit.vision.AddVisibleUnit(targetUnit);
            }

            targetUnit.vision.AddVisibleUnit(unit); // The target Unit becomes aware of this Unit if they weren't already
        }

        public void BecomeVisibleAllyOfTarget(Unit targetUnit)
        {
            // The target Unit becomes an enemy of this Unit's faction if they weren't already
            if (unit.alliance.IsAlly(targetUnit) == false)
            {
                targetUnit.vision.RemoveVisibleUnit(unit);
                unit.vision.RemoveVisibleUnit(targetUnit);

                targetUnit.alliance.AddAlly(unit);
                unit.vision.AddVisibleUnit(targetUnit);
            }

            targetUnit.vision.AddVisibleUnit(unit); // The target Unit becomes aware of this Unit if they weren't already
        }

        public virtual void DamageTargets(HeldItem heldWeapon)
        {
            if (IsAttackAction() == false)
                Debug.LogWarning(GetType().Name + " is not an attack action, but it is trying to use the 'DamageTarget' method.");
            else
                Debug.LogWarning("The 'DamageTarget' method has not been implemented for the " + GetType().Name + " action.");
        }

        public virtual bool IsInAttackRange(Unit targetUnit, GridPosition startGridPosition, GridPosition targetGridPosition)
        {
            if (IsAttackAction() == false)
                Debug.LogWarning(GetType().Name + " is not an attack action, but it is trying to use the 'IsInAttackRange' method.");
            else
                Debug.LogWarning("The 'IsInAttackRange' method has not been implemented for the " + GetType().Name + " action.");
            return false;
        }

        public virtual bool IsInAttackRange(Unit targetUnit)
        {
            if (IsAttackAction() == false)
                Debug.LogWarning(GetType().Name + " is not an attack action, but it is trying to use the 'IsInAttackRange' method.");
            else
                Debug.LogWarning("The 'IsInAttackRange' method has not been implemented for the " + GetType().Name + " action.");
            return false;
        }

        public virtual GridPosition GetNearestAttackPosition(GridPosition startGridPosition, Unit targetUnit)
        {
            if (IsAttackAction() == false)
                Debug.LogWarning(GetType().Name + " is not an attack action, but it is trying to use the 'GetNearestAttackPosition' method.");
            else
                Debug.LogWarning("The 'GetNearestAttackPosition' method has not been implemented for the " + GetType().Name + " action.");
            return unit.GridPosition;
        }

        public virtual bool IsValidUnitInActionArea(GridPosition targetGridPosition)
        {
            Debug.LogWarning("The 'IsValidUnitInActionArea' method has not been implemented for the " + GetType().Name + " action.");
            return false;
        }

        public virtual List<GridPosition> GetActionAreaGridPositions(GridPosition targetGridPosition)
        {
            Debug.LogWarning("The 'GetPossibleAttackGridPositions' method has not been implemented for the " + GetType().Name + " action.");
            return null;
        }

        public virtual List<GridPosition> GetActionGridPositionsInRange(GridPosition startGridPosition)
        {
            Debug.LogWarning("The 'GetActionGridPositionsInRange' method has not been implemented for the " + GetType().Name + " action.");
            return null;
        }

        /// <summary>Determines the value of performing this BaseAction at the 'actionGridPosition'. For use when an NPC needs to determine which combat action to take.</summary>
        /// <param name="actionGridPosition">The target action position we are testing.</param>
        /// <returns>NPCAIAction with an associated actionValue. A higher actionValue is better.</returns>
        public virtual NPCAIAction GetNPCAIAction_ActionGridPosition(GridPosition actionGridPosition)
        {
            Debug.LogWarning("The 'GetNPCAIAction_ActionGridPosition' method has not been implemented for the " + GetType().Name + " action.");
            return null;
        }

        /// <summary>Determines the value of attacking the targetUnit. For use when an NPC needs to determine which enemy unit is best to set as this unit's targetEnemyUnit (in UnitActionHandler).</summary>
        /// <param name="targetUnit">The Unit we are testing.</param>
        /// <returns>NPCAIAction with an associated actionValue. A higher actionValue is better.</returns>
        public virtual NPCAIAction GetNPCAIAction_Unit(Unit targetUnit)
        {
            Debug.LogWarning("The 'GetNPCAIAction_Unit' method has not been implemented for the " + GetType().Name + " action.");
            return null;
        }

        public void SetUnit(Unit unit) => this.unit = unit;

        public bool IsDefaultAttackAction() => this is MeleeAction || this is ShootAction;

        public abstract bool IsHotbarAction();

        public abstract bool IsValidAction();

        public abstract bool IsAttackAction();

        public abstract bool IsMeleeAttackAction();

        public abstract bool IsRangedAttackAction();

        public abstract bool ActionIsUsedInstantly();

        public abstract int GetActionPointsCost();

        public abstract int GetEnergyCost();
    }
}
