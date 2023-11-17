using InventorySystem;
using UnityEngine;

namespace UnitSystem.ActionSystem 
{
    public abstract class BaseStanceAction : BaseAction
    {
        readonly protected int baseAPCost = 10;

        public abstract void SwitchStance();

        public abstract HeldItemStance HeldItemStance();
    }
}
