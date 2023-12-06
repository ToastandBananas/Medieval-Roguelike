using UnityEngine;

namespace UnitSystem.ActionSystem
{
    public enum ActionState { Idle, Patrol, Wander, Follow, InspectSound, Fight, Flee, Hunt, FindFood }

    public class StateController : MonoBehaviour
    {
        [SerializeField] ActionState defaultState;
        public ActionState CurrentState { get; private set; }

        Unit unit;
        NPCActionHandler npcActionHandler;

        void Start()
        {
            unit = GetComponent<Unit>();
            npcActionHandler = unit.UnitActionHandler as NPCActionHandler;

            if (DefaultStateInvalid())
            {
                Debug.LogWarning(unit.name + "'s default State is <" + defaultState.ToString() + "> which is an invalid default State to have. Fix me!");
                ChangeDefaultState(ActionState.Idle);
            }

            SetToDefaultState();
        }

        public void SetCurrentState(ActionState state)
        {
            npcActionHandler.ResetToDefaults();
            CurrentState = state;
        }

        public void SetToDefaultState()
        {
            if (npcActionHandler.shouldFollowLeader && npcActionHandler.Leader() != null)
                SetCurrentState(ActionState.Follow);
            else
            {
                if (DefaultStateInvalid())
                    ChangeDefaultState(ActionState.Idle);

                SetCurrentState(defaultState);
            }
        }

        bool DefaultStateInvalid() => defaultState == ActionState.Fight || defaultState == ActionState.Flee || defaultState == ActionState.InspectSound;

        public ActionState DefaultState() => defaultState;

        public void ChangeDefaultState(ActionState newDefaultState) => defaultState = newDefaultState;
    }
}
