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
            StanceStatModifier_ScriptableObject stanceStatModifier = heldEquipment.GetStanceStatModifier(HeldItemStance());
            if (stanceStatModifier != null)
                stanceStatModifier.StatModifier.ApplyModifiers(Unit.stats);
        }

        protected void RemoveStanceStatModifiers(HeldEquipment heldEquipment)
        {
            StanceStatModifier_ScriptableObject stanceStatModifier = heldEquipment.GetStanceStatModifier(HeldItemStance());
            if (stanceStatModifier != null)
                stanceStatModifier.StatModifier.RemoveModifiers(Unit.stats);
        }
    }
}
