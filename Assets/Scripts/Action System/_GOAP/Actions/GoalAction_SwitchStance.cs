using UnitSystem.ActionSystem.Actions;
using UnityEngine;

namespace UnitSystem.ActionSystem.GOAP.GoalActions
{
    public class GoalAction_SwitchStance : GoalAction_Base
    {
        Action_BaseStance chosenStanceAction;
        GoalAction_Fight fightAction;

        void Start()
        {
            fightAction = (GoalAction_Fight)npcActionHandler.GoalPlanner.GetGoalAction(typeof(GoalAction_Fight));
        }

        public override float Cost()
        {
            chosenStanceAction = null;
            for (int i = 0; i < unit.UnitActionHandler.AvailableStanceActions.Count; i++)
            {
                // Is the stance action valid and does the Unit have enough energy?
                if (!unit.UnitActionHandler.AvailableStanceActions[i].IsValidAction() || !unit.Stats.HasEnoughEnergy(unit.UnitActionHandler.AvailableStanceActions[i].InitialEnergyCost()))
                    continue;

                if (Random.Range(0f, 1f) < unit.UnitActionHandler.AvailableStanceActions[i].NPCChanceToSwitchStance())
                {
                    chosenStanceAction = unit.UnitActionHandler.AvailableStanceActions[i];
                    return 5f;
                }
            }

            // Don't switch stances
            return 100f;
        }

        public override void OnTick()
        {
            SwitchStance();
        }

        void SwitchStance()
        {
            if (chosenStanceAction != null)
                chosenStanceAction.QueueAction();
            else if (fightAction != null)
                fightAction.OnTick();
            else
                TurnManager.Instance.FinishTurn(unit);
        }
    }
}
