using System.Collections.Generic;
using System;
using UnitSystem.ActionSystem.GOAP.Goals;

namespace UnitSystem.ActionSystem.GOAP.GoalActions
{
    public class GoalAction_Idle : GoalAction_Base
    {
        readonly List<Type> supportedGoals = new(new Type[] { typeof(Goal_Idle) });

        public override List<Type> SupportedGoals() => supportedGoals;

        public override float Cost() => 0f;

        public override void OnActivated(Goal_Base linkedGoal)
        {
            base.OnActivated(linkedGoal);
            unit.UnitActionHandler.SkipTurn();
        }
    }
}
