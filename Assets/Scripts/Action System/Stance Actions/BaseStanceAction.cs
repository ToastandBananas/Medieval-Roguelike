using InventorySystem;
using UnityEngine;

namespace UnitSystem.ActionSystem 
{
    public abstract class BaseStanceAction : BaseAction
    {
        readonly int baseAPCost = 10;

        public override int GetActionPointsCost() => Mathf.RoundToInt(baseAPCost * unit.unitMeshManager.GetPrimaryHeldMeleeWeapon().itemData.Item.Weight * 0.5f);

        public abstract void SwitchStance();

        public abstract HeldItemStance HeldItemStance();
    }
}
