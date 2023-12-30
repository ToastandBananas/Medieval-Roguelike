using System.Collections.Generic;
using UnityEngine;
using UnitSystem.ActionSystem.UI;
using GridSystem;

namespace UnitSystem.ActionSystem.Actions
{
    public abstract class Action_Base : MonoBehaviour
    {
        public ActionType ActionType { get; private set; }
        public ActionBarSlot ActionBarSlot { get; private set; }
        public Unit Unit { get; private set; }
        public GridPosition TargetGridPosition { get; protected set; }

        public abstract void TakeAction();

        protected virtual void StartAction() => Unit.Stats.UseEnergy(EnergyCost());

        public virtual void SetTargetGridPosition(GridPosition gridPosition) => TargetGridPosition = gridPosition;

        /// <summary>Only use this version of QueueAction if the target grid position is irrelevant or if it/other necessary variables have already been set.</summary>
        public virtual void QueueAction()
        {
            // Debug.Log(name);
            Unit.UnitActionHandler.QueueAction(this);
        }

        public virtual void QueueAction(GridPosition targetGridPosition)
        {
            TargetGridPosition = targetGridPosition;
            QueueAction();
        }

        public virtual void CancelAction() { }

        public virtual void CompleteAction()
        {
            if (Unit.IsPlayer)
            {
                ActionSystemUI.UpdateActionVisuals();
                if (ActionBarSlot != null)
                    ActionBarSlot.UpdateIcon();
            }
        }

        public NPCAIAction GetBestNPCAIActionFromList(List<NPCAIAction> npcAIActionList)
        {
            npcAIActionList.Sort((NPCAIAction a, NPCAIAction b) => b.actionValue - a.actionValue);
            return npcAIActionList[0];
        }

        public virtual bool IsValidUnitInActionArea(GridPosition targetGridPosition)
        {
            Debug.LogWarning("The 'IsValidUnitInActionArea' method has not been implemented for the " + GetType().Name + " action.");
            return false;
        }

        public virtual List<GridPosition> GetActionAreaGridPositions(GridPosition targetGridPosition)
        {
            Debug.LogWarning("The 'GetPossibleAttackGridPositions' method has not been implemented for the " + GetType().Name + " action.");
            return null;
        }

        public virtual List<GridPosition> GetActionGridPositionsInRange(GridPosition startGridPosition)
        {
            Debug.LogWarning("The 'GetActionGridPositionsInRange' method has not been implemented for the " + GetType().Name + " action.");
            return null;
        }

        /// <summary>Determines the value of performing this BaseAction at the 'actionGridPosition'. For use when an NPC needs to determine which combat action to take.</summary>
        /// <param name="actionGridPosition">The target action position we are testing.</param>
        /// <returns>NPCAIAction with an associated actionValue. A higher actionValue is better.</returns>
        public virtual NPCAIAction GetNPCAIAction_ActionGridPosition(GridPosition actionGridPosition)
        {
            Debug.LogWarning("The 'GetNPCAIAction_ActionGridPosition' method has not been implemented for the " + GetType().Name + " action.");
            return null;
        }

        /// <summary>Determines the value of attacking the targetUnit. For use when an NPC needs to determine which enemy unit is best to set as this unit's targetEnemyUnit (in UnitActionHandler).</summary>
        /// <param name="targetUnit">The Unit we are testing.</param>
        /// <returns>NPCAIAction with an associated actionValue. A higher actionValue is better.</returns>
        public virtual NPCAIAction GetNPCAIAction_Unit(Unit targetUnit)
        {
            Debug.LogWarning("The 'GetNPCAIAction_Unit' method has not been implemented for the " + GetType().Name + " action.");
            return null;
        }

        public virtual void OnActionSelected() 
        {
            if (ActionIsUsedInstantly())
                QueueAction();
            else
                GridSystemVisual.UpdateAttackRangeGridVisual();
        }

        public void SetActionBarSlot(ActionBarSlot slot) => ActionBarSlot = slot;

        protected virtual void Initialize() { }

        public virtual void OnReturnToPool() => CancelAction();

        public abstract bool CanQueueMultiple();

        public bool IsDefaultAttackAction => this is Action_Melee || this is Action_Shoot;

        public Action_BaseAttack BaseAttackAction => this as Action_BaseAttack;

        public void Setup(Unit unit, ActionType actionType)
        {
            this.Unit = unit;
            this.ActionType = actionType;
            Initialize();
        }

        public virtual Sprite ActionIcon() => ActionType.ActionIcon;

        public virtual string ActionName() => ActionType.ActionName;

        public abstract string TooltipDescription();

        public abstract ActionBarSection ActionBarSection();

        public abstract bool IsValidAction();

        public abstract bool CanBeClearedFromActionQueue();

        public abstract bool IsInterruptable();

        public abstract bool ActionIsUsedInstantly();

        public abstract int ActionPointsCost();

        public abstract int EnergyCost();

        public virtual float EnergyCostPerTurn() => 0f;
    }
}