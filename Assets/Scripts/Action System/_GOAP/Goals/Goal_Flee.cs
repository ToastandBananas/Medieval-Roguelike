using System;
using System.Collections.Generic;
using UnitSystem.ActionSystem.GOAP.GoalActions;
using UnityEngine;

namespace UnitSystem.ActionSystem.GOAP.Goals
{
    public class Goal_Flee : Goal_Base
    {
        [Tooltip("Needs to remain higher than fight goal priority")]
        [SerializeField] int fleePriority = 80;

        GoalAction_Flee fleeAction;

        readonly List<Type> supportedGoalActions = new(new Type[] { typeof(GoalAction_Flee) });

        void Start()
        {
            fleeAction = (GoalAction_Flee)goalPlanner.GetGoalAction(typeof(GoalAction_Flee));
        }

        public override List<Type> SupportedGoalActions() => supportedGoalActions;

        public override void OnTickGoal()
        {
            if (fleeAction.UnitToFleeFrom != null && fleeAction.FledFarEnough)
                fleeAction.ResetToDefaults();
        }

        public override void OnGoalActivated(GoalAction_Base linkedGoalAction)
        {
            base.OnGoalActivated(linkedGoalAction);
            unit.StateController.SetCurrentState(GoalState.Flee);
        }

        public override int CalculatePriority()
        {
            if (fleeAction.UnitToFleeFrom == null && unit.Vision.knownEnemies.Count > 0 && fleeAction.ShouldAlwaysFleeCombat)
                fleeAction.StartFlee(unit.Vision.GetClosestEnemy(true, fleeAction.DefaultFleeDistance), fleeAction.DefaultFleeDistance);

            if (fleeAction.UnitToFleeFrom != null && !fleeAction.UnitToFleeFrom.HealthSystem.IsDead && !fleeAction.FledFarEnough)
                return fleePriority;
            return -1;
        }

        public override bool CanRun() => fleeAction != null;
    }
}
