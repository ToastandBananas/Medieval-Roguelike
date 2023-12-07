using UnitSystem.ActionSystem.GOAP.GoalActions;
using UnityEngine;

namespace UnitSystem.ActionSystem.GOAP.Goals
{
    public abstract class Goal_Base : MonoBehaviour
    {
        protected Unit unit;
        protected GoalPlanner goalPlanner;
        protected GoalAction_Base linkedGoalAction;

        protected readonly int defaultStatePriority = 20;

        protected virtual void Awake()
        {
            unit = GetComponentInParent<Unit>();
            goalPlanner = unit.UnitActionHandler.NPCActionHandler.GoalPlanner;
        }

        public abstract bool CanRun();

        public abstract int CalculatePriority();

        public virtual void OnGoalActivated(GoalAction_Base linkedGoalAction) => this.linkedGoalAction = linkedGoalAction;

        public virtual void OnGoalDeactivated() => linkedGoalAction = null;

        public virtual void OnTickGoal() { }
    }
}
