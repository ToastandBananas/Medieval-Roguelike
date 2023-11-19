using InventorySystem;

namespace UnitSystem.ActionSystem 
{
    public abstract class BaseStanceAction : BaseAction
    {
        readonly protected int baseAPCost = 10;

        public abstract void SwitchStance();

        public abstract HeldItemStance HeldItemStance();

        protected void ApplyStanceStatModifiers(HeldEquipment heldEquipment)
        {
            StanceStatModifier_ScriptableObject stanceStatModifier = heldEquipment.GetStanceStatModifier(InventorySystem.HeldItemStance.RaiseShield);
            if (stanceStatModifier != null)
                stanceStatModifier.StatModifier.ApplyModifiers(unit.stats);
        }

        protected void RemoveStanceStatModifiers(HeldEquipment heldEquipment)
        {
            StanceStatModifier_ScriptableObject stanceStatModifier = heldEquipment.GetStanceStatModifier(InventorySystem.HeldItemStance.RaiseShield);
            if (stanceStatModifier != null)
                stanceStatModifier.StatModifier.RemoveModifiers(unit.stats);
        }
    }
}
