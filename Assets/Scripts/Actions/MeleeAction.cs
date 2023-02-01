using System;
using System.Collections;
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
                unit.unitActionHandler.QueueAction(unit.unitActionHandler.GetAction<TurnAction>(), unit.unitActionHandler.targetEnemyUnit.gridPosition);
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
        float attackRange = 1.4f;
        if (unit.IsUnarmed())
        {
            if (TacticsPathfindingUtilities.CalculateWorldSpaceDistance_XYZ(unit.gridPosition, enemyUnit.gridPosition) / LevelGrid.Instance.GridSize() <= unarmedAttackRange)
                return true;
        }
        else
        {
            if (TacticsPathfindingUtilities.CalculateWorldSpaceDistance_XYZ(unit.gridPosition, enemyUnit.gridPosition) / LevelGrid.Instance.GridSize() <= attackRange)
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
