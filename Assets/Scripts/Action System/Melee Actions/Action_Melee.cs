using System.Collections;
using UnityEngine;
using GridSystem;
using InventorySystem;
using UnitSystem.ActionSystem.UI;
using Utilities;

namespace UnitSystem.ActionSystem.Actions
{
    public class Action_Melee : Action_BaseAttack
    {
        readonly int baseAPCost = 300;

        public override void SetTargetGridPosition(GridPosition gridPosition)
        {
            base.SetTargetGridPosition(gridPosition);
            SetTargetEnemyUnit();
        }

        public override int ActionPointsCost()
        {
            float cost;
            if (Unit.UnitEquipment != null && Unit.UnitEquipment is UnitEquipment_Humanoid)
            {
                Unit.UnitEquipment.HumanoidEquipment.GetEquippedWeapons(out Item_Weapon primaryWeapon, out Item_Weapon secondaryWeapon);
                if (primaryWeapon != null)
                {
                    if (secondaryWeapon != null)
                    {
                        cost = baseAPCost / 2f * ActionPointCostModifier_WeaponType(primaryWeapon);
                        cost += baseAPCost / 2f * ActionPointCostModifier_WeaponType(secondaryWeapon);
                    }
                    else
                        cost = baseAPCost * ActionPointCostModifier_WeaponType(primaryWeapon);
                }
                else
                    cost = baseAPCost * ActionPointCostModifier_WeaponType(null);

                if (Unit.UnitEquipment.HumanoidEquipment.InVersatileStance)
                    cost *= Action_VersatileStance.APCostModifier;
            }
            else
                cost = baseAPCost * ActionPointCostModifier_WeaponType(null);

            // If not facing the target position, add the cost of turning towards that position
            Unit.UnitActionHandler.TurnAction.DetermineTargetTurnDirection(TargetGridPosition);
            cost += Unit.UnitActionHandler.TurnAction.ActionPointsCost();
            return Mathf.RoundToInt(cost);
        }

        public override void TakeAction()
        {
            if (Unit.UnitActionHandler.IsAttacking) return;

            if (TargetEnemyUnit == null)
                SetTargetEnemyUnit();

            if (TargetEnemyUnit == null || TargetEnemyUnit.HealthSystem.IsDead)
            {
                TargetEnemyUnit = null;
                Unit.UnitActionHandler.SetTargetEnemyUnit(null);
                Unit.UnitActionHandler.FinishAction();
                return;
            }

            if (IsInAttackRange(TargetEnemyUnit, Unit.GridPosition, TargetEnemyUnit.GridPosition))
            {
                StartAction();
                Unit.StartCoroutine(DoAttack());
            }
            else
                MoveToTargetInstead();
        }

        public void DoOpportunityAttack(Unit targetEnemyUnit)
        {
            // Unit's can't opportunity attack while moving
            if (Unit.UnitActionHandler.MoveAction.IsMoving)
                return;

            TargetEnemyUnit = targetEnemyUnit;
            Unit.UnitActionHandler.SetIsAttacking(true);
            Unit.StartCoroutine(Attack());
        }
        
        void AttackTarget(HeldItem heldWeaponAttackingWith, ItemData weaponItemData, bool isUsingOffhandWeapon)
        {
            bool attackDodged = TargetEnemyUnit.UnitActionHandler.TryDodgeAttack(Unit, heldWeaponAttackingWith, this, isUsingOffhandWeapon);
            if (attackDodged)
                TargetEnemyUnit.UnitAnimator.DoDodge(Unit, heldWeaponAttackingWith, null);
            else
            {
                // The targetUnit tries to block and if they're successful, the weapon/shield they blocked with is added as a corresponding Value in the attacking Unit's targetUnits dictionary
                bool attackBlocked = TargetEnemyUnit.UnitActionHandler.TryBlockMeleeAttack(Unit, heldWeaponAttackingWith, this, isUsingOffhandWeapon);
                Unit.UnitActionHandler.TargetUnits.TryGetValue(TargetEnemyUnit, out HeldItem itemBlockedWith);

                if (attackBlocked && itemBlockedWith != null)
                    itemBlockedWith.BlockAttack(Unit);
            }

            Unit.StartCoroutine(WaitToDamageTargets(heldWeaponAttackingWith, weaponItemData));
        }

        protected override IEnumerator DoAttack()
        {
            while (TargetEnemyUnit.UnitActionHandler.MoveAction.IsMoving || TargetEnemyUnit.UnitAnimator.beingKnockedBack)
                yield return null;

            // If the target Unit moved out of range, queue a movement instead
            if (IsInAttackRange(TargetEnemyUnit, Unit.GridPosition, TargetEnemyUnit.GridPosition) == false)
            {
                MoveToTargetInstead();
                yield break;
            }

            StartCoroutine(Attack());

            // Wait until the attack lands before completing the action
            while (Unit.UnitActionHandler.IsAttacking)
                yield return null;

            CompleteAction();
            TurnManager.Instance.StartNextUnitsTurn(Unit); // This must remain outside of CompleteAction in case we need to call CompleteAction early within MoveToTargetInstead
        }

        IEnumerator Attack()
        {
            // The unit being attacked becomes aware of this unit
            Unit.Vision.BecomeVisibleUnitOfTarget(TargetEnemyUnit, true);

            // We need to skip a frame in case the target Unit's meshes are being enabled
            yield return null;
            
            // If this is the Player attacking, or if this is an NPC that's visible on screen
            if (Unit.IsPlayer || TargetEnemyUnit.IsPlayer || Unit.UnitMeshManager.IsVisibleOnScreen || TargetEnemyUnit.UnitMeshManager.IsVisibleOnScreen)
            {
                // Rotate towards the target
                if (Unit.UnitActionHandler.TurnAction.IsFacingTarget(TargetEnemyUnit.GridPosition) == false)
                    Unit.UnitActionHandler.TurnAction.RotateTowards_Unit(TargetEnemyUnit, false);

                // Wait to finish any rotations already in progress
                while (Unit.UnitActionHandler.TurnAction.isRotating)
                    yield return null;
                
                // Play the attack animations and handle blocking for the target
                if (Unit.UnitEquipment.IsUnarmed || Unit.UnitEquipment.RangedWeaponEquipped)
                {
                    Unit.UnitAnimator.DoDefaultUnarmedAttack();
                    AttackTarget(null, null, false);
                }
                else if (Unit.UnitEquipment.IsDualWielding)
                {
                    float distanceToTarget = Vector3.Distance(Unit.WorldPosition, TargetEnemyUnit.WorldPosition);
                    HeldMeleeWeapon primaryHeldWeapon = Unit.UnitMeshManager.GetPrimaryHeldMeleeWeapon();
                    HeldMeleeWeapon secondaryHeldWeapon = Unit.UnitMeshManager.GetLeftHeldMeleeWeapon();
                    bool primaryWeaponInRange = primaryHeldWeapon.ItemData.Item.Weapon.MaxRange >= distanceToTarget && primaryHeldWeapon.ItemData.Item.Weapon.MinRange <= distanceToTarget;
                    bool secondaryWeaponInRange = secondaryHeldWeapon.ItemData.Item.Weapon.MaxRange >= distanceToTarget && secondaryHeldWeapon.ItemData.Item.Weapon.MinRange <= distanceToTarget;

                    if (primaryWeaponInRange && secondaryWeaponInRange) // Dual wield attack
                    {
                        Unit.UnitAnimator.StartDualMeleeAttack();
                        primaryHeldWeapon.DoDefaultAttack(TargetGridPosition);
                        AttackTarget(primaryHeldWeapon, primaryHeldWeapon.ItemData, false);

                        yield return new WaitForSeconds((AnimationTimes.Instance.DefaultWeaponAttackTime(primaryHeldWeapon.ItemData.Item as Item_Weapon) / 2f) + 0.05f);

                        secondaryHeldWeapon.DoDefaultAttack(TargetGridPosition);
                        AttackTarget(secondaryHeldWeapon, secondaryHeldWeapon.ItemData, true);
                    }
                    else
                    {
                        Unit.UnitAnimator.StartMeleeAttack();

                        if (primaryWeaponInRange) // Primary weapon only attack
                        {
                            primaryHeldWeapon.DoDefaultAttack(TargetGridPosition);
                            AttackTarget(primaryHeldWeapon, primaryHeldWeapon.ItemData, false);
                        }
                        else // Secondary weapon only attack
                        {
                            secondaryHeldWeapon.DoDefaultAttack(TargetGridPosition);
                            AttackTarget(secondaryHeldWeapon, secondaryHeldWeapon.ItemData, false);
                        }
                    }
                }
                else
                {
                    // Primary weapon attack
                    Unit.UnitAnimator.StartMeleeAttack();
                    if (Unit.UnitEquipment.MeleeWeaponEquipped)
                    {
                        HeldMeleeWeapon primaryMeleeWeapon = Unit.UnitMeshManager.GetPrimaryHeldMeleeWeapon();
                        primaryMeleeWeapon.DoDefaultAttack(TargetGridPosition);
                        AttackTarget(primaryMeleeWeapon, primaryMeleeWeapon.ItemData, false);
                    }
                    else // Fallback to unarmed attack
                    {
                        Unit.UnitAnimator.DoDefaultUnarmedAttack();
                        AttackTarget(null, null, false);
                    }
                }
            }
            else // If this is an NPC who's outside of the screen, instantly damage the target without an animation
            {
                // Try to dodge or block the attack
                bool attackDodged = false;
                bool attackBlocked = false;
                if (Unit.UnitEquipment.IsUnarmed || Unit.UnitEquipment.RangedWeaponEquipped) // Unarmed or has a ranged weapon equipped, but no ammo
                {
                    attackDodged = TargetEnemyUnit.UnitActionHandler.TryDodgeAttack(Unit, null, this, false);
                    if (attackDodged == false)
                    {
                        attackBlocked = TargetEnemyUnit.UnitActionHandler.TryBlockMeleeAttack(Unit, null, this, false);
                        DamageTargets(null, null);
                    }
                }
                else if (Unit.UnitEquipment.IsDualWielding) // Dual wield attack
                {
                    float distanceToTarget = Vector3.Distance(Unit.WorldPosition, TargetEnemyUnit.WorldPosition);
                    HeldMeleeWeapon primaryHeldWeapon = Unit.UnitMeshManager.GetPrimaryHeldMeleeWeapon();
                    HeldMeleeWeapon secondaryHeldWeapon = Unit.UnitMeshManager.GetLeftHeldMeleeWeapon();
                    bool primaryWeaponInRange = primaryHeldWeapon.ItemData.Item.Weapon.MaxRange >= distanceToTarget && primaryHeldWeapon.ItemData.Item.Weapon.MinRange <= distanceToTarget;
                    bool secondaryWeaponInRange = secondaryHeldWeapon.ItemData.Item.Weapon.MaxRange >= distanceToTarget && secondaryHeldWeapon.ItemData.Item.Weapon.MinRange <= distanceToTarget;
                    
                    bool mainAttackDodged = false;
                    bool mainAttackBlocked = false;
                    if (primaryWeaponInRange)
                    {
                        mainAttackDodged = TargetEnemyUnit.UnitActionHandler.TryDodgeAttack(Unit, Unit.UnitMeshManager.rightHeldItem, this, false);
                        if (mainAttackDodged == false)
                        {
                            mainAttackBlocked = TargetEnemyUnit.UnitActionHandler.TryBlockMeleeAttack(Unit, Unit.UnitMeshManager.rightHeldItem, this, false);
                            DamageTargets(Unit.UnitMeshManager.rightHeldItem as HeldMeleeWeapon, Unit.UnitMeshManager.rightHeldItem.ItemData);
                        }
                    }

                    bool offhandAttackDodged = false;
                    bool offhandAttackBlocked = false;
                    if (secondaryWeaponInRange)
                    {
                        offhandAttackDodged = TargetEnemyUnit.UnitActionHandler.TryDodgeAttack(Unit, Unit.UnitMeshManager.leftHeldItem, this, true);
                        if (offhandAttackDodged == false)
                        {
                            offhandAttackBlocked = TargetEnemyUnit.UnitActionHandler.TryBlockMeleeAttack(Unit, Unit.UnitMeshManager.leftHeldItem, this, true);
                            DamageTargets(Unit.UnitMeshManager.leftHeldItem as HeldMeleeWeapon, Unit.UnitMeshManager.leftHeldItem.ItemData);
                        }
                    }

                    if (mainAttackDodged || offhandAttackDodged)
                        attackDodged = true;

                    if (mainAttackBlocked || offhandAttackBlocked)
                        attackBlocked = true;
                }
                else
                {
                    HeldMeleeWeapon primaryMeleeWeapon = Unit.UnitMeshManager.GetPrimaryHeldMeleeWeapon();
                    attackDodged = TargetEnemyUnit.UnitActionHandler.TryDodgeAttack(Unit, primaryMeleeWeapon, this, false);
                    if (attackDodged == false)
                    {
                        attackBlocked = TargetEnemyUnit.UnitActionHandler.TryBlockMeleeAttack(Unit, primaryMeleeWeapon, this, false);
                        if (primaryMeleeWeapon != null)
                            DamageTargets(primaryMeleeWeapon, primaryMeleeWeapon.ItemData); // Right hand weapon attack
                        else
                            DamageTargets(null, null); // Fallback to unarmed damage
                    }
                }

                // Rotate towards the target
                if (Unit.UnitActionHandler.TurnAction.IsFacingTarget(TargetEnemyUnit.GridPosition) == false)
                    Unit.UnitActionHandler.TurnAction.RotateTowards_Unit(TargetEnemyUnit, false);

                // If the attack was dodged or blocked and the defending unit isn't facing their attacker, turn to face the attacker
                if (attackDodged || attackBlocked)
                    TargetEnemyUnit.UnitActionHandler.TurnAction.RotateTowards_Unit(Unit, true);

                Unit.UnitActionHandler.SetIsAttacking(false);
            }
        }

        public override void PlayAttackAnimation() { }

        public override void CompleteAction()
        {
            base.CompleteAction();
            
            if (Unit.IsPlayer)
            {
                Unit.UnitActionHandler.PlayerActionHandler.SetDefaultSelectedAction();
                if (PlayerInput.Instance.AutoAttack == false)
                {
                    TargetEnemyUnit = null;
                    Unit.UnitActionHandler.SetTargetEnemyUnit(null);
                }
            }

            Unit.UnitActionHandler.FinishAction();
        }

        public override NPCAIAction GetNPCAIAction_Unit(Unit targetUnit)
        {
            float finalActionValue = 0f;
            if (IsValidAction() && targetUnit != null && targetUnit.HealthSystem.IsDead == false)
            {
                // Target the Unit with the lowest health and/or the nearest target
                finalActionValue += 500 - (targetUnit.HealthSystem.GetBodyPart(BodyPartType.Torso).CurrentHealthNormalized * 100f);
                float distance = Vector3.Distance(Unit.WorldPosition, targetUnit.WorldPosition);
                float minAttackRange = 1f;
                if (Unit.UnitEquipment.IsDualWielding)
                    minAttackRange = Mathf.Min(Unit.UnitMeshManager.GetPrimaryHeldMeleeWeapon().ItemData.Item.Weapon.MinRange, Unit.UnitMeshManager.GetLeftHeldMeleeWeapon().ItemData.Item.Weapon.MinRange);
                else if (Unit.UnitEquipment.MeleeWeaponEquipped)
                    minAttackRange = Unit.UnitMeshManager.GetPrimaryHeldMeleeWeapon().ItemData.Item.Weapon.MinRange;

                if (distance < minAttackRange)
                    finalActionValue = 0f;
                else
                    finalActionValue -= distance * 10f;

                return new NPCAIAction
                {
                    baseAction = this,
                    actionGridPosition = targetUnit.GridPosition,
                    actionValue = Mathf.RoundToInt(finalActionValue)
                };
            }

            return new NPCAIAction
            {
                baseAction = this,
                actionGridPosition = Unit.GridPosition,
                actionValue = -1
            };
        }

        public override NPCAIAction GetNPCAIAction_ActionGridPosition(GridPosition actionGridPosition)
        {
            float finalActionValue = 0f;

            // Make sure there's a Unit at this grid position
            if (LevelGrid.HasUnitAtGridPosition(actionGridPosition, out Unit unitAtGridPosition))
            {
                // Adjust the finalActionValue based on the Alliance of the unit at the grid position
                if (unitAtGridPosition.HealthSystem.IsDead == false && Unit.Alliance.IsEnemy(unitAtGridPosition))
                {
                    // Enemies in the action area increase this action's value
                    finalActionValue += 70f;

                    // Lower enemy health gives this action more value
                    finalActionValue += 70f - (unitAtGridPosition.HealthSystem.GetBodyPart(BodyPartType.Torso).CurrentHealthNormalized * 70f);

                    // Favor the targetEnemyUnit
                    if (Unit.UnitActionHandler.TargetEnemyUnit != null && unitAtGridPosition == Unit.UnitActionHandler.TargetEnemyUnit)
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
                actionGridPosition = Unit.GridPosition,
                actionValue = -1
            };
        }

        public override bool IsValidUnitInActionArea(GridPosition targetGridPosition)
        {
            Unit unitAtGridPosition = LevelGrid.GetUnitAtGridPosition(targetGridPosition);
            if (unitAtGridPosition != null && !unitAtGridPosition.HealthSystem.IsDead && !Unit.Alliance.IsAlly(unitAtGridPosition) && Unit.Vision.IsDirectlyVisible(unitAtGridPosition))
                return true;
            return false;
        }

        public override bool IsValidAction()
        {
            if (Unit != null && (Unit.UnitEquipment.MeleeWeaponEquipped || Unit.Stats.CanFightUnarmed))
                return true;
            return false;
        }

        public override string TooltipDescription()
        {
            if (Unit.UnitEquipment.IsUnarmed || Unit.UnitEquipment.RangedWeaponEquipped)
                return "Engage in <b>hand-to-hand</b> combat, delivering a swift and powerful strike to your target.";
            else if (Unit.UnitEquipment.IsDualWielding)
                return $"Deliver coordinated strikes with your <b>{Unit.UnitMeshManager.rightHeldItem.ItemData.Item.Name}</b> and <b>{Unit.UnitMeshManager.leftHeldItem.ItemData.Item.Name}</b>.";
            else
                return $"Deliver a decisive strike to your target using your <b>{Unit.UnitMeshManager.GetPrimaryHeldMeleeWeapon().ItemData.Item.Name}</b>.";
        }

        public override float AccuracyModifier() => 1f;

        public override int EnergyCost() => 0;

        public override bool IsInterruptable() => false;

        public override bool CanQueueMultiple() => false;

        public override ActionBarSection ActionBarSection() => UI.ActionBarSection.Special;

        public override bool CanBeClearedFromActionQueue() => true;

        public override bool CanShowAttackRange() => true;

        public override bool CanAttackThroughUnits() => false;

        public override bool IsMeleeAttackAction() => true;

        public override bool IsRangedAttackAction() => false;

        public override bool ActionIsUsedInstantly() => false;

        public override float MinAttackRange()
        {
            if (Unit.UnitEquipment == null || Unit.UnitEquipment.IsUnarmed || Unit.UnitEquipment.RangedWeaponEquipped)
                return 1f;
            else
            {
                HeldMeleeWeapon primaryHeldWeapon = Unit.UnitMeshManager.GetPrimaryHeldMeleeWeapon();
                if (Unit.UnitEquipment.IsDualWielding)
                {
                    HeldMeleeWeapon secondaryHeldWeapon = Unit.UnitMeshManager.GetLeftHeldMeleeWeapon();
                    return Mathf.Min(primaryHeldWeapon.ItemData.Item.Weapon.MinRange, secondaryHeldWeapon.ItemData.Item.Weapon.MinRange);
                }
                else if (primaryHeldWeapon != null)
                    return primaryHeldWeapon.ItemData.Item.Weapon.MinRange;
                else
                    return 1f;
            }
        }

        public override float MaxAttackRange()
        {
            if (Unit.UnitEquipment == null || Unit.UnitEquipment.IsUnarmed || Unit.UnitEquipment.RangedWeaponEquipped)
                return Unit.Stats.UnarmedAttackRange;
            else
            {
                HeldMeleeWeapon primaryHeldWeapon = Unit.UnitMeshManager.GetPrimaryHeldMeleeWeapon();
                if (Unit.UnitEquipment.IsDualWielding)
                {
                    HeldMeleeWeapon secondaryHeldWeapon = Unit.UnitMeshManager.GetLeftHeldMeleeWeapon();
                    return Mathf.Max(primaryHeldWeapon.ItemData.Item.Weapon.MaxRange, secondaryHeldWeapon.ItemData.Item.Weapon.MaxRange);
                }
                else if (primaryHeldWeapon != null)
                    return primaryHeldWeapon.ItemData.Item.Weapon.MaxRange;
                else
                    return Unit.Stats.UnarmedAttackRange;
            }
        }
    }
}
