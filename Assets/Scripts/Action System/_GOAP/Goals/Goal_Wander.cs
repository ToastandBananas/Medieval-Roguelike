using UnityEngine;

namespace UnitSystem.ActionSystem.GOAP.Goals
{
    public class Goal_Wander : Goal_Base
    {
        public override int CalculatePriority() => unit.StateController.DefaultState() == ActionState.Wander ? defaultStatePriority : 0;

        public override bool CanRun() => true;
    }
}
