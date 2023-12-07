using UnitSystem.ActionSystem.GOAP.GoalActions;
using UnityEngine;

namespace UnitSystem.ActionSystem.GOAP.Goals
{
    public class Goal_Flee : Goal_Base
    {
        [SerializeField] int fleePriority = 80;
        GoalAction_Flee fleeAction;

        void Start()
        {
            fleeAction = (GoalAction_Flee)goalPlanner.GetGoalAction(typeof(GoalAction_Flee));
        }

        public override void OnTickGoal()
        {
            if (fleeAction.UnitToFleeFrom != null && fleeAction.FledFarEnough)
                fleeAction.ResetToDefaults();
        }

        public override int CalculatePriority()
        {
            if (fleeAction.UnitToFleeFrom == null && unit.Vision.knownEnemies.Count > 0 && fleeAction.ShouldAlwaysFleeCombat)
                fleeAction.StartFlee(unit.Vision.GetClosestEnemy(true, fleeAction.DefaultFleeDistance), fleeAction.DefaultFleeDistance);

            if (fleeAction.UnitToFleeFrom != null && !fleeAction.UnitToFleeFrom.Health.IsDead && !fleeAction.FledFarEnough)
                return fleePriority;
            return -1;
        }

        public override bool CanRun() => fleeAction != null;
    }
}
