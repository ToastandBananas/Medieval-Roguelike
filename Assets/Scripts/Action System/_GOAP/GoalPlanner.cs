using System;
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

        // Cache commonly accessed actions that most NPCs will have
        public GoalAction_Fight FightAction { get; private set; }
        public GoalAction_Flee FleeAction { get; private set; }
        public GoalAction_Follow FollowAction { get; private set; }
        public GoalAction_InspectSound InspectSoundAction { get; private set; }

        void Awake()
        {
            Goals = GetComponents<Goal_Base>();
            GoalActions = GetComponentsInChildren<GoalAction_Base>();
        }

        void Start()
        {
            FightAction = GetGoalAction(typeof(GoalAction_Fight)) as GoalAction_Fight;
            FleeAction = GetGoalAction(typeof(GoalAction_Flee)) as GoalAction_Flee;
            FollowAction = GetGoalAction(typeof(GoalAction_Follow)) as GoalAction_Follow;
            InspectSoundAction = GetGoalAction(typeof(GoalAction_InspectSound)) as GoalAction_InspectSound;
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
                    if (!Goals[i].SupportedGoalActions().Contains(GoalActions[j].GetType()))
                        continue;

                    // Found a suitable Goal Action
                    // Debug.Log($"{GoalActions[j].GetType().Name} Cost: {GoalActions[j].Cost()}");
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
            {
                // Debug.Log($"{transform.parent.name}: {activeGoalAction.GetType().Name} | Priority: {activeGoalAction.LinkedGoal.CalculatePriority()} | Cost: {activeGoalAction.Cost()}");
                activeGoalAction.PerformAction();
            }
        }

        public Goal_Base GetGoal(Type goalType)
        {
            for (int i = 0; i < Goals.Length; i++)
            {
                if (Goals[i].GetType() == goalType)
                    return Goals[i];
            }
            return null;
        }

        public GoalAction_Base GetGoalAction(Type goalType)
        {
            for (int i = 0; i < GoalActions.Length; i++)
            {
                if (GoalActions[i].GetType() == goalType)
                    return GoalActions[i];
            }
            return null;
        }
    }
}
