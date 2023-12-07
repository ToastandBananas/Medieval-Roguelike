using UnitSystem.ActionSystem.GOAP.GoalActions;
using UnityEngine;

namespace UnitSystem.ActionSystem.GOAP.Goals
{
    public class Goal_InspectSound : Goal_Base
    {
        [SerializeField] int inspectSoundPriority = 50; // Needs to remain lower than fight goal action priority
        GoalAction_InspectSound inspectSoundAction;

        void Start()
        {
            inspectSoundAction = (GoalAction_InspectSound)goalPlanner.GetGoalAction(typeof(GoalAction_InspectSound));
        }

        public override int CalculatePriority()
        {
            if (unit.StateController.CurrentState == GoalState.InspectSound)
                return inspectSoundPriority;
            return 0;
        }

        public override bool CanRun() => inspectSoundAction != null && unit.StateController.CurrentState == GoalState.InspectSound;
    }
}
