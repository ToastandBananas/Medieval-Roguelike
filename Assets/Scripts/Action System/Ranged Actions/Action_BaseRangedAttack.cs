using GridSystem;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace UnitSystem.ActionSystem.Actions
{
    public abstract class Action_BaseRangedAttack : Action_BaseAttack
    {
        public abstract bool TryHitTarget(GridPosition targetGridPosition);
    }
}
