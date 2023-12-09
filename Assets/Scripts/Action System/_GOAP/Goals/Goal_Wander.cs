using System;
using System.Collections.Generic;
using UnitSystem.ActionSystem.GOAP.GoalActions;

namespace UnitSystem.ActionSystem.GOAP.Goals
{
    public class Goal_Wander : Goal_Base
    {
        readonly List<Type> supportedGoalActions = new(new Type[] { typeof(GoalAction_Wander) });

        public override List<Type> SupportedGoalActions() => supportedGoalActions;

        public override void OnGoalActivated(GoalAction_Base linkedGoalAction)
        {
            base.OnGoalActivated(linkedGoalAction);
            unit.StateController.SetCurrentState(GoalState.Wander);
        }

        public override int CalculatePriority() => unit.StateController.DefaultState == GoalState.Wander ? defaultStatePriority : 0;

        public override bool CanRun() => true;
    }
}
