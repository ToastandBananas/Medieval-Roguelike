using Pathfinding;
using Pathfinding.Util;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SwipeAction : BaseAction
{
    public bool isAttacking { get; private set; }
    bool nextAttackFree;

    List<GridPosition> validGridPositionsList = new List<GridPosition>();
    List<GridPosition> nearestGridPositionsList = new List<GridPosition>();
    List<Vector3> potentialPositions = new List<Vector3>();

    void Start()
    {
        unit.unitActionHandler.GetAction<MoveAction>().OnStopMoving += MoveAction_OnStopMoving;
    }

    public override void TakeAction(GridPosition gridPosition)
    {
        if (isAttacking) return;

        if (IsValidUnitInActionArea(gridPosition) == false)
        {
            unit.unitActionHandler.SetTargetEnemyUnit(null);
            unit.unitActionHandler.FinishAction();

            Debug.Log("No valid unit in action area");
            unit.unitActionHandler.SetQueuedAttack(null);
            return;
        }

        StartAction();

        if (IsInAttackRange(null, unit.gridPosition, unit.unitActionHandler.targetAttackGridPosition))
        {
            if (unit.unitActionHandler.GetAction<TurnAction>().IsFacingTarget(unit.unitActionHandler.targetAttackGridPosition))
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

    void Attack()
    {
        Debug.Log("Do Swipe Attack");

        foreach(GridPosition gridPosition in GetActionAreaGridPositions(unit.unitActionHandler.targetAttackGridPosition))
        {

        }

        Unit targetUnit = unit.unitActionHandler.targetEnemyUnit;
        BecomeVisibleEnemyOfTarget(targetUnit);

        if (unit.IsPlayer() || unit.IsVisibleOnScreen())
        {
            if (unit.rightHeldItem != null) // Right hand weapon attack
            {
                unit.unitAnimator.StartMeleeAttack();
                unit.rightHeldItem.DoDefaultAttack(targetUnit.TryBlockMeleeAttack(unit, out HeldItem itemBlockedWith), itemBlockedWith);
            }
            else if (unit.leftHeldItem != null) // Left hand weapon attack
            {
                unit.unitAnimator.StartMeleeAttack();
                unit.leftHeldItem.DoDefaultAttack(targetUnit.TryBlockMeleeAttack(unit, out HeldItem itemBlockedWith), itemBlockedWith);
            }

            StartCoroutine(WaitToCompleteAction());
        }
        else // If this is an NPC who's outside of the screen, instantly damage the target without an animation
        {
            bool attackBlocked = false;
            if (unit.rightHeldItem != null)
            {
                attackBlocked = targetUnit.TryBlockMeleeAttack(unit, out HeldItem itemBlockedWith);
                DamageTargets(unit.rightHeldItem as HeldMeleeWeapon, attackBlocked, itemBlockedWith); // Right hand weapon attack
            }
            else if (unit.leftHeldItem != null)
            {
                attackBlocked = targetUnit.TryBlockMeleeAttack(unit, out HeldItem itemBlockedWith);
                DamageTargets(unit.leftHeldItem as HeldMeleeWeapon, attackBlocked, itemBlockedWith); // Left hand weapon attack
            }

            if (attackBlocked)
                StartCoroutine(targetUnit.unitActionHandler.GetAction<TurnAction>().RotateTowards_AttackingTargetUnit(unit, true));

            CompleteAction();
            TurnManager.Instance.StartNextUnitsTurn(unit);
        }
    }

    public override void DamageTargets(HeldMeleeWeapon heldMeleeWeapon, bool attackBlocked, HeldItem itemBlockedWith)
    {
        Unit targetUnit = unit.unitActionHandler.targetEnemyUnit;
        if (targetUnit != null)
        {
            int armorAbsorbAmount = 0;
            int damageAmount = heldMeleeWeapon.DamageAmount();

            if (attackBlocked)
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

                if (heldMeleeWeapon != null)
                    heldMeleeWeapon.ResetAttackBlocked();

                if (itemBlockedWith is HeldShield)
                    targetUnit.GetShield().LowerShield();
                else
                {
                    HeldMeleeWeapon heldWeapon = itemBlockedWith as HeldMeleeWeapon;
                    heldWeapon.LowerWeapon();
                }
            }
            else
                targetUnit.health.TakeDamage(damageAmount - armorAbsorbAmount);
        }

        if (unit.IsPlayer() && PlayerInput.Instance.autoAttack == false)
            unit.unitActionHandler.SetTargetEnemyUnit(null);
    }

    public override EnemyAIAction GetEnemyAIAction(Unit targetUnit)
    {
        float finalActionValue = 0f;
        if (targetUnit != null && IsValidAction())
        {
            if (targetUnit != null && targetUnit.health.IsDead() == false)
            {
                // Target the Unit with the lowest health and/or the nearest target
                finalActionValue += 500 - (targetUnit.health.CurrentHealthNormalized() * 100f);
                float distance = TacticsPathfindingUtilities.CalculateWorldSpaceDistance_XYZ(unit.gridPosition, targetUnit.gridPosition);
                float minAttackRange = unit.GetPrimaryMeleeWeapon().itemData.item.Weapon().minRange;

                if (distance < minAttackRange)
                    finalActionValue = 0f;
                else
                    finalActionValue -= distance * 10f;
            }
        }

        if (targetUnit == null)
            return new EnemyAIAction
            {
                unit = null,
                gridPosition = unit.gridPosition,
                actionValue = -1
            };

        return new EnemyAIAction
        {
            unit = targetUnit,
            gridPosition = targetUnit.gridPosition,
            actionValue = Mathf.RoundToInt(finalActionValue)
        };
    }

    public List<GridPosition> GetValidGridPositionsInRange(Unit targetUnit)
    {
        validGridPositionsList.Clear();
        if (targetUnit == null)
            return validGridPositionsList;

        float maxAttackRange = unit.GetPrimaryMeleeWeapon().itemData.item.Weapon().maxRange;

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

            // Check for obstacles
            float sphereCastRadius = 0.1f;
            Vector3 attackDir = ((nodeGridPosition.WorldPosition() + (Vector3.up * unit.ShoulderHeight() * 2f)) - (targetUnit.WorldPosition() + (Vector3.up * targetUnit.ShoulderHeight() * 2f))).normalized;
            if (Physics.SphereCast(targetUnit.WorldPosition() + (Vector3.up * targetUnit.ShoulderHeight() * 2f), sphereCastRadius, attackDir, out RaycastHit hit, Vector3.Distance(nodeGridPosition.WorldPosition() + (Vector3.up * unit.ShoulderHeight() * 2f), targetUnit.WorldPosition() + (Vector3.up * targetUnit.ShoulderHeight() * 2f)), unit.unitActionHandler.AttackObstacleMask()))
                continue;

            validGridPositionsList.Add(nodeGridPosition);
        }

        ListPool<GraphNode>.Release(nodes);
        return validGridPositionsList;
    }

    public override List<GridPosition> GetActionAreaGridPositions(GridPosition targetGridPosition)
    {
        validGridPositionsList.Clear();

        // Get the closest cardinal or intermediate direction
        Vector3 directionToTarget = (targetGridPosition.WorldPosition() - unit.WorldPosition()).normalized;
        Vector3 generalDirection = GetGeneralDirection(directionToTarget);

        if (LevelGrid.IsValidGridPosition(targetGridPosition) == false)
            return validGridPositionsList;

        // Exclude attacker's position
        if (targetGridPosition == unit.gridPosition)
            return validGridPositionsList;

        // Check if the target position is within the max attack range
        //if (Vector3.Distance(unit.WorldPosition(), targetGridPosition.WorldPosition()) > maxAttackRange)
        if (IsInAttackRange(null, unit.gridPosition, targetGridPosition) == false)
            return validGridPositionsList;

        // Check for obstacles
        float sphereCastRadius = 0.1f;
        Vector3 heightOffset = Vector3.up * unit.ShoulderHeight() * 2f;
        if (Physics.SphereCast(unit.WorldPosition() + heightOffset, sphereCastRadius, directionToTarget, out RaycastHit hit, Vector3.Distance(targetGridPosition.WorldPosition() + heightOffset, unit.WorldPosition() + heightOffset), unit.unitActionHandler.AttackObstacleMask()))
        {
            // Debug.Log(targetGridPosition.WorldPosition() + " (the target position) is blocked by " + hit.collider.name);
            return validGridPositionsList; ;
        }

        float maxAttackRange = unit.GetPrimaryMeleeWeapon().itemData.item.Weapon().maxRange;
        float boundsDimension = (maxAttackRange * 2) + 0.1f;
        List<GraphNode> nodes = ListPool<GraphNode>.Claim();
        nodes = AstarPath.active.data.layerGridGraph.GetNodesInRegion(new Bounds(targetGridPosition.WorldPosition(), new Vector3(boundsDimension, boundsDimension, boundsDimension)));

        // Validate the potential attack positions based on the requirements
        for (int i = 0; i < nodes.Count; i++)
        {
            GridPosition nodeGridPosition = new GridPosition((Vector3)nodes[i].position); 
            
            if (LevelGrid.IsValidGridPosition(nodeGridPosition) == false)
                continue;

            // Exclude attacker's position
            if (nodeGridPosition == unit.gridPosition)
                continue;

            // Check if the node is in the general direction of the attack
            Vector3 yDifference = Vector3.up * (nodeGridPosition.WorldPosition().y - unit.WorldPosition().y);
            Vector3 directionToNode = (nodeGridPosition.WorldPosition() - (unit.WorldPosition() + yDifference)).normalized;
            float angleBetweenDirections = Vector3.Angle(generalDirection, directionToNode);
            if (angleBetweenDirections > 45f)
                continue;

            // Check if the node is within the max attack range
            //if (Vector3.Distance(unit.WorldPosition(), nodeGridPosition.WorldPosition()) > maxAttackRange)
            if (IsInAttackRange(null, unit.gridPosition, nodeGridPosition) == false)
                continue;

            // Make sure the node isn't too much lower or higher than the target grid position (this is a swipe attack, so think of it basically swiping across in a horizontal line)
            if (Mathf.Abs(nodeGridPosition.WorldPosition().y - targetGridPosition.WorldPosition().y) > 0.5f)
                continue;

            // Check for obstacles
            Vector3 directionToAttackPosition = ((nodeGridPosition.WorldPosition() + heightOffset) - (unit.WorldPosition() + heightOffset)).normalized;
            if (Physics.SphereCast(unit.WorldPosition() + heightOffset, sphereCastRadius, directionToAttackPosition, out hit, Vector3.Distance(nodeGridPosition.WorldPosition() + heightOffset, unit.WorldPosition() + heightOffset), unit.unitActionHandler.AttackObstacleMask()))
            {
                // Debug.Log(nodeGridPosition.WorldPosition() + " is blocked by " + hit.collider.name);
                continue;
            }

            validGridPositionsList.Add(nodeGridPosition);
        }

        ListPool<GraphNode>.Release(nodes);
        return validGridPositionsList;
    }

    Vector3 GetGeneralDirection(Vector3 directionToTarget)
    {
        // Add the cardinal directions, each represented by a Vector3, to an array
        Vector3[] directions = {
            Vector3.forward,
            Vector3.back,
            Vector3.right,
            Vector3.left,
            (Vector3.forward + Vector3.right).normalized,
            (Vector3.forward + Vector3.left).normalized,
            (Vector3.back + Vector3.right).normalized,
            (Vector3.back + Vector3.left).normalized
        };

        float maxDot = float.MinValue;
        Vector3 generalDirection = Vector3.zero;

        foreach (Vector3 dir in directions)
        {
            float dot = Vector3.Dot(directionToTarget, dir);
            if (dot > maxDot)
            {
                maxDot = dot;
                generalDirection = dir;
            }
        }

        return generalDirection;
    }

    public GridPosition GetNearestMeleePosition(GridPosition startGridPosition, Unit targetUnit)
    {
        nearestGridPositionsList.Clear();
        List<GridPosition> gridPositions = ListPool<GridPosition>.Claim();
        gridPositions = GetValidGridPositionsInRange(targetUnit);
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

    public override GridPosition GetNearestAttackPosition(GridPosition startGridPosition, Unit targetUnit)
    {
        nearestGridPositionsList.Clear();
        List<GridPosition> gridPositions = ListPool<GridPosition>.Claim();
        gridPositions = GetValidGridPositionsInRange(targetUnit);
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
        List<GridPosition> attackGridPositions = ListPool<GridPosition>.Claim();
        attackGridPositions = GetActionAreaGridPositions(targetGridPosition);

        for (int i = 0; i < attackGridPositions.Count; i++)
        {
            if (LevelGrid.Instance.HasAnyUnitOnGridPosition(attackGridPositions[i]) == false)
                continue;

            Unit unitAtGridPosition = LevelGrid.Instance.GetUnitAtGridPosition(attackGridPositions[i]);
            if (unit.alliance.IsAlly(unitAtGridPosition))
                continue;

            if (unit.health.IsDead())
                continue;

            // If the loop makes it to this point, then it found a valid unit
            ListPool<GridPosition>.Release(attackGridPositions);
            return true;
        }

        ListPool<GridPosition>.Release(attackGridPositions);
        return false;
    }

    public override bool IsInAttackRange(Unit targetUnit, GridPosition startGridPosition, GridPosition targetGridPosition) => unit.unitActionHandler.GetAction<MeleeAction>().IsInAttackRange(targetUnit, startGridPosition, targetGridPosition);

    public override bool IsInAttackRange(Unit targetUnit) => unit.unitActionHandler.GetAction<MeleeAction>().IsInAttackRange(targetUnit);

    public override int GetActionPointsCost()
    {
        if (nextAttackFree)
        {
            nextAttackFree = false;
            return 0;
        }
        return 300;
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
        if (unit.GetPrimaryMeleeWeapon() != null)
            yield return new WaitForSeconds(AnimationTimes.Instance.GetWeaponAttackAnimationTime(unit.GetPrimaryMeleeWeapon().itemData.item as Weapon) / 2f);
        else
            yield return new WaitForSeconds(0.25f);

        CompleteAction();
        TurnManager.Instance.StartNextUnitsTurn(unit);
    }

    void MoveAction_OnStopMoving(object sender, EventArgs e) => nextAttackFree = false;

    public override bool IsValidAction() => unit.MeleeWeaponEquipped();

    public override bool IsAttackAction() => true;

    public override bool IsMeleeAttackAction() => true;

    public override bool IsRangedAttackAction() => false;

    public override bool ActionIsUsedInstantly() => false;

    public override string GetActionName() => "Swipe";
}
