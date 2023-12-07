using UnityEngine;

namespace UnitSystem.ActionSystem
{
    public enum GoalState { Idle, Patrol, Wander, Follow, InspectSound, Fight, Flee, Hunt, FindFood }

    public class StateController : MonoBehaviour
    {
        [SerializeField] GoalState defaultState;
        public GoalState CurrentState { get; private set; }

        Unit unit;
        //NPCActionHandler npcActionHandler;

        void Start()
        {
            unit = GetComponent<Unit>();
            //npcActionHandler = unit.UnitActionHandler as NPCActionHandler;

            if (DefaultStateInvalid)
            {
                Debug.LogWarning(unit.name + "'s default State is <" + defaultState.ToString() + "> which is an invalid default State to have. Fix me!");
                ChangeDefaultState(GoalState.Idle);
            }

            SetToDefaultState();
        }

        public void SetCurrentState(GoalState state) => CurrentState = state;

        public void SetToDefaultState()
        {
            //if (npcActionHandler.ShouldFollowLeader && npcActionHandler.Leader != null)
                //SetCurrentState(GoalState.Follow);
            //else
            //{
                if (DefaultStateInvalid)
                    ChangeDefaultState(GoalState.Idle);

                SetCurrentState(defaultState);
            //}
        }

        bool DefaultStateInvalid => defaultState == GoalState.Fight || defaultState == GoalState.Flee || defaultState == GoalState.InspectSound;

        public GoalState DefaultState => defaultState;

        public void ChangeDefaultState(GoalState newDefaultState) => defaultState = newDefaultState;
    }
}
