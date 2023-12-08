using GridSystem;
using InteractableObjects;
using InventorySystem;
using System;
using System.Collections.Generic;
using UnitSystem.ActionSystem.Actions;
using UnitSystem.ActionSystem.GOAP.Goals;
using UnityEngine;

namespace UnitSystem.ActionSystem.GOAP.GoalActions
{
    public class GoalAction_FindWeapon : GoalAction_Base
    {
        readonly List<Type> supportedGoals = new(new Type[] { typeof(Goal_FindWeapon) });

        public override List<Type> SupportedGoals() => supportedGoals;

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
            if (unit.UnitEquipment.OtherWeaponSet_IsEmpty())
            {
                // Equip any weapon from their inventory if they have one
                if (unit.UnitInventoryManager.ContainsMeleeWeaponInAnyInventory(out ItemData weaponItemData))
                    npcActionHandler.GetAction<EquipAction>().QueueAction(weaponItemData, weaponItemData.Item.Equipment.EquipSlot, null);
                // Else, try pickup the nearby weapon
                else if (weaponNearby)
                    npcActionHandler.InteractAction.QueueAction(foundLooseWeapon);
            }
            else // If there are weapons in the other weapon set
            {
                // Swap to their melee weapon set if they have one
                if (unit.UnitEquipment.OtherWeaponSet_IsMelee())
                    npcActionHandler.GetAction<SwapWeaponSetAction>().QueueAction();
                // Else, swap to their ranged weapon set if they have ammo
                else if (unit.UnitEquipment.OtherWeaponSet_IsRanged() && unit.UnitEquipment.HasValidAmmunitionEquipped(unit.UnitEquipment.GetRangedWeaponFromOtherWeaponSet().Item as RangedWeapon))
                    npcActionHandler.GetAction<SwapWeaponSetAction>().QueueAction();
            }
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
