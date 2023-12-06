using UnitSystem.ActionSystem.GOAP.GoalActions;
using UnityEngine;

namespace UnitSystem.ActionSystem.GOAP.Goals
{
    public class Goal_Base : MonoBehaviour
    {
        protected Unit unit;
        protected GoalAction_Base linkedGoalAction;

        protected readonly int defaultStatePriority = 20;

        void Awake()
        {
            unit = GetComponentInParent<Unit>();
        }

        public virtual bool CanRun() => false;

        public virtual int CalculatePriority() => -1;

        public virtual void OnGoalActivated(GoalAction_Base linkedGoalAction) => this.linkedGoalAction = linkedGoalAction;

        public virtual void OnGoalDeactivated() => linkedGoalAction = null;

        public virtual void OnTickGoal() { }
    }
}
