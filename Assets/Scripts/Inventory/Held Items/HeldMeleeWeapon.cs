using System.Collections;
using UnityEngine;
using GridSystem;
using UnitSystem;

namespace InventorySystem
{
    public class HeldMeleeWeapon : HeldItem
    {
        readonly float defaultAttackTransitionTime = 0.1f;
        readonly float defaultBlockTransitionTime = 0.1f;

        public override void SetupHeldItem(ItemData itemData, Unit unit, EquipSlot equipSlot)
        {
            base.SetupHeldItem(itemData, unit, equipSlot);

            if (this == unit.unitMeshManager.leftHeldItem)
                anim.SetBool("leftHandItem", true);
            else
            {
                anim.SetBool("leftHandItem", false);

                if (itemData.Item.Weapon.IsTwoHanded)
                    anim.SetBool("twoHanded", true);
                else
                    anim.SetBool("twoHanded", false);
            }

            SetDefaultWeaponStance();
            UpdateActionIcons();
        }

        public override void DoDefaultAttack(GridPosition targetGridPosition)
        {
            // Determine attack animation based on melee weapon type
            if (this == unit.unitMeshManager.rightHeldItem)
            {
                if (itemData.Item.MeleeWeapon.DefaultMeleeAttackType == MeleeAttackType.Overhead)
                {
                    if (itemData.Item.Weapon.IsTwoHanded)
                        anim.CrossFadeInFixedTime("DefaultAttack_2H", defaultAttackTransitionTime);
                    else
                        anim.CrossFadeInFixedTime("DefaultAttack_1H_R", defaultAttackTransitionTime);
                }
                else if (itemData.Item.MeleeWeapon.DefaultMeleeAttackType == MeleeAttackType.Thrust)
                {
                    if (itemData.Item.Weapon.IsTwoHanded)
                        anim.CrossFadeInFixedTime("DefaultThrustAttack_2H", defaultAttackTransitionTime);
                    else
                        anim.CrossFadeInFixedTime("DefaultThrustAttack_1H_R", defaultAttackTransitionTime);
                }

                HeldItem oppositeHeldItem = GetOppositeHeldItem();
                if (oppositeHeldItem != null && oppositeHeldItem.itemData.Item is Shield)
                    oppositeHeldItem.anim.CrossFadeInFixedTime("MeleeAttack_OtherHand_L", defaultAttackTransitionTime);
            }
            else if (this == unit.unitMeshManager.leftHeldItem)
            {
                if (itemData.Item.MeleeWeapon.DefaultMeleeAttackType == MeleeAttackType.Overhead)
                    anim.CrossFadeInFixedTime("DefaultAttack_1H_L", defaultAttackTransitionTime);
                else if (itemData.Item.MeleeWeapon.DefaultMeleeAttackType == MeleeAttackType.Thrust)
                    anim.CrossFadeInFixedTime("DefaultThrustAttack_1H_L", defaultAttackTransitionTime);

                HeldItem oppositeHeldItem = GetOppositeHeldItem();
                if (oppositeHeldItem != null && oppositeHeldItem.itemData.Item is Shield)
                    oppositeHeldItem.anim.CrossFadeInFixedTime("MeleeAttack_OtherHand_R", defaultAttackTransitionTime);
            }

            // Rotate the weapon towards the target, just in case they are above or below this Unit's position
            StartCoroutine(RotateWeaponTowardsTarget(targetGridPosition));
        }

        public void DoSwipeAttack(GridPosition targetGridPosition)
        {
            // Play the Swipe animation
            anim.CrossFadeInFixedTime("SwipeAttack_2H", defaultAttackTransitionTime);

            // Rotate the weapon towards the target, just in case they are above or below this Unit's position
            StartCoroutine(RotateWeaponTowardsTarget(targetGridPosition));
        }

        public override void BlockAttack(Unit attackingUnit)
        {
            base.BlockAttack(attackingUnit);
            RaiseWeapon();
        }

        public void RaiseSpearWall()
        {
            unit.unitAnimator.StopMovingForward();

            anim.SetBool("spearWall", true);
            if (this == unit.unitMeshManager.leftHeldItem)
                anim.CrossFadeInFixedTime("SpearWall_1H_L", defaultBlockTransitionTime);
            else
            {
                if (itemData.Item.Weapon.IsTwoHanded)
                    anim.CrossFadeInFixedTime("SpearWall_2H", defaultBlockTransitionTime);
                else
                    anim.CrossFadeInFixedTime("SpearWall_1H_R", defaultBlockTransitionTime);
            }

            currentHeldItemStance = HeldItemStance.SpearWall;
        }

        public void LowerSpearWall()
        {
            anim.SetBool("spearWall", false);
            anim.CrossFadeInFixedTime("Idle", defaultBlockTransitionTime);

            if (anim.GetBool("versatileStance") == false)
                currentHeldItemStance = HeldItemStance.Default;
            else
                currentHeldItemStance = HeldItemStance.Versatile;
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
                    anim.CrossFadeInFixedTime("RaiseWeapon_2H", defaultBlockTransitionTime);
                else
                    anim.CrossFadeInFixedTime("RaiseWeapon_1H_R", defaultBlockTransitionTime);
            }
            else if (unit.unitMeshManager.leftHeldItem == this)
                anim.CrossFadeInFixedTime("RaiseWeapon_1H_L", defaultBlockTransitionTime);
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

        public void Recoil()
        {
            if (unit.unitMeshManager.rightHeldItem == this)
            {
                if (itemData.Item.Weapon.IsTwoHanded)
                    anim.Play("BlockRecoil_2H");
                else
                    anim.Play("BlockRecoil_1H_R");
            }
            else if (unit.unitMeshManager.leftHeldItem == this)
                anim.Play("BlockRecoil_1H_L");
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

        protected override float GetFumbleChance()
        {
            MeleeWeapon weapon = itemData.Item as MeleeWeapon;

            float fumbleChance = (0.5f - (unit.stats.WeaponSkill(weapon) / 100f)) * 0.4f; // Weapon skill modifier
            fumbleChance += weapon.Weight / unit.stats.Strength.GetValue() / 100f * 15f; // Weapon weight to strength ratio modifier

            if (fumbleChance < 0f)
                fumbleChance = 0f;
            else
            {
                // Less likely to fumble when two-handing a melee weapon
                if (weapon.IsTwoHanded || currentHeldItemStance == HeldItemStance.Versatile)
                    fumbleChance *= 0.8f;
            }
            // Debug.Log(unit.name + " fumble chance: " + fumbleChance);
            return fumbleChance;
        }
    }
}
