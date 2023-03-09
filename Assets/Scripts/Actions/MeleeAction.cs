using Pathfinding;
using Pathfinding.Util;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

enum MeleeSkill { Default, Swipe, Charge }

public class MeleeAction : BaseAction
{
    [Header("Unarmed Combat")]
    [SerializeField] bool canFightUnarmed;
    [SerializeField] float unarmedAttackRange = 1.4f;
    [SerializeField] int baseUnarmedDamage = 5;

    List<GridPosition> validGridPositionsList = new List<GridPosition>();
    List<GridPosition> nearestGridPositionsList = new List<GridPosition>();

    MeleeSkill currentMeleeSkill = MeleeSkill.Default;

    public bool isAttacking { get; private set; }
    bool nextAttackFree;

    readonly float dualWieldPrimaryEfficiency = 0.8f;
    readonly float dualWieldSecondaryEfficiency = 0.6f;

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
        Unit targetUnit = unit.unitActionHandler.targetEnemyUnit;
        BecomeVisibleEnemyOfTarget(targetUnit);

        switch (currentMeleeSkill)
        {
            case MeleeSkill.Default:
                DefaultAttack(targetUnit);
                break;
            case MeleeSkill.Swipe:
                break;
            case MeleeSkill.Charge:
                break;
            default:
                break;
        }
    }

    void DefaultAttack(Unit targetUnit)
    {
        if (unit.IsPlayer() || unit.IsVisibleOnScreen())
        {
            if (unit.IsUnarmed())
            {
                if (canFightUnarmed)
                    unit.unitAnimator.DoUnarmedAttack(AttackBlocked(targetUnit, out HeldItem itemBlockedWith), itemBlockedWith);
            }
            else if (unit.IsDualWielding())
            {
                // Dual wield attack
                unit.unitAnimator.StartDualMeleeAttack();
                unit.rightHeldItem.DoDefaultAttack(AttackBlocked(targetUnit, out HeldItem itemBlockedWith), itemBlockedWith);
                StartCoroutine(unit.leftHeldItem.DelayDoDefaultAttack(AttackBlocked(targetUnit, out HeldItem secondaryItemBlockedWith), secondaryItemBlockedWith));
            }
            else if (unit.rightHeldItem != null) // Right hand weapon attack
            {
                unit.unitAnimator.StartMeleeAttack();
                unit.rightHeldItem.DoDefaultAttack(AttackBlocked(targetUnit, out HeldItem itemBlockedWith), itemBlockedWith);
            }
            else if (unit.leftHeldItem != null) // Left hand weapon attack
            {
                unit.unitAnimator.StartMeleeAttack();
                unit.leftHeldItem.DoDefaultAttack(AttackBlocked(targetUnit, out HeldItem itemBlockedWith), itemBlockedWith);
            }

            StartCoroutine(WaitToCompleteAction());
        }
        else // If this is an NPC who's outside of the screen, instantly damage the target without an animation
        {
            bool attackBlocked = false;
            if (unit.IsUnarmed())
            {
                attackBlocked = AttackBlocked(targetUnit, out HeldItem itemBlockedWith);
                DamageTarget(null, attackBlocked, itemBlockedWith);
            }
            else if (unit.IsDualWielding()) // Dual wield attack
            {
                bool mainAttackBlocked = AttackBlocked(targetUnit, out HeldItem itemBlockedWith);
                bool offhandAttackBlocked = AttackBlocked(targetUnit, out HeldItem secondaryItemBlockedWith);
                if (mainAttackBlocked || offhandAttackBlocked)
                    attackBlocked = true;

                DamageTarget(unit.rightHeldItem as HeldMeleeWeapon, mainAttackBlocked, itemBlockedWith);
                DamageTarget(unit.leftHeldItem as HeldMeleeWeapon, offhandAttackBlocked, secondaryItemBlockedWith);
            }
            else if (unit.rightHeldItem != null)
            {
                attackBlocked = AttackBlocked(targetUnit, out HeldItem itemBlockedWith);
                DamageTarget(unit.rightHeldItem as HeldMeleeWeapon, attackBlocked, itemBlockedWith); // Right hand weapon attack
            }
            else if (unit.leftHeldItem != null)
            {
                attackBlocked = AttackBlocked(targetUnit, out HeldItem itemBlockedWith);
                DamageTarget(unit.leftHeldItem as HeldMeleeWeapon, attackBlocked, itemBlockedWith); // Left hand weapon attack
            }

            if (attackBlocked)
                StartCoroutine(targetUnit.unitActionHandler.GetAction<TurnAction>().RotateTowards_AttackingTargetUnit(unit, true));

            CompleteAction();
            TurnManager.Instance.StartNextUnitsTurn(unit);
        }
    }

    public void DamageTarget(HeldMeleeWeapon heldMeleeWeapon, bool attackBlocked, HeldItem itemBlockedWith)
    {
        Unit targetUnit = unit.unitActionHandler.targetEnemyUnit;
        if (targetUnit != null)
        {
            int armorAbsorbAmount = 0;

            int damageAmount;
            if (heldMeleeWeapon == null) // If unarmed
                damageAmount = UnarmedDamage();
            else
                damageAmount = heldMeleeWeapon.itemData.damage;

            if (unit.IsDualWielding())
            {
                if (this == unit.GetRightMeleeWeapon())
                    damageAmount = Mathf.RoundToInt(damageAmount * dualWieldPrimaryEfficiency);
                else
                    damageAmount = Mathf.RoundToInt(damageAmount * dualWieldSecondaryEfficiency);
            }

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
                                blockAmount = Mathf.RoundToInt(blockAmount * dualWieldPrimaryEfficiency);
                        }
                    }
                    else // If blocked with dual wield left hand weapon
                        blockAmount = Mathf.RoundToInt(targetUnit.stats.WeaponBlockPower(targetUnit.GetLeftMeleeWeapon()) * dualWieldSecondaryEfficiency);
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

    bool AttackBlocked(Unit targetUnit, out HeldItem itemBlockedWith)
    {
        float random;
        TurnAction targetUnitTurnAction = targetUnit.unitActionHandler.GetAction<TurnAction>();
        if (targetUnitTurnAction.AttackerInFrontOfUnit(unit))
        {
            if (targetUnit.ShieldEquipped())
            {
                // Try blocking with shield
                random = Random.Range(1f, 100f);
                if (random <= targetUnit.stats.ShieldBlockChance(targetUnit.GetShield(), false))
                {
                    itemBlockedWith = targetUnit.GetShield();
                    return true;
                }

                // Still have a chance to block with weapon
                if (targetUnit.MeleeWeaponEquipped())
                {
                    random = Random.Range(1f, 100f);
                    if (random <= targetUnit.stats.WeaponBlockChance(targetUnit.GetPrimaryMeleeWeapon(), false, true))
                    {
                        itemBlockedWith = targetUnit.GetPrimaryMeleeWeapon();
                        return true;
                    }
                }
            }
            else if (targetUnit.MeleeWeaponEquipped())
            {
                if (targetUnit.IsDualWielding())
                {
                    // Try blocking with right weapon
                    random = Random.Range(1f, 100f);
                    if (random <= targetUnit.stats.WeaponBlockChance(targetUnit.GetPrimaryMeleeWeapon(), false, false) * dualWieldPrimaryEfficiency)
                    {
                        itemBlockedWith = targetUnit.GetPrimaryMeleeWeapon();
                        return true;
                    }

                    // Try blocking with left weapon
                    random = Random.Range(1f, 100f);
                    if (random <= targetUnit.stats.WeaponBlockChance(targetUnit.GetLeftMeleeWeapon(), false, false) * dualWieldSecondaryEfficiency)
                    {
                        itemBlockedWith = targetUnit.GetLeftMeleeWeapon();
                        return true;
                    }
                }
                else
                {
                    // Try blocking with only weapon
                    random = Random.Range(1f, 100f);
                    if (random <= targetUnit.stats.WeaponBlockChance(targetUnit.GetPrimaryMeleeWeapon(), false, false))
                    {
                        itemBlockedWith = targetUnit.GetPrimaryMeleeWeapon();
                        return true;
                    }
                }
            }
        }
        else if (targetUnitTurnAction.AttackerBesideUnit(unit))
        {
            if (targetUnit.ShieldEquipped())
            {
                // Try blocking with shield
                random = Random.Range(1f, 100f);
                if (random <= targetUnit.stats.ShieldBlockChance(targetUnit.GetShield(), true))
                {
                    itemBlockedWith = targetUnit.GetShield();
                    return true;
                }

                // Still have a chance to block with weapon
                if (targetUnit.MeleeWeaponEquipped())
                {
                    random = Random.Range(1f, 100f);
                    if (random <= targetUnit.stats.WeaponBlockChance(targetUnit.GetPrimaryMeleeWeapon(), true, true))
                    {
                        itemBlockedWith = targetUnit.GetPrimaryMeleeWeapon();
                        return true;
                    }
                }
            }
            else if (targetUnit.MeleeWeaponEquipped())
            {
                if (targetUnit.IsDualWielding())
                {
                    // Try blocking with right weapon
                    random = Random.Range(1f, 100f);
                    if (random <= targetUnit.stats.WeaponBlockChance(targetUnit.GetPrimaryMeleeWeapon(), true, false) * dualWieldPrimaryEfficiency)
                    {
                        itemBlockedWith = targetUnit.GetPrimaryMeleeWeapon();
                        return true;
                    }

                    // Try blocking with left weapon
                    random = Random.Range(1f, 100f);
                    if (random <= targetUnit.stats.WeaponBlockChance(targetUnit.GetLeftMeleeWeapon(), true, false) * dualWieldSecondaryEfficiency)
                    {
                        itemBlockedWith = targetUnit.GetLeftMeleeWeapon();
                        return true;
                    }
                }
                else
                {
                    // Try blocking with only weapon
                    random = Random.Range(1f, 100f);
                    if (random <= targetUnit.stats.WeaponBlockChance(targetUnit.GetPrimaryMeleeWeapon(), true, false))
                    {
                        itemBlockedWith = targetUnit.GetPrimaryMeleeWeapon();
                        return true;
                    }
                }
            }
        }

        itemBlockedWith = null;
        return false;
    }

    IEnumerator WaitToCompleteAction()
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
        TurnManager.Instance.StartNextUnitsTurn(unit);
    }

    public bool IsInAttackRange(Unit targetUnit, GridPosition startGridPosition, GridPosition targetGridPosition)
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

    public bool IsInAttackRange(Unit targetUnit) => IsInAttackRange(targetUnit, unit.gridPosition, targetUnit.gridPosition);

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
                float minAttackRange = 1f;
                if (unit.MeleeWeaponEquipped())
                    minAttackRange = unit.GetPrimaryMeleeWeapon().itemData.item.Weapon().minRange;

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
        float maxAttackRange;
        if (unit.MeleeWeaponEquipped())
            maxAttackRange = unit.GetPrimaryMeleeWeapon().itemData.item.Weapon().maxRange;
        else
            maxAttackRange = unarmedAttackRange;

        float boundsDimension = (maxAttackRange * 2) + 0.1f;
        validGridPositionsList.Clear();
        List<GraphNode> nodes = ListPool<GraphNode>.Claim();
        nodes = AstarPath.active.data.layerGridGraph.GetNodesInRegion(new Bounds(startGridPosition.WorldPosition(), new Vector3(boundsDimension, boundsDimension, boundsDimension)));

        for (int i = 0; i < nodes.Count; i++)
        {
            GridPosition nodeGridPosition = new GridPosition((Vector3)nodes[i].position);

            if (LevelGrid.IsValidGridPosition(nodeGridPosition) == false)
                continue;

            if (LevelGrid.Instance.HasAnyUnitOnGridPosition(nodeGridPosition) == false) // Grid Position is empty, no Unit to shoot
                continue;

            Unit targetUnit = LevelGrid.Instance.GetUnitAtGridPosition(nodeGridPosition);

            // If the target is dead
            if (targetUnit.health.IsDead())
                continue;

            // If the target is out of sight
            if (unit.vision.IsVisible(targetUnit) == false)
                continue;

            // If both Units are on the same team
            if (unit.alliance.IsAlly(targetUnit))
                continue;

            // If target is out of attack range
            if (IsInAttackRange(targetUnit, startGridPosition, nodeGridPosition) == false)
                continue;

            // Debug.Log(gridPosition);
            validGridPositionsList.Add(nodeGridPosition);
        }

        ListPool<GraphNode>.Release(nodes);
        return validGridPositionsList;
    }

    public override List<GridPosition> GetValidActionGridPositionList_Secondary(GridPosition startGridPosition)
    {
        float maxAttackRange;
        if (unit.MeleeWeaponEquipped())
            maxAttackRange = unit.GetPrimaryMeleeWeapon().itemData.item.Weapon().maxRange;
        else
            maxAttackRange = unarmedAttackRange;

        float boundsDimension = (maxAttackRange * 2) + 0.1f;
        validGridPositionsList.Clear();
        List<GraphNode> nodes = ListPool<GraphNode>.Claim();
        nodes = AstarPath.active.data.layerGridGraph.GetNodesInRegion(new Bounds(startGridPosition.WorldPosition(), new Vector3(boundsDimension, boundsDimension, boundsDimension)));

        for (int i = 0; i < nodes.Count; i++)
        {
            GridPosition nodeGridPosition = new GridPosition((Vector3)nodes[i].position);

            if (LevelGrid.IsValidGridPosition(nodeGridPosition) == false)
                continue;

            if (LevelGrid.Instance.HasAnyUnitOnGridPosition(nodeGridPosition) == false) // Grid Position is empty, no Unit to shoot
                continue;

            Unit targetUnit = LevelGrid.Instance.GetUnitAtGridPosition(nodeGridPosition);

            // If the target is dead
            if (targetUnit.health.IsDead())
                continue;

            // If the target is out of sight
            if (unit.vision.IsVisible(targetUnit) == false)
                continue;
            
            // If the Units aren't neutral to each other
            if (unit.alliance.IsNeutral(targetUnit) == false)
                continue;
            Debug.Log(targetUnit.name + " is neutral");
            // If target is out of attack range
            if (IsInAttackRange(targetUnit, startGridPosition, nodeGridPosition) == false)
                continue;
            Debug.Log(targetUnit.name + " is in attack range");
            // Debug.Log(gridPosition);
            validGridPositionsList.Add(nodeGridPosition);
        }

        ListPool<GraphNode>.Release(nodes);
        return validGridPositionsList;
    }

    public List<GridPosition> GetValidGridPositionsInRange(Unit targetUnit)
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

    public GridPosition GetNearestMeleePosition(GridPosition startGridPosition, Unit targetUnit)
    {
        nearestGridPositionsList.Clear();
        List<GridPosition> gridPositions = ListPool<GridPosition>.Claim();
        gridPositions = GetValidGridPositionsInRange(targetUnit);
        float nearestDistance = 10000000f;

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
        float nearestDistanceToTarget = 10000000f;
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
