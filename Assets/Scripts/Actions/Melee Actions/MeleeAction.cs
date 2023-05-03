using Pathfinding;
using Pathfinding.Util;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static GridSystemVisual;

public class MeleeAction : BaseAction
{
    [Header("Unarmed Combat")]
    [SerializeField] bool canFightUnarmed;
    [SerializeField] float unarmedAttackRange = 1.4f;
    [SerializeField] int baseUnarmedDamage = 5;

    List<GridPosition> validGridPositionsList = new List<GridPosition>();
    List<GridPosition> nearestGridPositionsList = new List<GridPosition>();

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
            unit.unitActionHandler.SetTargetEnemyUnit(null);
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
                unit.unitActionHandler.QueueAction(unit.unitActionHandler.GetAction<TurnAction>());
                CompleteAction();
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
        Unit targetUnit = unit.unitActionHandler.targetEnemyUnit;

        // If this is the Player attacking, or if this is an NPC that's visible on screen
        if (unit.IsPlayer() || unit.IsVisibleOnScreen())
        {
            // Play the attack animations and handle blocking for the target
            if (unit.IsUnarmed())
            {
                if (canFightUnarmed)
                    unit.unitAnimator.DoDefaultUnarmedAttack();
            }
            else if (unit.IsDualWielding())
            {
                // Dual wield attack
                unit.unitAnimator.StartDualMeleeAttack();
                unit.rightHeldItem.DoDefaultAttack();
                StartCoroutine(unit.leftHeldItem.DelayDoDefaultAttack());
            }
            else if (unit.rightHeldItem != null) 
            {
                // Right hand weapon attack
                unit.unitAnimator.StartMeleeAttack(); 
                unit.rightHeldItem.DoDefaultAttack();
            }
            else if (unit.leftHeldItem != null) 
            {
                // Left hand weapon attack
                unit.unitAnimator.StartMeleeAttack();
                unit.leftHeldItem.DoDefaultAttack();
            }

            // Wait until the attack lands before completing the action
            StartCoroutine(WaitToCompleteAction());
        }
        else // If this is an NPC who's outside of the screen, instantly damage the target without an animation
        {
            bool attackBlocked = false;
            if (unit.IsUnarmed())
            {
                attackBlocked = targetUnit.TryBlockMeleeAttack(unit);
                DamageTargets(null);
            }
            else if (unit.IsDualWielding()) // Dual wield attack
            {
                bool mainAttackBlocked = targetUnit.TryBlockMeleeAttack(unit);
                DamageTargets(unit.rightHeldItem as HeldMeleeWeapon);

                bool offhandAttackBlocked = targetUnit.TryBlockMeleeAttack(unit);
                DamageTargets(unit.leftHeldItem as HeldMeleeWeapon);

                if (mainAttackBlocked || offhandAttackBlocked)
                    attackBlocked = true;
            }
            else if (unit.rightHeldItem != null)
            {
                attackBlocked = targetUnit.TryBlockMeleeAttack(unit);
                DamageTargets(unit.rightHeldItem as HeldMeleeWeapon); // Right hand weapon attack
            }
            else if (unit.leftHeldItem != null)
            {
                attackBlocked = targetUnit.TryBlockMeleeAttack(unit);
                DamageTargets(unit.leftHeldItem as HeldMeleeWeapon); // Left hand weapon attack
            }

            if (attackBlocked)
                StartCoroutine(targetUnit.unitActionHandler.GetAction<TurnAction>().RotateTowards_AttackingTargetUnit(unit, true));

            CompleteAction();
            TurnManager.Instance.StartNextUnitsTurn(unit);
        }
    }

    public override void DamageTargets(HeldItem heldWeapon)
    {
        HeldMeleeWeapon heldMeleeWeapon = heldWeapon as HeldMeleeWeapon;
        foreach (KeyValuePair<Unit, HeldItem> target in unit.unitActionHandler.targetUnits)
        {
            Unit targetUnit = target.Key;
            HeldItem itemBlockedWith = target.Value;

            if (targetUnit != null)
            {
                // The unit being attacked becomes aware of this unit
                BecomeVisibleEnemyOfTarget(targetUnit);

                int armorAbsorbAmount = 0;
                int damageAmount;
                if (heldMeleeWeapon == null) // If unarmed
                    damageAmount = UnarmedDamage();
                else
                    damageAmount = heldMeleeWeapon.DamageAmount();

                // If the attack was blocked
                if (itemBlockedWith != null)
                {
                    int blockAmount = 0;
                    if (targetUnit.ShieldEquipped() && itemBlockedWith == targetUnit.GetShield()) // If blocked with shield
                        blockAmount = targetUnit.stats.ShieldBlockPower(targetUnit.GetShield());
                    else if (targetUnit.MeleeWeaponEquipped()) // If blocked with melee weapon
                    {
                        if (itemBlockedWith == targetUnit.GetPrimaryMeleeWeapon()) // If blocked with primary weapon (only weapon, or dual wield right hand weapon)
                        {
                            blockAmount = targetUnit.stats.WeaponBlockPower(targetUnit.GetPrimaryMeleeWeapon());
                            if (unit.IsDualWielding())
                            {
                                if (this == unit.GetRightMeleeWeapon())
                                    blockAmount = Mathf.RoundToInt(blockAmount * GameManager.dualWieldPrimaryEfficiency);
                            }
                        }
                        else // If blocked with dual wield left hand weapon
                            blockAmount = Mathf.RoundToInt(targetUnit.stats.WeaponBlockPower(targetUnit.GetLeftMeleeWeapon()) * GameManager.dualWieldSecondaryEfficiency);
                    }

                    targetUnit.health.TakeDamage(damageAmount - blockAmount - armorAbsorbAmount);

                    if (itemBlockedWith is HeldShield)
                        targetUnit.GetShield().LowerShield();
                    else
                    {
                        HeldMeleeWeapon blockingWeapon = itemBlockedWith as HeldMeleeWeapon;
                        blockingWeapon.LowerWeapon();
                    }
                }
                else
                    targetUnit.health.TakeDamage(damageAmount - armorAbsorbAmount);
            }
        }

        unit.unitActionHandler.targetUnits.Clear();

        if (unit.IsPlayer() && PlayerInput.Instance.autoAttack == false)
            unit.unitActionHandler.SetTargetEnemyUnit(null);
    }

    public override bool IsInAttackRange(Unit targetUnit, GridPosition startGridPosition, GridPosition targetGridPosition)
    {
        if (targetUnit != null && unit.vision.IsInLineOfSight(targetUnit) == false)
            return false;

        float distance = TacticsPathfindingUtilities.CalculateWorldSpaceDistance_XZ(startGridPosition, targetGridPosition);
        if (unit.MeleeWeaponEquipped())
        {
            Weapon meleeWeapon = unit.GetPrimaryMeleeWeapon().itemData.item.Weapon();
            float maxRangeToTargetPosition = meleeWeapon.maxRange - Mathf.Abs(targetGridPosition.y - startGridPosition.y);
            if (maxRangeToTargetPosition < 0f) maxRangeToTargetPosition = 0f;

            if (distance > maxRangeToTargetPosition || distance < meleeWeapon.minRange)
                return false;
        }
        else
        {
            float maxRangeToTargetPosition = unarmedAttackRange - Mathf.Abs(targetGridPosition.y - startGridPosition.y);
            if (maxRangeToTargetPosition < 0f) maxRangeToTargetPosition = 0f;

            if (distance > maxRangeToTargetPosition || distance < 1f)
                return false;
        }

        return true;
    }

    public override bool IsInAttackRange(Unit targetUnit) => IsInAttackRange(targetUnit, unit.gridPosition, targetUnit.gridPosition);

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

    IEnumerator WaitToCompleteAction()
    {
        if (unit.IsUnarmed())
            yield return new WaitForSeconds(AnimationTimes.Instance.UnarmedAttackTime());
        else if (unit.IsDualWielding())
            yield return new WaitForSeconds(AnimationTimes.Instance.DualWieldAttackTime());
        else
            yield return new WaitForSeconds(AnimationTimes.Instance.DefaultWeaponAttackTime(unit.GetPrimaryMeleeWeapon().itemData.item as Weapon));

        CompleteAction();
        TurnManager.Instance.StartNextUnitsTurn(unit);
    }

    public override NPCAIAction GetNPCAIAction_Unit(Unit targetUnit)
    {
        float finalActionValue = 0f;
        if (IsValidAction() && targetUnit != null && targetUnit.health.IsDead() == false)
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

            return new NPCAIAction
            {
                baseAction = this,
                actionGridPosition = targetUnit.gridPosition,
                actionValue = Mathf.RoundToInt(finalActionValue)
            };
        }

        return new NPCAIAction
        {
            baseAction = this,
            actionGridPosition = unit.gridPosition,
            actionValue = -1
        };
    }

    public override NPCAIAction GetNPCAIAction_ActionGridPosition(GridPosition actionGridPosition)
    {
        float finalActionValue = 0f;

        // Make sure there's a Unit at this grid position
        if (LevelGrid.Instance.HasAnyUnitOnGridPosition(actionGridPosition))
        {
            // Adjust the finalActionValue based on the Alliance of the unit at the grid position
            Unit unitAtGridPosition = LevelGrid.Instance.GetUnitAtGridPosition(actionGridPosition);
            if (unit.alliance.IsEnemy(unitAtGridPosition))
            {
                // Enemies in the action area increase this action's value
                finalActionValue += 70f;

                // Lower enemy health gives this action more value
                finalActionValue += 70f - (unitAtGridPosition.health.CurrentHealthNormalized() * 70f);

                // Favor the targetEnemyUnit
                if (unit.unitActionHandler.targetEnemyUnit != null && unitAtGridPosition == unit.unitActionHandler.targetEnemyUnit)
                    finalActionValue += 15f;
            }
            else
                finalActionValue = -1f;

            return new NPCAIAction
            {
                baseAction = this,
                actionGridPosition = actionGridPosition,
                actionValue = Mathf.RoundToInt(finalActionValue)
            };
        }

        return new NPCAIAction
        {
            baseAction = this,
            actionGridPosition = unit.gridPosition,
            actionValue = -1
        };
    }

    public override int GetActionPointsCost()
    {
        if (nextAttackFree)
        {
            nextAttackFree = false;
            return 0;
        }
        return 300;
    }

    public override List<GridPosition> GetActionGridPositionsInRange(GridPosition startGridPosition)
    {
        float minRange;
        float maxRange;

        if (unit.IsUnarmed())
        {
            minRange = 1f;
            maxRange = UnarmedAttackRange(startGridPosition, false);
        }
        else
        {
            minRange = unit.GetPrimaryMeleeWeapon().itemData.item.Weapon().minRange;
            maxRange = unit.GetPrimaryMeleeWeapon().itemData.item.Weapon().maxRange;
        }

        float boundsDimension = (maxRange * 2) + 0.1f;

        validGridPositionsList.Clear();
        List<GraphNode> nodes = ListPool<GraphNode>.Claim();
        nodes = AstarPath.active.data.layerGridGraph.GetNodesInRegion(new Bounds(startGridPosition.WorldPosition(), new Vector3(boundsDimension, boundsDimension, boundsDimension)));

        for (int i = 0; i < nodes.Count; i++)
        {
            GridPosition nodeGridPosition = new GridPosition((Vector3)nodes[i].position);

            if (LevelGrid.IsValidGridPosition(nodeGridPosition) == false)
                continue;

            float maxRangeToNodePosition = maxRange - Mathf.Abs(nodeGridPosition.y - startGridPosition.y);
            if (maxRangeToNodePosition < 0f) maxRangeToNodePosition = 0f;

            float distance = TacticsPathfindingUtilities.CalculateWorldSpaceDistance_XZ(startGridPosition, nodeGridPosition);
            if (distance > maxRangeToNodePosition || distance < minRange)
                continue;

            // Check for obstacles
            float sphereCastRadius = 0.1f;
            Vector3 offset = Vector3.up * unit.ShoulderHeight() * 2f;
            Vector3 shootDir = ((nodeGridPosition.WorldPosition() + offset) - (startGridPosition.WorldPosition() + offset)).normalized;
            if (Physics.SphereCast(startGridPosition.WorldPosition() + offset, sphereCastRadius, shootDir, out RaycastHit hit, Vector3.Distance(unit.WorldPosition() + offset, nodeGridPosition.WorldPosition() + offset), unit.unitActionHandler.AttackObstacleMask()))
                continue;

            validGridPositionsList.Add(nodeGridPosition);
        }

        ListPool<GraphNode>.Release(nodes);
        return validGridPositionsList;
    }

    public override List<GridPosition> GetActionAreaGridPositions(GridPosition targetGridPosition)
    {
        validGridPositionsList.Clear();
        if (LevelGrid.IsValidGridPosition(targetGridPosition) == false)
            return validGridPositionsList;

        if (IsInAttackRange(null, unit.gridPosition, targetGridPosition) == false)
            return validGridPositionsList;

        float sphereCastRadius = 0.1f;
        Vector3 offset = Vector3.up * unit.ShoulderHeight() * 2f;
        Vector3 shootDir = ((unit.WorldPosition() + offset) - (targetGridPosition.WorldPosition() + offset)).normalized;
        if (Physics.SphereCast(targetGridPosition.WorldPosition() + offset, sphereCastRadius, shootDir, out RaycastHit hit, Vector3.Distance(unit.WorldPosition() + offset, targetGridPosition.WorldPosition() + offset), unit.unitActionHandler.AttackObstacleMask()))
            return validGridPositionsList; // Blocked by an obstacle

        validGridPositionsList.Add(targetGridPosition);
        return validGridPositionsList;
    }

    public List<GridPosition> GetValidGridPositions_InRangeOfTarget(Unit targetUnit)
    {
        validGridPositionsList.Clear();
        if (targetUnit == null)
            return validGridPositionsList;

        float maxAttackRange;
        if (unit.MeleeWeaponEquipped())
            maxAttackRange = unit.GetPrimaryMeleeWeapon().itemData.item.Weapon().maxRange;
        else
            maxAttackRange = unarmedAttackRange;

        float boundsDimension = (maxAttackRange * 2) + 0.1f;
        List<GraphNode> nodes = ListPool<GraphNode>.Claim();
        nodes = AstarPath.active.data.layerGridGraph.GetNodesInRegion(new Bounds(targetUnit.gridPosition.WorldPosition(), new Vector3(boundsDimension, boundsDimension, boundsDimension)));

        for (int i = 0; i < nodes.Count; i++)
        {
            GridPosition nodeGridPosition = new GridPosition((Vector3)nodes[i].position);

            if (LevelGrid.IsValidGridPosition(nodeGridPosition) == false)
                continue;

            // If Grid Position has a Unit there already
            if (LevelGrid.Instance.HasAnyUnitOnGridPosition(nodeGridPosition)) 
                continue;
            
            // If target is out of attack range from this Grid Position
            if (IsInAttackRange(null, nodeGridPosition, targetUnit.gridPosition) == false)
                continue;

            float sphereCastRadius = 0.1f;
            Vector3 attackDir = ((nodeGridPosition.WorldPosition() + (Vector3.up * unit.ShoulderHeight() * 2f)) - (targetUnit.WorldPosition() + (Vector3.up * targetUnit.ShoulderHeight() * 2f))).normalized;
            if (Physics.SphereCast(targetUnit.WorldPosition() + (Vector3.up * targetUnit.ShoulderHeight() * 2f), sphereCastRadius, attackDir, out RaycastHit hit, Vector3.Distance(nodeGridPosition.WorldPosition() + (Vector3.up * unit.ShoulderHeight() * 2f), targetUnit.WorldPosition() + (Vector3.up * targetUnit.ShoulderHeight() * 2f)), unit.unitActionHandler.AttackObstacleMask()))
                continue; // Blocked by an obstacle

            validGridPositionsList.Add(nodeGridPosition);
        }

        ListPool<GraphNode>.Release(nodes);
        return validGridPositionsList;
    }

    public override GridPosition GetNearestAttackPosition(GridPosition startGridPosition, Unit targetUnit)
    {
        nearestGridPositionsList.Clear();
        List<GridPosition> gridPositions = ListPool<GridPosition>.Claim();
        gridPositions = GetValidGridPositions_InRangeOfTarget(targetUnit);
        float nearestDistance = 10000f;

        // First, find the nearest valid Grid Positions to the Player
        for (int i = 0; i < gridPositions.Count; i++)
        {
            float distance = Vector3.Distance(gridPositions[i].WorldPosition(), startGridPosition.WorldPosition());
            if (distance < nearestDistance)
            {
                nearestGridPositionsList.Clear();
                nearestGridPositionsList.Add(gridPositions[i]);
                nearestDistance = distance;
            }
            else if (Mathf.Approximately(distance, nearestDistance))
                nearestGridPositionsList.Add(gridPositions[i]);
        }

        GridPosition nearestGridPosition = startGridPosition;
        float nearestDistanceToTarget = 10000f;
        for (int i = 0; i < nearestGridPositionsList.Count; i++)
        {
            // Get the Grid Position that is closest to the target Grid Position
            float distance = Vector3.Distance(nearestGridPositionsList[i].WorldPosition(), targetUnit.transform.position);
            if (distance < nearestDistanceToTarget)
            {
                nearestDistanceToTarget = distance;
                nearestGridPosition = nearestGridPositionsList[i];
            }
        }

        ListPool<GridPosition>.Release(gridPositions);
        return nearestGridPosition;
    }

    public override bool IsValidUnitInActionArea(GridPosition targetGridPosition)
    {
        if (LevelGrid.Instance.HasAnyUnitOnGridPosition(targetGridPosition) && unit.alliance.IsAlly(LevelGrid.Instance.GetUnitAtGridPosition(targetGridPosition)) == false)
            return true;
        return false;
    }

    public override bool IsValidAction()
    {
        if (unit.MeleeWeaponEquipped() || (canFightUnarmed && unit.IsUnarmed()))
            return true;
        return false;
    }

    public override bool IsAttackAction() => true;

    public override bool IsMeleeAttackAction() => true;

    public override bool IsRangedAttackAction() => false;

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