using System;
using System.Collections.Generic;
using UnitSystem.ActionSystem.GOAP.GoalActions;

namespace UnitSystem.ActionSystem.GOAP.Goals
{
    public class Goal_Patrol : Goal_Base
    {
        GoalAction_Patrol patrolAction;

        readonly List<Type> supportedGoalActions = new(new Type[] { typeof(GoalAction_Patrol) });

        void Start()
        {
            patrolAction = (GoalAction_Patrol)goalPlanner.GetGoalAction(typeof(GoalAction_Patrol));
        }

        public override List<Type> SupportedGoalActions() => supportedGoalActions;

        public override void OnGoalActivated(GoalAction_Base linkedGoalAction)
        {
            base.OnGoalActivated(linkedGoalAction);
            unit.StateController.SetCurrentState(GoalState.Patrol);
        }

        public override int CalculatePriority() => unit.StateController.DefaultState == GoalState.Patrol ? defaultStatePriority : 0;

        public override bool CanRun() => patrolAction != null && patrolAction.PatrolPointCount > 0;
    }
}
