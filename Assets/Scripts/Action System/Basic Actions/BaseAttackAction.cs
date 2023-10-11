using GridSystem;
using InventorySystem;
using UnitSystem;

namespace ActionSystem
{
    public abstract class BaseAttackAction : BaseAction
    {
        protected Unit targetEnemyUnit;

        public virtual void QueueAction(Unit targetEnemyUnit)
        {
            this.targetEnemyUnit = targetEnemyUnit;
            targetGridPosition = targetEnemyUnit.GridPosition;
            QueueAction();
        }

        public abstract void DamageTargets(HeldItem heldWeapon);

        public abstract bool IsInAttackRange(Unit targetUnit, GridPosition startGridPosition, GridPosition targetGridPosition);

        public abstract bool IsInAttackRange(Unit targetUnit);

        public abstract GridPosition GetNearestAttackPosition(GridPosition startGridPosition, Unit targetUnit);

        public abstract bool IsMeleeAttackAction();

        public abstract bool IsRangedAttackAction();
    }
}
