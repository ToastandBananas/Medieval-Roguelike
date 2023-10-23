using System.Collections;
using UnityEngine;
using GridSystem;
using UnitSystem;

namespace InventorySystem
{
    public class HeldMeleeWeapon : HeldItem
    {
        public override void DoDefaultAttack(GridPosition targetGridPosition)
        {
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
            StartCoroutine(RotateWeaponTowardsTarget(targetGridPosition));
        }

        public void DoSwipeAttack(GridPosition targetGridPosition)
        {
            // Play the Swipe animation
            anim.Play("SwipeAttack_2H");

            // Rotate the weapon towards the target, just in case they are above or below this Unit's position
            StartCoroutine(RotateWeaponTowardsTarget(targetGridPosition));
        }

        public override void BlockAttack(Unit attackingUnit)
        {
            base.BlockAttack(attackingUnit);
            RaiseWeapon();
        }

        public override void StopBlocking() => LowerWeapon();

        public void RaiseWeapon()
        {
            if (isBlocking)
                return;

            isBlocking = true;
            if (unit.unitMeshManager.rightHeldItem == this)
            {
                if (itemData.Item.Weapon.IsTwoHanded)
                    anim.Play("RaiseWeapon_2H");
                else
                    anim.Play("RaiseWeapon_1H_R");
            }
            else if (unit.unitMeshManager.leftHeldItem == this)
                anim.Play("RaiseWeapon_1H_L");
        }

        public void LowerWeapon()
        {
            if (isBlocking == false)
                return;

            isBlocking = false;
            if (unit.unitMeshManager.rightHeldItem == this)
            {
                if (itemData.Item.Weapon.IsTwoHanded)
                    anim.Play("LowerWeapon_2H");
                else
                    anim.Play("LowerWeapon_1H_R");
            }
            else if (unit.unitMeshManager.leftHeldItem == this)
                anim.Play("LowerWeapon_1H_L");
        }

        IEnumerator RotateWeaponTowardsTarget(GridPosition targetGridPosition)
        {
            if (targetGridPosition.y == unit.GridPosition.y)
                yield break;

            Vector3 lookPos = (targetGridPosition.WorldPosition - transform.parent.position).normalized;
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
