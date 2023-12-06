using UnityEngine;

namespace UnitSystem.ActionSystem.GOAP.Goals
{
    public class Goal_Idle : Goal_Base
    {
        [SerializeField] int priority = 10;

        public override int CalculatePriority() => priority;

        public override bool CanRun() => true;
    }
}
