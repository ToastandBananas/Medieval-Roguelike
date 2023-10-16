using System.Collections;
using UnityEngine;
using GridSystem;
using ActionSystem;
using UnitSystem;

namespace InventorySystem
{
    public class HeldMeleeWeapon : HeldItem
    {
        public bool weaponRaised { get; private set; }

        public override void DoDefaultAttack()
        {
            if (anim == null)
                return;

            Unit targetUnit = unit.unitActionHandler.targetEnemyUnit;
            HeldItem itemBlockedWith;

            // The targetUnit tries to block and if they're successful, the targetUnit and the weapon/shield they blocked with are added to the targetUnits dictionary
            bool attackBlocked = targetUnit.unitActionHandler.TryBlockMeleeAttack(unit);
            unit.unitActionHandler.targetUnits.TryGetValue(targetUnit, out itemBlockedWith);

            // If the target is successfully blocking the attack
            if (attackBlocked)
                BlockAttack(targetUnit, itemBlockedWith);

            // Determine attack animation based on melee weapon type
            if (this == unit.unitMeshManager.rightHeldItem)
            {
                if (itemData.Item.Weapon.IsTwoHanded)
                    anim.Play("DefaultAttack_2H");
                else
                    anim.Play("DefaultAttack_1H_R");

                if (unit.unitMeshManager.leftHeldItem != null && unit.unitMeshManager.leftHeldItem.itemData.Item is Shield)
                    unit.unitMeshManager.leftHeldItem.anim.Play("MeleeAttack_OtherHand_L");
            }
            else if (this == unit.unitMeshManager.leftHeldItem)
            {
                if (itemData.Item.Weapon.IsTwoHanded == false)
                    anim.Play("DefaultAttack_1H_L");

                if (unit.unitMeshManager.rightHeldItem != null && unit.unitMeshManager.rightHeldItem.itemData.Item is Shield)
                    unit.unitMeshManager.rightHeldItem.anim.Play("MeleeAttack_OtherHand_R");
            }

            // Rotate the weapon towards the target, just in case they are above or below this Unit's position
            StartCoroutine(RotateWeaponTowardsTarget(targetUnit.GridPosition));
        }

        public void DoSwipeAttack()
        {
            if (anim == null)
                return;

            GridPosition targetGridPosition = unit.unitActionHandler.GetAction<SwipeAction>().targetGridPosition;
            foreach (GridPosition gridPosition in unit.unitActionHandler.GetAction<SwipeAction>().GetActionAreaGridPositions(targetGridPosition))
            {
                if (LevelGrid.Instance.HasAnyUnitOnGridPosition(gridPosition) == false)
                    continue;

                Unit targetUnit = LevelGrid.Instance.GetUnitAtGridPosition(gridPosition);
                HeldItem itemBlockedWith;

                // The targetUnit tries to block and if they're successful, the targetUnit and the weapon/shield they blocked with are added to the targetUnits dictionary
                bool attackBlocked = targetUnit.unitActionHandler.TryBlockMeleeAttack(unit);
                unit.unitActionHandler.targetUnits.TryGetValue(targetUnit, out itemBlockedWith);

                // If the target is successfully blocking the attack
                if (attackBlocked)
                    BlockAttack(targetUnit, itemBlockedWith);
            }

            // Play the Swipe animation
            anim.Play("SwipeAttack_2H");

            // Rotate the weapon towards the target, just in case they are above or below this Unit's position
            StartCoroutine(RotateWeaponTowardsTarget(targetGridPosition));
        }

        void BlockAttack(Unit blockingUnit, HeldItem itemBlockedWith)
        {
            // Target Unit rotates towards this Unit & does block animation with shield or weapon
            blockingUnit.unitActionHandler.turnAction.RotateTowards_Unit(unit, false);
            if (itemBlockedWith is HeldShield)
                blockingUnit.unitMeshManager.GetHeldShield().RaiseShield();
            else
            {
                HeldMeleeWeapon heldWeapon = itemBlockedWith as HeldMeleeWeapon;
                heldWeapon.RaiseWeapon();
            }
        }

        public void RaiseWeapon()
        {
            if (weaponRaised)
                return;

            weaponRaised = true;
            if (unit.unitMeshManager.rightHeldItem == this)
            {
                if (itemData.Item.Weapon.IsTwoHanded)
                    anim.Play("RaiseWeapon_2H");
                else
                    anim.Play("RaiseWeapon_1H_R");
            }
            else if (unit.unitMeshManager.leftHeldItem == this)
            {
                if (itemData.Item.Weapon.IsTwoHanded)
                    Debug.LogWarning("Animation not created yet.");
                else
                    anim.Play("RaiseWeapon_1H_L");
            }
        }

        public void LowerWeapon()
        {
            if (weaponRaised == false)
                return;

            weaponRaised = false;
            if (unit.unitMeshManager.rightHeldItem == this)
            {
                if (itemData.Item.Weapon.IsTwoHanded)
                    anim.Play("LowerWeapon_2H");
                else
                    anim.Play("LowerWeapon_1H_R");
            }
            else if (unit.unitMeshManager.leftHeldItem == this)
            {
                if (itemData.Item.Weapon.IsTwoHanded)
                    Debug.LogWarning("Animation not created yet.");
                else
                    anim.Play("LowerWeapon_1H_L");
            }
        }

        // Used in animation Key Frame
        void DamageTargetUnits()
        {
            unit.unitActionHandler.lastQueuedAction.BaseAttackAction.DamageTargets(this);
        }

        IEnumerator RotateWeaponTowardsTarget(GridPosition targetGridPosition)
        {
            if (targetGridPosition.y == unit.GridPosition.y)
                yield break;

            Vector3 lookPos = (targetGridPosition.WorldPosition() - transform.parent.position).normalized;
            Vector3 startRotation = transform.parent.localEulerAngles;
            Quaternion targetRotation = Quaternion.LookRotation(lookPos);
            targetRotation = Quaternion.Euler(new Vector3(startRotation.x, startRotation.y, -targetRotation.eulerAngles.x));
            float rotateSpeed = 10f;

            while (unit.unitActionHandler.isAttacking)
            {
                transform.parent.localRotation = Quaternion.Slerp(transform.parent.localRotation, targetRotation, rotateSpeed * Time.deltaTime);
                yield return null;
            }

            transform.parent.localRotation = targetRotation;
        }

        public int DamageAmount()
        {
            int damageAmount = itemData.Damage;
            if (unit.UnitEquipment.IsDualWielding())
            {
                if (this == unit.unitMeshManager.GetRightHeldMeleeWeapon())
                    damageAmount = Mathf.RoundToInt(damageAmount * GameManager.dualWieldPrimaryEfficiency);
                else
                    damageAmount = Mathf.RoundToInt(damageAmount * GameManager.dualWieldSecondaryEfficiency);
            }

            return damageAmount;
        }

        public float MaxRange(GridPosition attackerGridPosition, GridPosition targetGridPosition, bool accountForHeight)
        {
            if (accountForHeight == false)
                return itemData.Item.Weapon.MaxRange;

            float maxRange = itemData.Item.Weapon.MaxRange - Mathf.Abs(targetGridPosition.y - attackerGridPosition.y);
            if (maxRange < 0f) maxRange = 0f;
            return maxRange;
        }
    }
}
