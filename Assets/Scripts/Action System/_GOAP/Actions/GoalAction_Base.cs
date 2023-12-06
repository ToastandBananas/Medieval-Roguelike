using System.Collections.Generic;
using UnityEngine;
using System;
using UnitSystem.ActionSystem.GOAP.Goals;

namespace UnitSystem.ActionSystem.GOAP.GoalActions
{
    public class GoalAction_Base : MonoBehaviour
    {
        protected Unit unit;
        protected NPCActionHandler npcActionHandler;
        protected Goal_Base linkedGoal;

        void Awake()
        {
            unit = GetComponentInParent<Unit>();
            npcActionHandler = unit.UnitActionHandler as NPCActionHandler;
        }

        public virtual List<Type> SupportedGoals() => null;

        public virtual float Cost() => 0f;

        public virtual void OnActivated(Goal_Base linkedGoal) => this.linkedGoal = linkedGoal;

        public virtual void OnDeactivated() => linkedGoal = null;

        public virtual void OnTick() { }
    }
}
