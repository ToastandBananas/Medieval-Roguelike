using UnityEngine;

public enum State { Idle, Patrol, Wander, Follow, MoveToTarget, Fight, Flee, Hunt, FindFood }

public class StateController : MonoBehaviour
{
    State defaultState = State.Idle;
    State currentState = State.Idle;

    Unit unit;

    void Start()
    {
        unit = GetComponent<Unit>();

        if (currentState == State.Idle)
            SetToDefaultState(unit.UnitActionHandler().GetAction<MoveAction>().shouldFollowLeader);
    }

    public State CurrentState() => currentState;

    public void SetCurrentState(State state)
    {
        unit.UnitActionHandler().GetAction<MoveAction>().ResetToDefaults();
        currentState = state;
    }

    public void SetToDefaultState(bool shouldFollowLeader)
    {
        unit.UnitActionHandler().GetAction<MoveAction>().ResetToDefaults();

        if (shouldFollowLeader && unit.Leader() != null)
            currentState = State.Follow;
        else
            currentState = defaultState;
    }

    public void ChangeDefaultState(State newDefaultState) => defaultState = newDefaultState;
}
