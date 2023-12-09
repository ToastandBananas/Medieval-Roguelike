namespace UnitSystem.ActionSystem.GOAP.GoalActions
{
    public class GoalAction_Idle : GoalAction_Base
    {
        public override void OnTick()
        {
            unit.UnitActionHandler.SkipTurn();
        }
    }
}
