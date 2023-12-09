using InventorySystem;

namespace UnitSystem.ActionSystem.Actions
{
    public abstract class Action_BaseStance : Action_Base
    {
        readonly protected int baseAPCost = 10;

        /// <summary>The chance to switch stances when the Unit's Fight goal is active.</summary>
        /// <returns>A float between 0f and 1f (0% to 100%).</returns>
        public abstract float NPCChanceToSwitchStance();

        public abstract void SwitchStance();

        public abstract HeldItemStance HeldItemStance();

        protected void ApplyStanceStatModifiers(HeldEquipment heldEquipment)
        {
            StanceStatModifier_ScriptableObject stanceStatModifier = heldEquipment.GetStanceStatModifier(HeldItemStance());
            if (stanceStatModifier != null)
                stanceStatModifier.StatModifier.ApplyModifiers(Unit.Stats);
        }

        protected void RemoveStanceStatModifiers(HeldEquipment heldEquipment)
        {
            StanceStatModifier_ScriptableObject stanceStatModifier = heldEquipment.GetStanceStatModifier(HeldItemStance());
            if (stanceStatModifier != null)
                stanceStatModifier.StatModifier.RemoveModifiers(Unit.Stats);
        }
    }
}
