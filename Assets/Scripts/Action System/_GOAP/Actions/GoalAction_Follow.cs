using System;
using System.Collections.Generic;
using UnitSystem.ActionSystem.GOAP.Goals;
using UnityEngine;

namespace UnitSystem.ActionSystem.GOAP.GoalActions
{
    public class GoalAction_Follow : GoalAction_Base
    {
        [SerializeField] float stopFollowDistance = 3f;
        [SerializeField] Unit leader;
        [SerializeField] bool shouldFollowLeader;
        public bool ShouldFollowLeader => shouldFollowLeader;

        readonly List<Type> supportedGoals = new(new Type[] { typeof(Goal_Follow) });

        public override List<Type> SupportedGoals() => supportedGoals;

        public override float Cost() => 0f;

        public override void OnActivated(Goal_Base linkedGoal)
        {
            base.OnActivated(linkedGoal);
            Follow();
        }

        #region Follow
        void Follow()
        {
            if (leader == null || leader.Health.IsDead)
            {
                Debug.LogWarning("Leader for " + unit.name + " is null or dead, but they are in the Follow state.");
                shouldFollowLeader = false;
                unit.StateController.SetToDefaultState();
                npcActionHandler.DetermineAction();
                return;
            }

            if (Vector3.Distance(unit.WorldPosition, leader.WorldPosition) <= stopFollowDistance)
                TurnManager.Instance.FinishTurn(unit);
            else if (npcActionHandler.MoveAction.IsMoving == false)
                npcActionHandler.MoveAction.QueueAction(leader.UnitActionHandler.TurnAction.GetGridPositionBehindUnit());
        }

        public Unit Leader => leader;

        public void SetLeader(Unit newLeader) => leader = newLeader;

        public void SetShouldFollowLeader(bool shouldFollowLeader) => this.shouldFollowLeader = shouldFollowLeader;
        #endregion
    }
}
