using Pathfinding;
using System;
using System.Collections.Generic;
using UnityEngine;

public class MeleeAction : BaseAction
{
    public event EventHandler OnStartAttack;
    public event EventHandler OnStopAttack;

    [Tooltip("1.4 will allow diagonal attacks")]
    [SerializeField][Range(1f, 3.5f)] float maxMeleeDistance = 1.4f;

    enum State { Aiming, SwingingSword_BeforeHit, SwingingSword_AfterHit }
    State currentState;
    float stateTimer;
    bool canAttack;
    Unit targetUnit;

    void Update()
    {
        if (isActive == false)
            return; 
        
        if (TurnSystem.Instance.IsPlayerTurn() && UnitActionSystemUI.Instance.SelectedActionValid() == false)
            return;

        stateTimer -= Time.deltaTime;
        switch (currentState)
        {
            case State.Aiming:
                float rotateSpeed = 10f;
                Vector3 lookPos = (new Vector3(targetUnit.WorldPosition().x, transform.position.y, targetUnit.WorldPosition().z) - unit.WorldPosition()).normalized;
                Quaternion rotation = Quaternion.LookRotation(lookPos);
                transform.rotation = Quaternion.Slerp(transform.rotation, rotation, rotateSpeed * Time.deltaTime);
                break;
            case State.SwingingSword_BeforeHit:
                if (canAttack)
                {
                    canAttack = false;
                    OnStartAttack?.Invoke(this, EventArgs.Empty);
                }
                break;
            case State.SwingingSword_AfterHit:
                break;
        }

        if (stateTimer <= 0f)
            NextState();
    }

    void NextState()
    {
        switch (currentState)
        {
            case State.Aiming:
                currentState = State.SwingingSword_BeforeHit;
                float beforeHitStateTime = 0.8f;
                stateTimer = beforeHitStateTime;
                break;
            case State.SwingingSword_BeforeHit:
                currentState = State.SwingingSword_AfterHit;
                float afterHitStateTime = 0.5f;
                stateTimer = afterHitStateTime;
                break;
            case State.SwingingSword_AfterHit:
                OnStopAttack?.Invoke(this, EventArgs.Empty);
                CompleteAction();
                break;
        }
    }

    public override void TakeAction(GridPosition gridPosition, Action onActionComplete)
    {
        targetUnit = LevelGrid.Instance.GetUnitAtGridPosition(gridPosition);

        currentState = State.Aiming;
        float aimingStateTime = 0.25f;
        stateTimer = aimingStateTime;
        canAttack = true;

        StartAction(onActionComplete);
    }

    public override List<GridPosition> GetValidActionGridPositionList()
    {
        List<GridPosition> validGridPositionList = new List<GridPosition>();
        GridPosition unitGridPosition = unit.GridPosition();

        ConstantPath path = ConstantPath.Construct(unit.transform.position, 100100);

        // Schedule the path for calculation
        AstarPath.StartPath(path);

        // Force the path request to complete immediately
        // This assumes the graph is small enough that this will not cause any lag
        path.BlockUntilCalculated();

        for (int i = 0; i < path.allNodes.Count; i++)
        {
            GridPosition nodeGridPosition = new GridPosition((Vector3)path.allNodes[i].position);

            if (LevelGrid.Instance.IsValidGridPosition(nodeGridPosition) == false)
                continue;

            if (LevelGrid.Instance.HasAnyUnitOnGridPosition(nodeGridPosition) == false) // Grid Position is empty, no Unit to shoot
                continue;

            Unit targetUnit = LevelGrid.Instance.GetUnitAtGridPosition(nodeGridPosition);

            // If both Units are on the same team
            if (targetUnit.IsAlly(unit.GetCurrentFaction()))
                continue;

            float distanceY = TacticsPathfindingUtilities.CalculateWorldSpaceDistanceY(unitGridPosition, nodeGridPosition);
            if (distanceY > maxMeleeDistance)
                continue;

            float distanceXZ = TacticsPathfindingUtilities.CalculateWorldSpaceDistance_XZ(unitGridPosition, nodeGridPosition);
            if (distanceXZ > maxMeleeDistance)
                continue;

            float sphereCastRadius = 0.1f;
            Vector3 attackDir = ((unitGridPosition.WorldPosition() + (Vector3.up * unit.ShoulderHeight())) - (targetUnit.WorldPosition() + (Vector3.up * targetUnit.ShoulderHeight()))).normalized;
            if (Physics.SphereCast(targetUnit.WorldPosition() + (Vector3.up * targetUnit.ShoulderHeight()), sphereCastRadius, attackDir, out RaycastHit hit, Vector3.Distance(unitGridPosition.WorldPosition() + (Vector3.up * unit.ShoulderHeight()), targetUnit.WorldPosition() + (Vector3.up * targetUnit.ShoulderHeight())), UnitActionSystem.Instance.ActionObstaclesMask()))
                continue; // Blocked by an obstacle

            validGridPositionList.Add(nodeGridPosition);
        }

        return validGridPositionList;
    }

    public override EnemyAIAction GetEnemyAIAction(GridPosition gridPosition)
    {
        int finalActionValue = 0;

        if (IsValidAction())
        {
            Unit targetUnit = LevelGrid.Instance.GetUnitAtGridPosition(gridPosition);

            // Target the Unit with the lowest health and/or the nearest target
            finalActionValue += 200 + Mathf.RoundToInt((1 - targetUnit.HealthSystem().CurrentHealthNormalized()) * 100f);
        }

        return new EnemyAIAction
        {
            gridPosition = gridPosition,
            actionValue = finalActionValue
        };
    }

    public override int GetActionPointsCost()
    {
        // TODO: Cost determined by the weapon and maybe the Unit's perks and/or attributes
        return 50;
    }

    public override bool IsValidAction()
    {
        HeldItem leftHeldItem = unit.LeftHeldItem();
        HeldItem rightHeldItem = unit.RightHeldItem();
        if ((rightHeldItem != null && rightHeldItem is MeleeWeapon) || (leftHeldItem != null && leftHeldItem is MeleeWeapon))
            return true;

        return false;
    }

    public Unit TargetUnit() => targetUnit;

    public float MaxMeleeDistance() => maxMeleeDistance;

    public override bool ActionIsUsedInstantly() => false;

    public override string GetActionName() => "Attack";
}
