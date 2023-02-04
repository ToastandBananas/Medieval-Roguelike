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

    public override void TakeAction(GridPosition gridPosition, Action onActionComplete)
    {
        if (isAttacking) return;

        if (unit.unitActionHandler.targetEnemyUnit == null || unit.unitActionHandler.targetEnemyUnit.health.IsDead())
        {
            unit.unitActionHandler.FinishAction();
            return;
        }

        StartAction(onActionComplete);

        if (IsInAttackRange(unit.unitActionHandler.targetEnemyUnit))
        {
            if (unit.unitActionHandler.GetAction<TurnAction>().IsFacingTarget(unit.unitActionHandler.targetEnemyUnit.gridPosition))
                Attack();
            else
            {
                nextAttackFree = true;
                CompleteAction();
                unit.unitActionHandler.GetAction<TurnAction>().SetTargetPosition(unit.unitActionHandler.GetAction<TurnAction>().targetDirection);
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

            if (unit.IsPlayer() && PlayerActionInput.Instance.autoAttack == false)
                unit.unitActionHandler.SetTargetEnemyUnit(null);

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

            if (unit.IsPlayer() && PlayerActionInput.Instance.autoAttack == false)
                unit.unitActionHandler.SetTargetEnemyUnit(null);

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
            if (TacticsPathfindingUtilities.CalculateWorldSpaceDistance_XYZ(unit.gridPosition, enemyUnit.gridPosition) / LevelGrid.Instance.GridSize() <= unarmedAttackRange)
                return true;
        }
        else
        {
            float maxRange;
            if (unit.rightHeldItem != null)
                maxRange = unit.GetRightMeleeWeapon().itemData.item.Weapon().maxRange;
            else
                maxRange = unit.GetLeftMeleeWeapon().itemData.item.Weapon().maxRange;

            if (TacticsPathfindingUtilities.CalculateWorldSpaceDistance_XYZ(unit.gridPosition, enemyUnit.gridPosition) / LevelGrid.Instance.GridSize() <= maxRange)
                return true;
        }
        return false;
    }

    public int UnarmedDamage()
    {
        return baseUnarmedDamage;
    }

    protected override void StartAction(Action onActionComplete)
    {
        base.StartAction(onActionComplete);
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
        float finalActionValue = 0;
        Unit targetUnit = null;

        if (IsValidAction())
        {
            targetUnit = LevelGrid.Instance.GetUnitAtGridPosition(gridPosition);

            if (targetUnit != null)
            {
                // Target the Unit with the lowest health and/or the nearest target
                finalActionValue += 500 - (targetUnit.health.CurrentHealthNormalized() * 100f);
                finalActionValue -= TacticsPathfindingUtilities.CalculateWorldSpaceDistance_XYZ(unit.gridPosition, targetUnit.gridPosition) * 10f;
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

            Weapon meleeWeapon = unit.GetPrimaryMeleeWeapon().itemData.item.Weapon();
            float distance = TacticsPathfindingUtilities.CalculateWorldSpaceDistance_XZ(startGridPosition, nodeGridPosition);
            if (distance > meleeWeapon.maxRange || distance < meleeWeapon.minRange)
                continue;

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

    public override bool IsValidAction()
    {
        if (unit.MeleeWeaponEquipped() || canFightUnarmed)
            return true;
        return false;
    }

    void MoveAction_OnStopMoving(object sender, EventArgs e) => nextAttackFree = false; 

    public bool CanFightUnarmed() => canFightUnarmed;

    public override bool ActionIsUsedInstantly() => false;

    public override string GetActionName() => "Melee Attack";
}
