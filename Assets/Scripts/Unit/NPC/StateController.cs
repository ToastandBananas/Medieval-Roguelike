using UnityEngine;

public enum State { Idle, Patrol, Wander, Follow, MoveToTarget, Fight, Flee, Hunt, FindFood }

public class StateController : MonoBehaviour
{
    [SerializeField] State defaultState = State.Idle;
    State currentState = State.Idle;

    Unit unit;
    NPCActionHandler npcActionHandler;

    void Start()
    {
        unit = GetComponent<Unit>();
        npcActionHandler = unit.unitActionHandler as NPCActionHandler;

        if (defaultState == State.Fight || defaultState == State.Flee || defaultState == State.MoveToTarget)
        {
            Debug.LogWarning(unit.name + "'s default State is <" + defaultState.ToString() + "> which is an invalid default State to have. Fix me!");
            ChangeDefaultState(State.Idle);
        }

        SetToDefaultState();
    }

    public State CurrentState() => currentState;

    public void SetCurrentState(State state)
    {
        npcActionHandler.ResetToDefaults();
        currentState = state;
    }

    public State DefaultState() => defaultState;

    public void SetToDefaultState()
    {
        npcActionHandler.ResetToDefaults();

        if (npcActionHandler.shouldFollowLeader && npcActionHandler.Leader() != null)
            currentState = State.Follow;
        else
            currentState = defaultState;
    }

    public void ChangeDefaultState(State newDefaultState) => defaultState = newDefaultState;
}
