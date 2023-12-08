using UnitSystem.ActionSystem.GOAP.GoalActions;
using UnityEngine;

namespace UnitSystem.ActionSystem.GOAP.Goals
{
    public class Goal_Idle : Goal_Base
    {
        [SerializeField] int priority = 1;

        public override void OnGoalActivated(GoalAction_Base linkedGoalAction)
        {
            base.OnGoalActivated(linkedGoalAction);
            unit.StateController.SetCurrentState(GoalState.Idle);
        }

        public override int CalculatePriority() => unit.StateController.DefaultState == GoalState.Idle ? defaultStatePriority : priority;

        public override bool CanRun() => true;
    }
}