namespace UnitSystem.ActionSystem.GOAP.GoalActions
{
    public class GoalAction_Idle : GoalAction_Base
    {
        public override void PerformAction()
        {
            unit.UnitActionHandler.SkipTurn();
        }
    }
}
