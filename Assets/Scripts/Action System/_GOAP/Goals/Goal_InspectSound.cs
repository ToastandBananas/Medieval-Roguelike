using System;
using System.Collections.Generic;
using UnitSystem.ActionSystem.GOAP.GoalActions;
using UnityEngine;

namespace UnitSystem.ActionSystem.GOAP.Goals
{
    public class Goal_InspectSound : Goal_Base
    {
        [Tooltip("Needs to remain lower than fight goal action priority")]
        [SerializeField] int inspectSoundPriority = 50;

        GoalAction_InspectSound inspectSoundAction;

        readonly List<Type> supportedGoalActions = new(new Type[] { typeof(GoalAction_InspectSound) });

        void Start()
        {
            inspectSoundAction = (GoalAction_InspectSound)goalPlanner.GetGoalAction(typeof(GoalAction_InspectSound));
        }

        public override List<Type> SupportedGoalActions() => supportedGoalActions;

        public override void OnGoalActivated(GoalAction_Base linkedGoalAction)
        {
            base.OnGoalActivated(linkedGoalAction);
            unit.StateController.SetCurrentState(GoalState.InspectSound);
        }

        public override int CalculatePriority()
        {
            if (unit.StateController.CurrentState == GoalState.InspectSound)
                return inspectSoundPriority;
            return -1;
        }

        public override bool CanRun() => inspectSoundAction != null && unit.StateController.CurrentState == GoalState.InspectSound;
    }
}
