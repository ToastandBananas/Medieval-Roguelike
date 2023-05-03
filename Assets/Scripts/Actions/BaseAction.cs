using System.Collections.Generic;
using UnityEngine;

public abstract class BaseAction : MonoBehaviour
{
    public Unit unit { get; private set; }

    protected bool isActive;

    public virtual void Awake()
    {
        unit = GetComponent<Unit>();
    }

    public abstract void TakeAction(GridPosition gridPosition); 
    
    protected virtual void StartAction()
    {
        isActive = true;
    }

    public virtual void CompleteAction()
    {
        isActive = false;
        if (unit.IsPlayer())
            UnitActionSystemUI.Instance.UpdateActionVisuals();
    }

    public NPCAIAction GetBestNPCAIActionFromList(List<NPCAIAction> npcAIActionList)
    {
        npcAIActionList.Sort((NPCAIAction a, NPCAIAction b) => b.actionValue - a.actionValue);
        return npcAIActionList[0];
    }

    public void BecomeVisibleEnemyOfTarget(Unit targetUnit)
    {
        // The target Unit becomes an enemy of this Unit's faction if they weren't already
        if (unit.alliance.IsEnemy(targetUnit) == false)
        {
            targetUnit.vision.RemoveVisibleUnit(unit);
            unit.vision.RemoveVisibleUnit(targetUnit);

            targetUnit.alliance.AddEnemy(unit);
            unit.vision.AddVisibleUnit(targetUnit);
        }

        targetUnit.vision.AddVisibleUnit(unit); // The target Unit becomes aware of this Unit if they weren't already
    }

    public void BecomeVisibleAllyOfTarget(Unit targetUnit)
    {
        // The target Unit becomes an enemy of this Unit's faction if they weren't already
        if (unit.alliance.IsAlly(targetUnit) == false)
        {
            targetUnit.vision.RemoveVisibleUnit(unit);
            unit.vision.RemoveVisibleUnit(targetUnit);

            targetUnit.alliance.AddAlly(unit);
            unit.vision.AddVisibleUnit(targetUnit);
        }

        targetUnit.vision.AddVisibleUnit(unit); // The target Unit becomes aware of this Unit if they weren't already
    }

    public virtual void DamageTargets(HeldItem heldWeapon)
    {
        if (IsAttackAction() == false)
            Debug.LogWarning(GetActionName() + " is not an attack action, but it is trying to use the 'DamageTarget' method.");
        else
            Debug.LogWarning("The 'DamageTarget' method has not been implemented for the " + GetActionName() + " action.");
    }

    public virtual bool IsInAttackRange(Unit targetUnit, GridPosition startGridPosition, GridPosition targetGridPosition)
    {
        if (IsAttackAction() == false)
            Debug.LogWarning(GetActionName() + " is not an attack action, but it is trying to use the 'IsInAttackRange' method.");
        else
            Debug.LogWarning("The 'IsInAttackRange' method has not been implemented for the " + GetActionName() + " action.");
        return false;
    }

    public virtual bool IsInAttackRange(Unit targetUnit)
    {
        if (IsAttackAction() == false)
            Debug.LogWarning(GetActionName() + " is not an attack action, but it is trying to use the 'IsInAttackRange' method.");
        else
            Debug.LogWarning("The 'IsInAttackRange' method has not been implemented for the " + GetActionName() + " action.");
        return false;
    }

    public virtual GridPosition GetNearestAttackPosition(GridPosition startGridPosition, Unit targetUnit)
    {
        if (IsAttackAction() == false)
            Debug.LogWarning(GetActionName() + " is not an attack action, but it is trying to use the 'GetNearestAttackPosition' method.");
        else
            Debug.LogWarning("The 'GetNearestAttackPosition' method has not been implemented for the " + GetActionName() + " action.");
        return unit.gridPosition;
    }

    public virtual bool IsValidUnitInActionArea(GridPosition targetGridPosition)
    {
        Debug.LogWarning("The 'IsUnitInActionArea' method has not been implemented for the " + GetActionName() + " action.");
        return false;
    }

    public virtual List<GridPosition> GetActionAreaGridPositions(GridPosition targetGridPosition)
    {
        Debug.LogWarning("The 'GetPossibleAttackGridPositions' method has not been implemented for the " + GetActionName() + " action.");
        return null;
    }

    public virtual List<GridPosition> GetActionGridPositionsInRange(GridPosition startGridPosition)
    {
        Debug.LogWarning("The 'GetActionGridPositionsInRange' method has not been implemented for the " + GetActionName() + " action.");
        return null;
    }

    /// <summary>Determines the value of performing this BaseAction at the 'actionGridPosition'. For use when an NPC needs to determine which combat action to take.</summary>
    /// <param name="actionGridPosition">The target action position we are testing.</param>
    /// <returns>NPCAIAction with an associated actionValue. A higher actionValue is better.</returns>
    public virtual NPCAIAction GetNPCAIAction_ActionGridPosition(GridPosition actionGridPosition)
    {
        Debug.LogWarning("The 'GetNPCAIAction_ActionGridPosition' method has not been implemented for the " + GetActionName() + " action.");
        return null;
    }

    /// <summary>Determines the value of attacking the targetUnit. For use when an NPC needs to determine which enemy unit is best to set as this unit's targetEnemyUnit (in UnitActionHandler).</summary>
    /// <param name="targetUnit">The Unit we are testing.</param>
    /// <returns>NPCAIAction with an associated actionValue. A higher actionValue is better.</returns>
    public virtual NPCAIAction GetNPCAIAction_Unit(Unit targetUnit)
    {
        Debug.LogWarning("The 'GetNPCAIAction_Unit' method has not been implemented for the " + GetActionName() + " action.");
        return null;
    }

    public bool IsActive() => isActive;

    public bool IsDefaultAttackAction() => this is MeleeAction || this is ShootAction;

    public abstract bool IsValidAction();

    public abstract bool IsAttackAction();

    public abstract bool IsMeleeAttackAction();

    public abstract bool IsRangedAttackAction();

    public abstract bool ActionIsUsedInstantly();

    public abstract int GetActionPointsCost();

    public abstract string GetActionName();
}
