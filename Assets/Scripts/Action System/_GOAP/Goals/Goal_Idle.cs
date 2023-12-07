using UnityEngine;

namespace UnitSystem.ActionSystem.GOAP.Goals
{
    public class Goal_Idle : Goal_Base
    {
        [SerializeField] int priority = 1;

        public override int CalculatePriority() => unit.StateController.DefaultState == GoalState.Idle ? defaultStatePriority : priority;

        public override bool CanRun() => true;
    }
}
