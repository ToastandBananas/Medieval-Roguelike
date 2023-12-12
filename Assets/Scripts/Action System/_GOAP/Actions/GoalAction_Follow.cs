using System;
using UnitSystem.ActionSystem.Actions;
using UnityEngine;

namespace UnitSystem.ActionSystem.GOAP.GoalActions
{
    public class GoalAction_Follow : GoalAction_Base
    {
        [SerializeField] float stopFollowDistance = 3f;
        [SerializeField] Unit leader;
        [SerializeField] bool shouldFollowLeader;
        public bool ShouldFollowLeader => shouldFollowLeader;

        public override MoveMode PreferredMoveMode() => leader != null ? leader.UnitActionHandler.MoveAction.CurrentMoveMode : MoveMode.Walk;

        public override void OnTick()
        {
            npcActionHandler.MoveAction.SetMoveMode(PreferredMoveMode());
            Follow();
        }

        #region Follow
        void Follow()
        {
            if (leader == null || leader.Health.IsDead)
            {
                // Debug.LogWarning("Leader for " + unit.name + " is null or dead, but they are in the Follow state.");
                shouldFollowLeader = false;
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
