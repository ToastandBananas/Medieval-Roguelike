using GridSystem;
using InteractableObjects;
using InventorySystem;
using UnitSystem.ActionSystem.Actions;
using UnityEngine;

namespace UnitSystem.ActionSystem.GOAP.GoalActions
{
    public class GoalAction_FindWeapon : GoalAction_Base
    {
        GoalAction_Fight fightAction;

        void Start()
        {
            fightAction = (GoalAction_Fight)npcActionHandler.GoalPlanner.GetGoalAction(typeof(GoalAction_Fight));
        }

        public override float Cost()
        {
            if (!unit.UnitEquipment.IsUnarmed)
                return 100f;

            if (unit.Stats.CanFightUnarmed)
                return 45f + (unit.Stats.UnarmedSkill.GetValue() / 10f);

            return 100f;
        }

        public override MoveMode PreferredMoveMode() => MoveMode.Run;

        public override void OnTick()
        {
            FindWeapon();
        }

        void FindWeapon()
        {
            bool weaponNearby = TryFindNearbyWeapon(out LooseItem foundLooseWeapon, out float distanceToWeapon);

            // If a weapon was found and it's next to this Unit, pick it up (the weapon is likely there from fumbling it)
            if (weaponNearby && distanceToWeapon <= LevelGrid.diaganolDistance)
            {
                npcActionHandler.InteractAction.QueueAction(foundLooseWeapon);
                return;
            }

            // If no weapons whatsoever are equipped
            if (!unit.UnitEquipment.OtherWeaponSet_IsEmpty() && !unit.UnitEquipment.OtherWeaponSet_IsRanged())
            {
                // Equip any weapon from their inventory if they have one
                if (unit.UnitInventoryManager.ContainsMeleeWeaponInAnyInventory(out ItemData weaponItemData))
                {
                    npcActionHandler.GetAction<Action_Equip>().QueueAction(weaponItemData, weaponItemData.Item.Equipment.EquipSlot, null);
                    return;
                }
                // Else, try pickup the nearby weapon
                else if (weaponNearby)
                {
                    npcActionHandler.InteractAction.QueueAction(foundLooseWeapon);
                    return;
                }
            }
            else // If there are weapons in the other weapon set
            {
                float distanceToTargetEnemy = 0f;
                if (unit.UnitActionHandler.TargetEnemyUnit != null)
                    distanceToTargetEnemy = Vector3.Distance(unit.WorldPosition, unit.UnitActionHandler.TargetEnemyUnit.WorldPosition);

                // Swap to their melee weapon set if they have one
                if (distanceToTargetEnemy <= npcActionHandler.GoalPlanner.FightAction.DistanceToPreferMeleeCombat)
                {
                    if (unit.UnitEquipment.OtherWeaponSet_IsMelee())
                    {
                        npcActionHandler.GetAction<Action_SwapWeaponSet>().QueueAction();
                        return;
                    }
                    else if (unit.UnitInventoryManager.ContainsMeleeWeaponInAnyInventory(out ItemData weaponItemData))
                    {
                        npcActionHandler.GetAction<Action_Equip>().QueueAction(weaponItemData, weaponItemData.Item.Equipment.EquipSlot, null);
                        return;
                    }
                    else if (weaponNearby)
                    {
                        npcActionHandler.InteractAction.QueueAction(foundLooseWeapon);
                        return;
                    }
                }
                // Else, swap to their ranged weapon set if they have ammo
                else if (unit.UnitEquipment.OtherWeaponSet_IsRanged() && unit.UnitEquipment.HasValidAmmunitionEquipped(unit.UnitEquipment.GetRangedWeaponFromOtherWeaponSet().Item as RangedWeapon))
                {
                    npcActionHandler.GetAction<Action_SwapWeaponSet>().QueueAction();
                    return;
                }
                else if (unit.UnitInventoryManager.ContainsMeleeWeaponInAnyInventory(out ItemData weaponItemData))
                {
                    npcActionHandler.GetAction<Action_Equip>().QueueAction(weaponItemData, weaponItemData.Item.Equipment.EquipSlot, null);
                    return;
                }
                else if (weaponNearby)
                {
                    npcActionHandler.InteractAction.QueueAction(foundLooseWeapon);
                    return;
                }
            }

            // Failed to find a weapon, so just fight or skip turn
            if (fightAction != null)
                fightAction.OnTick();
            else
                TurnManager.Instance.FinishTurn(unit);
        }

        public bool TryFindNearbyWeapon(out LooseItem foundLooseWeapon, out float distanceToWeapon)
        {
            LooseItem closestLooseWeapon = unit.Vision.GetClosestWeapon(out float distanceToClosestWeapon);
            if (closestLooseWeapon == null)
            {
                foundLooseWeapon = null;
                distanceToWeapon = distanceToClosestWeapon;
                return false;
            }

            if (closestLooseWeapon.ItemData.Item is RangedWeapon)
            {
                if (unit.UnitEquipment.HasValidAmmunitionEquipped(closestLooseWeapon.ItemData.Item.RangedWeapon))
                {
                    foundLooseWeapon = closestLooseWeapon;
                    distanceToWeapon = distanceToClosestWeapon;
                    return true;
                }
                else
                {
                    LooseItem closestLooseMeleeWeapon = unit.Vision.GetClosestMeleeWeapon(out float distanceToClosestMeleeWeapon);
                    if (closestLooseMeleeWeapon != null)
                    {
                        foundLooseWeapon = closestLooseMeleeWeapon;
                        distanceToWeapon = distanceToClosestMeleeWeapon;
                        return true;
                    }
                }

            }

            foundLooseWeapon = closestLooseWeapon;
            distanceToWeapon = distanceToClosestWeapon;
            return true;
        }
    }
}
