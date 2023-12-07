using UnitSystem.ActionSystem.GOAP.GoalActions;
using UnityEngine;

namespace UnitSystem.ActionSystem.GOAP.Goals
{
    public class Goal_Follow : Goal_Base
    {
        GoalAction_Follow followAction;

        void Start()
        {
            followAction = (GoalAction_Follow)goalPlanner.GetGoalAction(typeof(GoalAction_Follow));
        }

        public override int CalculatePriority()
        {
            if (followAction.ShouldFollowLeader) // If should follow leader, this should have a higher priority regardless of the Unit's default goal state
                return defaultStatePriority + 1;
            else if (unit.StateController.DefaultState == GoalState.Follow)
                return defaultStatePriority;
            return 0;
        }

        public override bool CanRun() => followAction != null && followAction.Leader != null;
    }
}
