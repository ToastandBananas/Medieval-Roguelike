using Pathfinding;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MeleeAction : BaseAction
{
    [Header("Unarmed Combat")]
    [SerializeField] bool canFightUnarmed;
    [SerializeField] float unarmedAttackRange = 1.4f;
    [SerializeField] int baseUnarmedDamage = 5;

    public bool isAttacking { get; private set; }
    bool nextAttackFree;

    void Start()
    {
        unit.unitActionHandler.GetAction<MoveAction>().OnStopMoving += MoveAction_OnStopMoving;
    }

    public override void TakeAction(GridPosition gridPosition)
    {
        if (isAttacking) return;

        if (unit.unitActionHandler.targetEnemyUnit == null || unit.unitActionHandler.targetEnemyUnit.health.IsDead())
        {
            unit.unitActionHandler.FinishAction();
            return;
        }

        StartAction();

        if (IsInAttackRange(unit.unitActionHandler.targetEnemyUnit))
        {
            if (unit.unitActionHandler.GetAction<TurnAction>().IsFacingTarget(unit.unitActionHandler.targetEnemyUnit.gridPosition))
                Attack();
            else
            {
                nextAttackFree = true;
                CompleteAction();
                unit.unitActionHandler.QueueAction(unit.unitActionHandler.GetAction<TurnAction>());
            }
        }
        else
        {
            CompleteAction();
            unit.unitActionHandler.TakeTurn();
            return;
        }

    }

    public void Attack()
    {
        if (unit.IsPlayer() || unit.IsVisibleOnScreen())
        {
            if (unit.leftHeldItem == null && unit.rightHeldItem == null && canFightUnarmed)
            {
                unit.unitAnimator.StartMeleeAttack();
                unit.unitActionHandler.targetEnemyUnit.vision.AddVisibleUnit(unit); // The target Unit becomes aware of this Unit
                unit.unitActionHandler.targetEnemyUnit.health.TakeDamage(UnarmedDamage());
            }
            else if (unit.IsDualWielding())
            {
                // Dual wield attack
                unit.unitAnimator.StartDualMeleeAttack();
                unit.rightHeldItem.DoDefaultAttack();
                StartCoroutine(unit.leftHeldItem.DelayDoDefaultAttack());
            }
            else if (unit.rightHeldItem != null) // Right hand weapon attack
            {
                unit.unitAnimator.StartMeleeAttack();
                unit.rightHeldItem.DoDefaultAttack();
            }
            else if (unit.leftHeldItem != null) // Left hand weapon attack
            {
                unit.unitAnimator.StartMeleeAttack();
                unit.leftHeldItem.DoDefaultAttack();
            }

            StartCoroutine(WaitToFinishAction());
        }
        else
        {
            unit.unitActionHandler.targetEnemyUnit.vision.AddVisibleUnit(unit); // The target Unit becomes aware of this Unit

            if (unit.IsDualWielding()) // Dual wield attack
                unit.unitActionHandler.targetEnemyUnit.health.TakeDamage(unit.leftHeldItem.itemData.damage + unit.rightHeldItem.itemData.damage);
            else if (unit.rightHeldItem != null)
                unit.unitActionHandler.targetEnemyUnit.health.TakeDamage(unit.rightHeldItem.itemData.damage); // Right hand weapon attack
            else if (unit.leftHeldItem != null)
                unit.unitActionHandler.targetEnemyUnit.health.TakeDamage(unit.leftHeldItem.itemData.damage); // Left hand weapon attack

            CompleteAction();
            StartCoroutine(TurnManager.Instance.StartNextUnitsTurn(unit));
        }
    }

    IEnumerator WaitToFinishAction()
    {
        if (unit.IsDualWielding())
            yield return new WaitForSeconds(AnimationTimes.Instance.dualWieldAttack_Time / 2f);
        else if (unit.rightHeldItem != null)
            yield return new WaitForSeconds(AnimationTimes.Instance.GetWeaponAttackAnimationTime(unit.rightHeldItem.itemData.item as Weapon) / 2f);
        else if (unit.leftHeldItem != null)
            yield return new WaitForSeconds(AnimationTimes.Instance.GetWeaponAttackAnimationTime(unit.leftHeldItem.itemData.item as Weapon) / 2f);
        else
            yield return new WaitForSeconds(0.25f);

        CompleteAction();
        StartCoroutine(TurnManager.Instance.StartNextUnitsTurn(unit));
    }

    public bool IsInAttackRange(Unit enemyUnit)
    {
        if (unit.IsUnarmed())
        {
            if (TacticsPathfindingUtilities.CalculateWorldSpaceDistance_XZ(unit.gridPosition, enemyUnit.gridPosition) / LevelGrid.Instance.GridSize() <= UnarmedAttackRange(enemyUnit.gridPosition, true))
                return true;
        }
        else
        {
            float maxRange;
            if (unit.rightHeldItem != null)
                maxRange = unit.GetRightMeleeWeapon().MaxRange(unit.gridPosition, enemyUnit.gridPosition);
            else
                maxRange = unit.GetLeftMeleeWeapon().MaxRange(unit.gridPosition, enemyUnit.gridPosition);

            if (TacticsPathfindingUtilities.CalculateWorldSpaceDistance_XZ(unit.gridPosition, enemyUnit.gridPosition) / LevelGrid.Instance.GridSize() <= maxRange)
                return true;
        }
        return false;
    }

    public int UnarmedDamage()
    {
        return baseUnarmedDamage;
    }

    protected override void StartAction()
    {
        base.StartAction();
        isAttacking = true;
    }

    public override void CompleteAction()
    {
        base.CompleteAction();
        isAttacking = false;
        unit.unitActionHandler.FinishAction();
    }

    public override EnemyAIAction GetEnemyAIAction(GridPosition gridPosition)
    {
        float finalActionValue = 0f;
        Unit targetUnit = null;

        if (IsValidAction())
        {
            targetUnit = LevelGrid.Instance.GetUnitAtGridPosition(gridPosition);

            if (targetUnit != null)
            {
                // Target the Unit with the lowest health and/or the nearest target
                finalActionValue += 500 - (targetUnit.health.CurrentHealthNormalized() * 100f);
                float distance = TacticsPathfindingUtilities.CalculateWorldSpaceDistance_XYZ(unit.gridPosition, targetUnit.gridPosition);
                float minAttackRange = 1f;
                if (unit.MeleeWeaponEquipped())
                    minAttackRange = unit.GetPrimaryMeleeWeapon().itemData.item.Weapon().minRange;

                if (distance < minAttackRange)
                    finalActionValue = 0f;
                else
                    finalActionValue -= distance * 10f;
            }
        }

        return new EnemyAIAction
        {
            unit = targetUnit,
            gridPosition = gridPosition,
            actionValue = Mathf.RoundToInt(finalActionValue)
        };
    }

    public override int GetActionPointsCost(GridPosition targetGridPosition)
    {
        if (nextAttackFree)
        {
            nextAttackFree = false;
            return 0;
        }
        return 300;
    }

    public override List<GridPosition> GetValidActionGridPositionList(GridPosition startGridPosition)
    {
        List<GridPosition> validGridPositionList = new List<GridPosition>();

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

            float distance = TacticsPathfindingUtilities.CalculateWorldSpaceDistance_XZ(startGridPosition, nodeGridPosition);
            if (unit.MeleeWeaponEquipped())
            {
                Weapon meleeWeapon = unit.GetPrimaryMeleeWeapon().itemData.item.Weapon();
                float maxRangeToNodePosition = meleeWeapon.maxRange - Mathf.Abs(nodeGridPosition.y - startGridPosition.y); ;
                if (maxRangeToNodePosition < 0f) maxRangeToNodePosition = 0f;

                if (distance > maxRangeToNodePosition || distance < meleeWeapon.minRange)
                    continue;
            }
            else
            {
                float maxRangeToNodePosition = unarmedAttackRange - Mathf.Abs(nodeGridPosition.y - startGridPosition.y); ;
                if (maxRangeToNodePosition < 0f) maxRangeToNodePosition = 0f;

                if (distance > maxRangeToNodePosition || distance < 1f)
                    continue;
            }

            Unit targetUnit = LevelGrid.Instance.GetUnitAtGridPosition(nodeGridPosition);

            // If both Units are on the same team
            if (targetUnit.alliance.IsAlly(unit.alliance.CurrentFaction()))
                continue;

            float sphereCastRadius = 0.1f;
            Vector3 shootDir = ((startGridPosition.WorldPosition() + (Vector3.up * unit.ShoulderHeight())) - (targetUnit.WorldPosition() + (Vector3.up * targetUnit.ShoulderHeight()))).normalized;
            if (Physics.SphereCast(targetUnit.WorldPosition() + (Vector3.up * targetUnit.ShoulderHeight()), sphereCastRadius, shootDir, out RaycastHit hit, Vector3.Distance(startGridPosition.WorldPosition() + (Vector3.up * unit.ShoulderHeight()), targetUnit.WorldPosition() + (Vector3.up * targetUnit.ShoulderHeight())), unit.unitActionHandler.AttackObstacleMask()))
                continue; // Blocked by an obstacle

            // Debug.Log(gridPosition);
            validGridPositionList.Add(nodeGridPosition);
        }

        return validGridPositionList;
    }

    public List<GridPosition> GetValidGridPositionsInRange(GridPosition targetGridPosition)
    {
        List<GridPosition> validGridPositionList = new List<GridPosition>();
        Unit targetUnit = LevelGrid.Instance.GetUnitAtGridPosition(targetGridPosition);

        if (targetUnit == null)
            return validGridPositionList;

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

            if (LevelGrid.Instance.HasAnyUnitOnGridPosition(nodeGridPosition)) // Grid Position has a Unit there already
                continue;

            float distance = TacticsPathfindingUtilities.CalculateWorldSpaceDistance_XZ(nodeGridPosition, targetGridPosition);
            float maxRangeToNodePosition = 0f;
            if (unit.MeleeWeaponEquipped())
            {
                Weapon meleeWeapon = unit.GetPrimaryMeleeWeapon().itemData.item.Weapon();
                maxRangeToNodePosition = meleeWeapon.maxRange - Mathf.Abs(targetGridPosition.y - nodeGridPosition.y);
                if (maxRangeToNodePosition < 0f) maxRangeToNodePosition = 0f; 
                
                if (distance > maxRangeToNodePosition || distance < meleeWeapon.minRange)
                    continue;
            }
            else
            {
                maxRangeToNodePosition = unarmedAttackRange - Mathf.Abs(targetGridPosition.y - nodeGridPosition.y);
                if (maxRangeToNodePosition < 0f) maxRangeToNodePosition = 0f;

                if (distance > maxRangeToNodePosition || distance < 1f)
                    continue;
            }

            float sphereCastRadius = 0.1f;
            Vector3 attackDir = ((nodeGridPosition.WorldPosition() + (Vector3.up * unit.ShoulderHeight() * 2f)) - (targetUnit.WorldPosition() + (Vector3.up * targetUnit.ShoulderHeight() * 2f))).normalized;
            if (Physics.SphereCast(targetUnit.WorldPosition() + (Vector3.up * targetUnit.ShoulderHeight() * 2f), sphereCastRadius, attackDir, out RaycastHit hit, Vector3.Distance(nodeGridPosition.WorldPosition() + (Vector3.up * unit.ShoulderHeight() * 2f), targetUnit.WorldPosition() + (Vector3.up * targetUnit.ShoulderHeight() * 2f)), unit.unitActionHandler.AttackObstacleMask()))
                continue; // Blocked by an obstacle

            // Debug.Log(gridPosition);
            validGridPositionList.Add(nodeGridPosition);
        }

        return validGridPositionList;
    }

    public GridPosition GetNearestMeleePosition(GridPosition startGridPosition, GridPosition targetGridPosition)
    {
        List<GridPosition> validGridPositionsList = GetValidGridPositionsInRange(targetGridPosition);
        List<GridPosition> nearestGridPositionsList = new List<GridPosition>();
        float nearestDistance = 10000000f;

        // First find the nearest valid Grid Positions to the Player
        for (int i = 0; i < validGridPositionsList.Count; i++)
        {
            float distance = Vector3.Distance(validGridPositionsList[i].WorldPosition(), startGridPosition.WorldPosition());
            if (distance < nearestDistance)
            {
                nearestGridPositionsList.Clear();
                nearestGridPositionsList.Add(validGridPositionsList[i]);
                nearestDistance = distance;
            }
            else if (Mathf.Approximately(distance, nearestDistance))
                nearestGridPositionsList.Add(validGridPositionsList[i]);
        }

        GridPosition nearestGridPosition = startGridPosition;
        float nearestDistanceToTarget = 10000000f;
        for (int i = 0; i < nearestGridPositionsList.Count; i++)
        {
            // Get the Grid Position that is closest to the target Grid Position
            float distance = Vector3.Distance(nearestGridPositionsList[i].WorldPosition(), targetGridPosition.WorldPosition());
            if (distance < nearestDistanceToTarget)
            {
                nearestDistanceToTarget = distance;
                nearestGridPosition = nearestGridPositionsList[i];
            }
        }

        return nearestGridPosition;
    }

    public override bool IsValidAction()
    {
        if (unit.MeleeWeaponEquipped() || (canFightUnarmed && unit.RangedWeaponEquipped() == false))
            return true;
        return false;
    }

    void MoveAction_OnStopMoving(object sender, EventArgs e) => nextAttackFree = false; 

    public bool CanFightUnarmed() => canFightUnarmed;

    public float UnarmedAttackRange(GridPosition enemyGridPosition, bool accountForHeight)
    {
        if (accountForHeight == false)
            return unarmedAttackRange;
        float maxRange = unarmedAttackRange - Mathf.Abs(enemyGridPosition.y - unit.gridPosition.y);
        if (maxRange < 0f) maxRange = 0f;
        return maxRange;
    }

    public override bool ActionIsUsedInstantly() => false;

    public override string GetActionName() => "Melee Attack";
}
