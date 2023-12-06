using UnitSystem.ActionSystem.GOAP.GoalActions;
using UnitSystem.ActionSystem.GOAP.Goals;
using UnityEngine;

namespace UnitSystem.ActionSystem.GOAP
{
    public class GoalPlanner : MonoBehaviour
    {
        Goal_Base[] Goals;
        GoalAction_Base[] GoalActions;

        Goal_Base activeGoal;
        GoalAction_Base activeGoalAction;

        void Awake()
        {
            Goals = GetComponents<Goal_Base>();
            GoalActions = GetComponentsInChildren<GoalAction_Base>();
        }

        public void DetermineGoal()
        {
            Goal_Base bestGoal = null;
            GoalAction_Base bestGoalAction = null;
            for (int i = 0; i < Goals.Length; i++)
            {
                // Can it run?
                if (!Goals[i].CanRun())
                    continue;

                // Is it a lower priority?
                if (bestGoal != null && Goals[i].CalculatePriority() < bestGoal.CalculatePriority())
                    continue;

                // Find the best cost action
                GoalAction_Base candidateAction = null;
                for (int j = 0; j < GoalActions.Length; j++)
                {
                    // Is the Goal supported?
                    if (!GoalActions[j].SupportedGoals().Contains(Goals[i].GetType()))
                        continue;

                    // Found a suitable Goal Action
                    if (candidateAction == null || GoalActions[j].Cost() < candidateAction.Cost())
                        candidateAction = GoalActions[j];
                }

                // Did we find an action?
                if (candidateAction != null)
                {
                    bestGoal = Goals[i];
                    bestGoalAction = candidateAction;
                }
            }

            // If no current goal
            if (activeGoal == null)
            {
                activeGoal = bestGoal;
                activeGoalAction = bestGoalAction;

                if (activeGoal != null)
                    activeGoal.OnGoalActivated(activeGoalAction);

                if (activeGoalAction != null)
                    activeGoalAction.OnActivated(activeGoal);
            }
            else if (activeGoal == bestGoal) // No change in goal
            {
                // Goal Action changed?
                if (activeGoalAction != bestGoalAction)
                {
                    activeGoalAction.OnDeactivated();
                    activeGoalAction = bestGoalAction;
                    activeGoalAction.OnActivated(activeGoal);
                }
            }
            else if (activeGoal != bestGoal) // New goal or no valid goal
            {
                activeGoal.OnGoalDeactivated();
                if (activeGoalAction != null)
                    activeGoalAction.OnDeactivated();

                activeGoal = bestGoal;
                activeGoalAction = bestGoalAction;

                if (activeGoal != null)
                    activeGoal.OnGoalActivated(activeGoalAction);

                if (activeGoalAction != null)
                    activeGoalAction.OnActivated(activeGoal);
            }

            // Tick the action
            if (activeGoalAction != null)
                activeGoalAction.OnTick();
        }
    }
}
