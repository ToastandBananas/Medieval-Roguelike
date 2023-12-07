using UnitSystem.ActionSystem.GOAP.GoalActions;
using UnityEngine;

namespace UnitSystem.ActionSystem.GOAP.Goals
{
    public class Goal_Wander : Goal_Base
    {
        GoalAction_Wander wanderAction;

        void Start()
        {
            wanderAction = (GoalAction_Wander)goalPlanner.GetGoalAction(typeof(GoalAction_Wander));
        }

        public override int CalculatePriority() => unit.StateController.DefaultState == GoalState.Wander ? defaultStatePriority : 0;

        public override bool CanRun() => wanderAction != null;
    }
}
