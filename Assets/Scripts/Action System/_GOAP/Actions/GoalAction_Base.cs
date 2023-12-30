using UnityEngine;
using UnitSystem.ActionSystem.GOAP.Goals;
using UnitSystem.ActionSystem.Actions;

namespace UnitSystem.ActionSystem.GOAP.GoalActions
{
    public abstract class GoalAction_Base : MonoBehaviour
    {
        protected Unit unit;
        protected NPCActionHandler npcActionHandler;
        public Goal_Base LinkedGoal { get; protected set; }

        void Awake()
        {
            unit = GetComponentInParent<Unit>();
            npcActionHandler = unit.UnitActionHandler as NPCActionHandler;
        }

        public virtual MoveMode PreferredMoveMode() => MoveMode.Walk;

        /// <summary>Cost determines which Goal Action will be chosen for a Goal. (Lowest cost will be chosen).</summary>
        public virtual float Cost() => 0f;

        public virtual void OnActivated(Goal_Base linkedGoal)
        {
            LinkedGoal = linkedGoal;
            unit.UnitActionHandler.MoveAction.SetMoveMode(PreferredMoveMode());
        }

        public virtual void OnDeactivated() => LinkedGoal = null;

        /// <summary>This is where the logic for the action should go.</summary>
        public abstract void PerformAction();
    }
}
