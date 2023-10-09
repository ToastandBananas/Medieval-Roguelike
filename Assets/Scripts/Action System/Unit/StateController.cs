using UnityEngine;
using UnitSystem;

namespace ActionSystem
{
    public enum State { Idle, Patrol, Wander, Follow, InspectSound, Fight, Flee, Hunt, FindFood }

    public class StateController : MonoBehaviour
    {
        [SerializeField] State defaultState;
        public State currentState { get; private set; }

        Unit unit;
        NPCActionHandler npcActionHandler;

        void Start()
        {
            unit = GetComponent<Unit>();
            npcActionHandler = unit.unitActionHandler as NPCActionHandler;

            if (DefaultStateInvalid())
            {
                Debug.LogWarning(unit.name + "'s default State is <" + defaultState.ToString() + "> which is an invalid default State to have. Fix me!");
                ChangeDefaultState(State.Idle);
            }

            SetToDefaultState();
        }

        public void SetCurrentState(State state)
        {
            npcActionHandler.ResetToDefaults();
            currentState = state;
        }

        public void SetToDefaultState()
        {
            if (npcActionHandler.shouldFollowLeader && npcActionHandler.Leader() != null)
                currentState = State.Follow;
            else
            {
                if (DefaultStateInvalid())
                    ChangeDefaultState(State.Idle);

                SetCurrentState(defaultState);
            }
        }

        bool DefaultStateInvalid() => defaultState == State.Fight || defaultState == State.Flee || defaultState == State.InspectSound;

        public State DefaultState() => defaultState;

        public void ChangeDefaultState(State newDefaultState) => defaultState = newDefaultState;
    }
}
