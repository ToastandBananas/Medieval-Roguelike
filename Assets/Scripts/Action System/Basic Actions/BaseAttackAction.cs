using GridSystem;
using InventorySystem;
using Pathfinding;
using Pathfinding.Util;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Utilities;

namespace UnitSystem.ActionSystem
{
    public abstract class BaseAttackAction : BaseAction
    {
        public Unit TargetEnemyUnit { get; protected set; }

        protected List<GridPosition> validGridPositionsList = new List<GridPosition>();
        protected List<GridPosition> nearestGridPositionsList = new List<GridPosition>();

        public virtual void QueueAction(Unit targetEnemyUnit)
        {
            this.TargetEnemyUnit = targetEnemyUnit;
            TargetGridPosition = targetEnemyUnit.GridPosition;
            QueueAction();
        }

        public override void QueueAction(GridPosition targetGridPosition)
        {
            this.TargetGridPosition = targetGridPosition;
            if (LevelGrid.HasUnitAtGridPosition(targetGridPosition, out Unit targetUnit))
                TargetEnemyUnit = targetUnit;
            QueueAction();
        }

        protected void MoveToTargetInstead()
        {
            CompleteAction();
            Unit.unitActionHandler.SetIsAttacking(false);
            Unit.unitActionHandler.MoveAction.QueueAction(GetNearestAttackPosition(Unit.GridPosition, TargetEnemyUnit));
            TurnManager.Instance.StartNextUnitsTurn(Unit);
        }

        protected override void StartAction()
        {
            base.StartAction();
            Unit.unitActionHandler.SetIsAttacking(true);
            Unit.stats.UseEnergy(InitialEnergyCost());
            if (Unit.IsPlayer && TargetEnemyUnit != null)
                TargetEnemyUnit.unitActionHandler.NPCActionHandler.SetStartChaseGridPosition(TargetEnemyUnit.GridPosition);
        }

        protected void SetTargetEnemyUnit()
        {
            if (LevelGrid.HasUnitAtGridPosition(TargetGridPosition, out Unit unitAtGridPosition))
            {
                Unit.unitActionHandler.SetTargetEnemyUnit(unitAtGridPosition);
                TargetEnemyUnit = unitAtGridPosition;
            }
            else if (Unit.unitActionHandler.TargetEnemyUnit != null)
                TargetEnemyUnit = Unit.unitActionHandler.TargetEnemyUnit;
        }

        public virtual void DamageTarget(Unit targetUnit, HeldItem heldWeaponAttackingWith, ItemData itemDataHittingWith, HeldItem heldItemBlockedWith, bool headShot)
        {
            if (targetUnit != null && targetUnit.health.IsDead == false)
            {
                bool attackBlocked = false;
                targetUnit.unitActionHandler.InterruptActions();

                float damageAmount;
                if (heldWeaponAttackingWith != null)
                {
                    damageAmount = heldWeaponAttackingWith.ItemData.Damage;
                    if (Unit.UnitEquipment.IsDualWielding)
                    {
                        if (heldWeaponAttackingWith == Unit.unitMeshManager.GetPrimaryHeldMeleeWeapon())
                            damageAmount *= Weapon.dualWieldPrimaryEfficiency;
                        else
                            damageAmount *= Weapon.dualWieldSecondaryEfficiency;
                    }
                    else if (Unit.UnitEquipment.InVersatileStance)
                        damageAmount *= VersatileStanceAction.damageModifier;
                }
                else if (itemDataHittingWith != null)
                    damageAmount = itemDataHittingWith.Damage;
                else
                    damageAmount = UnarmedAttackDamage();

                // TODO: Calculate armor's damage absorbtion
                float armorAbsorbAmount = 0f;

                // If the attack was blocked
                if (heldItemBlockedWith != null)
                {
                    float blockAmount = 0f;
                    attackBlocked = true;
                    if (heldItemBlockedWith is HeldShield)
                    {
                        HeldShield shieldBlockedWith = heldItemBlockedWith as HeldShield;
                        blockAmount = targetUnit.stats.BlockPower(shieldBlockedWith);

                        // Play the recoil animation & then lower the shield if not in Raise Shield Stance
                        shieldBlockedWith.Recoil();

                        // Potentially fumble & drop the shield
                        shieldBlockedWith.TryFumbleHeldItem();
                    }
                    else if (heldItemBlockedWith is HeldMeleeWeapon)
                    {
                        HeldMeleeWeapon meleeWeaponBlockedWith = heldItemBlockedWith as HeldMeleeWeapon;
                        blockAmount = targetUnit.stats.BlockPower(meleeWeaponBlockedWith);

                        if (Unit.UnitEquipment.IsDualWielding)
                        {
                            if (meleeWeaponBlockedWith == Unit.unitMeshManager.GetRightHeldMeleeWeapon())
                                blockAmount *= Weapon.dualWieldPrimaryEfficiency;
                            else
                                blockAmount *= Weapon.dualWieldSecondaryEfficiency;
                        }

                        // Play the recoil animation & then lower the weapon
                        meleeWeaponBlockedWith.Recoil();

                        // Potentially fumble & drop the weapon
                        meleeWeaponBlockedWith.TryFumbleHeldItem();
                    }

                    damageAmount = damageAmount - armorAbsorbAmount - blockAmount;
                    targetUnit.health.TakeDamage(Mathf.RoundToInt(damageAmount), Unit);
                }
                else
                {
                    damageAmount -= armorAbsorbAmount;
                    targetUnit.health.TakeDamage(Mathf.RoundToInt(damageAmount), Unit);
                }

                // Don't try to knockback if the Unit died or they didn't take any damage due to armor absorbtion
                bool knockedBack = false;
                if ((damageAmount > 0f || attackBlocked) && !targetUnit.health.IsDead)
                {
                    knockedBack = Unit.stats.TryKnockback(targetUnit, heldWeaponAttackingWith, itemDataHittingWith, attackBlocked);
                    if (IsMeleeAttackAction())
                        targetUnit.health.OnHitByMeleeAttack();
                }

                if (heldWeaponAttackingWith != null && heldWeaponAttackingWith.CurrentHeldItemStance == HeldItemStance.SpearWall)
                {
                    SpearWallAction spearWallAction = Unit.unitActionHandler.GetAction<SpearWallAction>();
                    if (spearWallAction != null)
                    {
                        if (knockedBack)
                            spearWallAction.OnKnockback();
                        else
                        {
                            if (targetUnit.unitActionHandler.MoveAction.AboutToMove)
                                spearWallAction.OnFailedKnockback(targetUnit.unitActionHandler.MoveAction.NextTargetGridPosition);
                            else
                                spearWallAction.OnFailedKnockback(targetUnit.GridPosition);
                        }
                    }
                }
            }
        }

        public virtual void DamageTargets(HeldItem heldWeaponAttackingWith, ItemData itemDataHittingWith, bool headShot)
        {
            foreach (KeyValuePair<Unit, HeldItem> target in Unit.unitActionHandler.targetUnits)
            {
                Unit targetUnit = target.Key;
                HeldItem itemBlockedWith = target.Value;
                DamageTarget(targetUnit, heldWeaponAttackingWith, itemDataHittingWith, itemBlockedWith, headShot);
            }

            if (heldWeaponAttackingWith != null)
                heldWeaponAttackingWith.TryFumbleHeldItem();
        }

        public virtual IEnumerator WaitToDamageTargets(HeldItem heldWeaponAttackingWith, ItemData itemDataHittingWith)
        {
            // TODO: Come up with a headshot method
            bool headShot = false;

            if (heldWeaponAttackingWith != null)
                yield return new WaitForSeconds(AnimationTimes.Instance.DefaultWeaponAttackTime(heldWeaponAttackingWith.ItemData.Item as Weapon));
            else
                yield return new WaitForSeconds(AnimationTimes.Instance.UnarmedAttackTime() * 0.5f);

            DamageTargets(heldWeaponAttackingWith, itemDataHittingWith, headShot);
        }

        public virtual int UnarmedAttackDamage()
        {
            return Unit.stats.BaseUnarmedDamage;
        }

        protected virtual IEnumerator DoAttack()
        {
            // Get the Units within the attack position
            List<Unit> targetUnits = ListPool<Unit>.Claim(); 
            foreach (GridPosition gridPosition in GetActionAreaGridPositions(TargetGridPosition))
            {
                if (LevelGrid.HasUnitAtGridPosition(gridPosition, out Unit targetUnit) == false)
                    continue;

                targetUnits.Add(targetUnit);

                // The unit being attacked becomes aware of this unit
                Unit.vision.BecomeVisibleUnitOfTarget(targetUnit, targetUnit.unitActionHandler.targetUnits.Count == 1);
            }

            // We need to skip a frame in case the target Unit's meshes are being enabled due to becoming visible
            yield return null; 
            
            HeldMeleeWeapon primaryMeleeWeapon = Unit.unitMeshManager.GetPrimaryHeldMeleeWeapon();

            // If this is the Player attacking, or if this is an NPC that's visible on screen
            if (Unit.IsPlayer || (TargetEnemyUnit != null && TargetEnemyUnit.IsPlayer) || Unit.unitMeshManager.IsVisibleOnScreen || (TargetEnemyUnit != null && TargetEnemyUnit.unitMeshManager.IsVisibleOnScreen))
            {
                if (TargetEnemyUnit != null)
                {
                    while (TargetEnemyUnit.unitActionHandler.MoveAction.IsMoving || TargetEnemyUnit.unitAnimator.beingKnockedBack)
                        yield return null;

                    // If the target Unit moved out of range, queue a movement instead
                    if (IsInAttackRange(TargetEnemyUnit, Unit.GridPosition, TargetEnemyUnit.GridPosition) == false)
                    {
                        MoveToTargetInstead();
                        yield break;
                    }
                }

                // Rotate towards the target
                if (targetUnits.Count == 1) // If there's only 1 target Unit in the attack area, they mind as well just face that target
                {
                    if (Unit.unitActionHandler.TurnAction.IsFacingTarget(targetUnits[0].GridPosition) == false)
                        Unit.unitActionHandler.TurnAction.RotateTowardsPosition(targetUnits[0].transform.position, false);
                }
                else
                {
                    if (Unit.unitActionHandler.TurnAction.IsFacingTarget(TargetGridPosition) == false)
                        Unit.unitActionHandler.TurnAction.RotateTowardsPosition(TargetGridPosition.WorldPosition, false);
                }

                // Wait to finish any rotations already in progress
                while (Unit.unitActionHandler.TurnAction.isRotating)
                    yield return null;

                for (int i = 0; i < targetUnits.Count; i++)
                {
                    // The targetUnit tries to dodge, and if they fail that, they try to block instead
                    if (targetUnits[i].unitActionHandler.TryDodgeAttack(Unit, primaryMeleeWeapon, this, false))
                        targetUnits[i].unitAnimator.DoDodge(Unit, primaryMeleeWeapon, null);
                    else
                    {
                        // The targetUnit tries to block and if they're successful, the targetUnit and the weapon/shield they blocked with are added to the targetUnits dictionary
                        bool attackBlocked = targetUnits[i].unitActionHandler.TryBlockMeleeAttack(Unit, primaryMeleeWeapon, false);
                        Unit.unitActionHandler.targetUnits.TryGetValue(targetUnits[i], out HeldItem itemBlockedWith);

                        // If the target is successfully blocking the attack
                        if (attackBlocked && itemBlockedWith != null)
                            itemBlockedWith.BlockAttack(Unit);
                    }
                }

                Unit.StartCoroutine(WaitToDamageTargets(primaryMeleeWeapon, primaryMeleeWeapon.ItemData));

                // Play the attack animations and handle blocking for each target
                PlayAttackAnimation();
            }
            else // If this is an NPC who's outside of the screen, instantly damage the target without an animation
            {
                // Rotate towards the target
                if (Unit.unitActionHandler.TurnAction.IsFacingTarget(TargetGridPosition) == false)
                    Unit.unitActionHandler.TurnAction.RotateTowardsPosition(TargetGridPosition.WorldPosition, true);

                for (int i = 0; i < targetUnits.Count; i++)
                {
                    // The targetUnit tries to dodge, and if they fail that, they try to block instead
                    if (targetUnits[i].unitActionHandler.TryDodgeAttack(Unit, primaryMeleeWeapon, this, false) == false)
                    {
                        bool headShot = false;

                        // The targetUnit tries to block the attack and if they do, they face their attacker
                        if (targetUnits[i].unitActionHandler.TryBlockMeleeAttack(Unit, primaryMeleeWeapon, false))
                            targetUnits[i].unitActionHandler.TurnAction.RotateTowards_Unit(Unit, true);

                        // Damage this unit
                        DamageTargets(primaryMeleeWeapon, primaryMeleeWeapon.ItemData, headShot);
                    }
                }

                Unit.unitActionHandler.SetIsAttacking(false);
            }

            ListPool<Unit>.Release(targetUnits);

            // Wait until the attack lands before completing the action
            while (Unit.unitActionHandler.IsAttacking)
                yield return null;

            CompleteAction();
            TurnManager.Instance.StartNextUnitsTurn(Unit); // This must remain outside of CompleteAction in case we need to call CompletAction early within MoveToTargetInstead
        }

        public override void CompleteAction()
        {
            base.CompleteAction();

            Unit.unitActionHandler.targetUnits.Clear();
        }

        public bool OtherUnitInTheWay(Unit unit, GridPosition startGridPosition, GridPosition targetGridPosition)
        {
            Unit targetUnit = LevelGrid.GetUnitAtGridPosition(targetGridPosition);
            if (unit == null || targetUnit == null)
                return false;

            // Check if there's a Unit in the way of the attack
            float raycastDistance = Vector3.Distance(startGridPosition.WorldPosition, targetGridPosition.WorldPosition);
            Vector3 attackDir = (targetGridPosition.WorldPosition - startGridPosition.WorldPosition).normalized;
            Vector3 offset = 0.1f * Vector3.up;
            if (Physics.Raycast(startGridPosition.WorldPosition + offset, attackDir, out RaycastHit hit, raycastDistance, unit.vision.UnitsMask))
            {
                if (hit.collider.gameObject != unit.gameObject && hit.collider.gameObject != targetUnit.gameObject && unit.vision.IsVisible(hit.collider.gameObject))
                    return true;
            }
            return false;
        }

        public override List<GridPosition> GetActionAreaGridPositions(GridPosition targetGridPosition)
        {
            validGridPositionsList.Clear();
            if (!LevelGrid.IsValidGridPosition(targetGridPosition))
                return validGridPositionsList;

            if (!IsInAttackRange(null, Unit.GridPosition, targetGridPosition))
                return validGridPositionsList;

            float sphereCastRadius = 0.1f;
            float raycastDistance = Vector3.Distance(Unit.WorldPosition, targetGridPosition.WorldPosition);
            Vector3 offset = 2f * Unit.ShoulderHeight * Vector3.up;
            Vector3 attackDir = (Unit.WorldPosition - targetGridPosition.WorldPosition).normalized;
            if (Physics.SphereCast(targetGridPosition.WorldPosition + offset, sphereCastRadius, attackDir, out _, raycastDistance, Unit.unitActionHandler.AttackObstacleMask))
                return validGridPositionsList; // Blocked by an obstacle

            // Check if there's a Unit in the way of the attack
            if (!CanAttackThroughUnits() && OtherUnitInTheWay(Unit, Unit.GridPosition, targetGridPosition))
                return validGridPositionsList;

            validGridPositionsList.Add(targetGridPosition);
            return validGridPositionsList;
        }

        public override List<GridPosition> GetActionGridPositionsInRange(GridPosition startGridPosition)
        {
            float boundsDimension = (MaxAttackRange() * 2) + 0.1f;

            validGridPositionsList.Clear();
            List<GraphNode> nodes = ListPool<GraphNode>.Claim();
            nodes.AddRange(AstarPath.active.data.layerGridGraph.GetNodesInRegion(new Bounds(startGridPosition.WorldPosition, new Vector3(boundsDimension, boundsDimension, boundsDimension))));

            for (int i = 0; i < nodes.Count; i++)
            {
                GridPosition nodeGridPosition = new GridPosition((Vector3)nodes[i].position);

                if (LevelGrid.IsValidGridPosition(nodeGridPosition) == false)
                    continue;

                if (!IsInAttackRange(null, startGridPosition, nodeGridPosition))
                    continue;

                // Check for obstacles
                float sphereCastRadius = 0.1f;
                float raycastDistance = Vector3.Distance(Unit.WorldPosition, nodeGridPosition.WorldPosition);
                Vector3 offset = 2f * Unit.ShoulderHeight * Vector3.up;
                Vector3 attackDir = (nodeGridPosition.WorldPosition - startGridPosition.WorldPosition).normalized;
                if (Physics.SphereCast(startGridPosition.WorldPosition + offset, sphereCastRadius, attackDir, out _, raycastDistance, Unit.unitActionHandler.AttackObstacleMask))
                    continue;

                // Check if there's a Unit in the way of the attack (but only if the attack can't be performed through or over other Units)
                if (!CanAttackThroughUnits() && OtherUnitInTheWay(Unit, startGridPosition, nodeGridPosition))
                    continue;

                validGridPositionsList.Add(nodeGridPosition);
            }

            ListPool<GraphNode>.Release(nodes);
            return validGridPositionsList;
        }

        public virtual GridPosition GetNearestAttackPosition(GridPosition startGridPosition, Unit targetUnit)
        {
            nearestGridPositionsList.Clear();
            List<GridPosition> gridPositions = ListPool<GridPosition>.Claim();
            gridPositions.AddRange(GetValidGridPositionsInRange(targetUnit));
            float nearestDistance = 100000f;

            // First find the nearest valid Grid Positions to the Player
            for (int i = 0; i < gridPositions.Count; i++)
            {
                float distance = Vector3.Distance(gridPositions[i].WorldPosition, startGridPosition.WorldPosition);
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
            float nearestDistanceToTarget = 100000f;
            for (int i = 0; i < nearestGridPositionsList.Count; i++)
            {
                // Get the Grid Position that is closest to the target Grid Position
                float distance = Vector3.Distance(nearestGridPositionsList[i].WorldPosition, targetUnit.WorldPosition);
                if (distance < nearestDistanceToTarget)
                {
                    nearestDistanceToTarget = distance;
                    nearestGridPosition = nearestGridPositionsList[i];
                }
            }

            ListPool<GridPosition>.Release(gridPositions);
            return nearestGridPosition;
        }

        protected List<GridPosition> GetValidGridPositionsInRange(Unit targetUnit)
        {
            validGridPositionsList.Clear();
            if (targetUnit == null)
                return validGridPositionsList;

            float boundsDimension = (MaxAttackRange() * 2) + 0.1f;
            List<GraphNode> nodes = ListPool<GraphNode>.Claim();
            nodes.AddRange(AstarPath.active.data.layerGridGraph.GetNodesInRegion(new Bounds(targetUnit.WorldPosition, new Vector3(boundsDimension, boundsDimension, boundsDimension))));

            for (int i = 0; i < nodes.Count; i++)
            {
                GridPosition nodeGridPosition = new GridPosition((Vector3)nodes[i].position);

                if (LevelGrid.IsValidGridPosition(nodeGridPosition) == false)
                    continue;

                // If Grid Position has a Unit there already
                if (LevelGrid.HasUnitAtGridPosition(nodeGridPosition, out _))
                    continue;

                // If target is out of attack range from this Grid Position
                if (IsInAttackRange(null, nodeGridPosition, targetUnit.GridPosition) == false)
                    continue;

                // Check for obstacles
                float sphereCastRadius = 0.1f;
                Vector3 unitOffset = 2f * Unit.ShoulderHeight * Vector3.up;
                Vector3 targetUnitOffset = 2f * targetUnit.ShoulderHeight * Vector3.up;
                float raycastDistance = Vector3.Distance(nodeGridPosition.WorldPosition + unitOffset, targetUnit.WorldPosition + targetUnitOffset);
                Vector3 attackDir = (nodeGridPosition.WorldPosition + unitOffset - (targetUnit.WorldPosition + targetUnitOffset)).normalized;
                if (Physics.SphereCast(targetUnit.WorldPosition + targetUnitOffset, sphereCastRadius, attackDir, out _, raycastDistance, Unit.unitActionHandler.AttackObstacleMask))
                    continue;

                if (!CanAttackThroughUnits() && OtherUnitInTheWay(Unit, nodeGridPosition, targetUnit.GridPosition))
                    continue;

                validGridPositionsList.Add(nodeGridPosition);
            }

            ListPool<GraphNode>.Release(nodes);
            return validGridPositionsList;
        }

        public override bool IsValidUnitInActionArea(GridPosition targetGridPosition)
        {
            List<GridPosition> attackGridPositions = ListPool<GridPosition>.Claim();
            attackGridPositions.AddRange(GetActionAreaGridPositions(targetGridPosition));
            for (int i = 0; i < attackGridPositions.Count; i++)
            {
                if (LevelGrid.HasUnitAtGridPosition(attackGridPositions[i], out Unit unitAtGridPosition) == false)
                    continue;

                if (unitAtGridPosition.health.IsDead)
                    continue;

                if (Unit.alliance.IsAlly(unitAtGridPosition))
                    continue;

                if (Unit.vision.IsDirectlyVisible(unitAtGridPosition) == false)
                    continue;

                // If the loop makes it to this point, then it found a valid unit
                ListPool<GridPosition>.Release(attackGridPositions);
                return true;
            }

            ListPool<GridPosition>.Release(attackGridPositions);
            return false;
        }

        protected float ActionPointCostModifier_WeaponType(Weapon weapon)
        {
            if (weapon == null) // Unarmed
                return 0.5f;

            switch (weapon.WeaponType)
            {
                case WeaponType.Bow:
                    return 1f;
                case WeaponType.Crossbow:
                    return 0.2f;
                case WeaponType.ThrowingWeapon:
                    return 0.6f;
                case WeaponType.Dagger:
                    return 0.55f;
                case WeaponType.Sword:
                    return 1f;
                case WeaponType.Axe:
                    return 1.35f;
                case WeaponType.Mace:
                    return 1.3f;
                case WeaponType.WarHammer:
                    return 1.5f;
                case WeaponType.Spear:
                    return 0.85f;
                case WeaponType.Polearm:
                    return 1.25f;
                default:
                    Debug.LogError(weapon.WeaponType.ToString() + " has not been implemented in this method. Fix me!");
                    return 1f;
            }
        }

        public virtual bool IsInAttackRange(Unit targetUnit, GridPosition startGridPosition, GridPosition targetGridPosition)
        {
            // Check for obstacles
            if (targetUnit != null)
            {
                if (!Unit.vision.IsInLineOfSight_SphereCast(targetUnit))
                    return false;
            }
            else if (!Unit.vision.IsInLineOfSight_SphereCast(targetGridPosition))
                return false;

            // Check if there's a Unit in the way of the attack (but only for actions that can't attack through or over other Units)
            if (!CanAttackThroughUnits() && OtherUnitInTheWay(Unit, startGridPosition, targetGridPosition))
                return false;

            float distance = Vector3.Distance(startGridPosition.WorldPosition, targetGridPosition.WorldPosition);
            if (distance < MinAttackRange() || distance > MaxAttackRange())
                return false;
            return true;
        }

        public virtual bool IsInAttackRange(Unit targetUnit)
        {
            if (targetUnit == null)
                return IsInAttackRange(null, Unit.GridPosition, TargetGridPosition);
            else
                return IsInAttackRange(targetUnit, Unit.GridPosition, targetUnit.GridPosition);
        }

        public abstract float MinAttackRange();
        public abstract float MaxAttackRange();

        /// <summary>A weapon's accuracy will be multiplied by this amount at the end of the calculation, rather than directly added or subtracted. Affects Ranged Accuracy & Dodge Chance.</summary>
        public abstract float AccuracyModifier();

        public abstract bool CanAttackThroughUnits();

        public abstract bool IsMeleeAttackAction();

        public abstract bool IsRangedAttackAction();

        public abstract void PlayAttackAnimation();
    }
}
