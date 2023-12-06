using UnityEngine;

namespace UnitSystem.ActionSystem.GOAP.Goals
{
    public class Goal_Patrol : Goal_Base
    {
        public override int CalculatePriority() => unit.StateController.DefaultState() == ActionState.Patrol ? defaultStatePriority : 0;

        public override bool CanRun() => true;
    }
}
