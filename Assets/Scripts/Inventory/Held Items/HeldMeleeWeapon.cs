using System.Collections;
using UnityEngine;
using GridSystem;
using UnitSystem;
using UnitSystem.ActionSystem.Actions;

namespace InventorySystem
{
    public class HeldMeleeWeapon : HeldItem
    {
        readonly float defaultAttackTransitionTime = 0.1f;
        readonly float defaultBlockTransitionTime = 0.1f;

        public override void SetupHeldItem(ItemData itemData, Unit unit, EquipSlot equipSlot)
        {
            base.SetupHeldItem(itemData, unit, equipSlot);

            if (this == unit.UnitMeshManager.leftHeldItem)
                Anim.SetBool("leftHandItem", true);
            else
            {
                Anim.SetBool("leftHandItem", false);

                if (itemData.Item.Weapon.IsTwoHanded)
                    Anim.SetBool("twoHanded", true);
                else
                    Anim.SetBool("twoHanded", false);
            }

            SetDefaultWeaponStance();
            UpdateActionIcons();
        }

        public override void SetupItemToThrow(ItemData itemData, Unit unit, Transform heldItemParent)
        {
            base.SetupItemToThrow(itemData, unit, heldItemParent);
            SetDefaultWeaponStance();
        }

        public override void DoDefaultAttack(GridPosition targetGridPosition)
        {
            // Determine attack animation based on melee weapon type
            if (this == unit.UnitMeshManager.rightHeldItem)
            {
                if (ItemData.Item.MeleeWeapon.DefaultMeleeAttackType == MeleeAttackType.Overhead)
                {
                    if (ItemData.Item.Weapon.IsTwoHanded)
                        Anim.CrossFadeInFixedTime("DefaultAttack_2H", defaultAttackTransitionTime);
                    else
                        Anim.CrossFadeInFixedTime("DefaultAttack_1H_R", defaultAttackTransitionTime);
                }
                else if (ItemData.Item.MeleeWeapon.DefaultMeleeAttackType == MeleeAttackType.Thrust)
                {
                    if (ItemData.Item.Weapon.IsTwoHanded)
                        Anim.CrossFadeInFixedTime("DefaultThrustAttack_2H", defaultAttackTransitionTime);
                    else
                        Anim.CrossFadeInFixedTime("DefaultThrustAttack_1H_R", defaultAttackTransitionTime);
                }

                HeldItem oppositeHeldItem = GetOppositeHeldItem();
                if (oppositeHeldItem != null && oppositeHeldItem.ItemData.Item is Item_Shield)
                    oppositeHeldItem.Anim.CrossFadeInFixedTime("MeleeAttack_OtherHand_L", defaultAttackTransitionTime);
            }
            else if (this == unit.UnitMeshManager.leftHeldItem)
            {
                if (ItemData.Item.MeleeWeapon.DefaultMeleeAttackType == MeleeAttackType.Overhead)
                    Anim.CrossFadeInFixedTime("DefaultAttack_1H_L", defaultAttackTransitionTime);
                else if (ItemData.Item.MeleeWeapon.DefaultMeleeAttackType == MeleeAttackType.Thrust)
                    Anim.CrossFadeInFixedTime("DefaultThrustAttack_1H_L", defaultAttackTransitionTime);

                HeldItem oppositeHeldItem = GetOppositeHeldItem();
                if (oppositeHeldItem != null && oppositeHeldItem.ItemData.Item is Item_Shield)
                    oppositeHeldItem.Anim.CrossFadeInFixedTime("MeleeAttack_OtherHand_R", defaultAttackTransitionTime);
            }

            // Rotate the weapon towards the target, just in case they are above or below this Unit's position
            StartCoroutine(RotateWeaponTowardsTarget(targetGridPosition));
        }

        public void DoSwipeAttack(GridPosition targetGridPosition)
        {
            // Play the Swipe animation
            Anim.CrossFadeInFixedTime("SwipeAttack_2H", defaultAttackTransitionTime);

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
            unit.UnitAnimator.StopMovingForward();

            Anim.SetBool("spearWall", true);
            if (this == unit.UnitMeshManager.leftHeldItem)
                Anim.CrossFadeInFixedTime("SpearWall_1H_L", defaultBlockTransitionTime);
            else
            {
                if (ItemData.Item.Weapon.IsTwoHanded)
                    Anim.CrossFadeInFixedTime("SpearWall_2H", defaultBlockTransitionTime);
                else
                    Anim.CrossFadeInFixedTime("SpearWall_1H_R", defaultBlockTransitionTime);
            }

            CurrentHeldItemStance = HeldItemStance.SpearWall;
        }

        public void LowerSpearWall()
        {
            Anim.SetBool("spearWall", false);
            Anim.CrossFadeInFixedTime("Idle", defaultBlockTransitionTime);

            if (Anim.GetBool("versatileStance") == false)
                CurrentHeldItemStance = HeldItemStance.Default;
            else
                CurrentHeldItemStance = HeldItemStance.Versatile;
        }

        public override void StopBlocking() => LowerWeapon();

        public void RaiseWeapon()
        {
            if (IsBlocking)
                return;

            IsBlocking = true;
            if (unit.UnitMeshManager.rightHeldItem == this)
            {
                if (ItemData.Item.Weapon.IsTwoHanded)
                    Anim.CrossFadeInFixedTime("RaiseWeapon_2H", defaultBlockTransitionTime);
                else
                    Anim.CrossFadeInFixedTime("RaiseWeapon_1H_R", defaultBlockTransitionTime);
            }
            else if (unit.UnitMeshManager.leftHeldItem == this)
                Anim.CrossFadeInFixedTime("RaiseWeapon_1H_L", defaultBlockTransitionTime);
        }

        public void LowerWeapon()
        {
            if (IsBlocking == false)
                return;

            IsBlocking = false;
            if (unit.UnitMeshManager.rightHeldItem == this)
            {
                if (ItemData.Item.Weapon.IsTwoHanded)
                    Anim.Play("LowerWeapon_2H");
                else
                    Anim.Play("LowerWeapon_1H_R");
            }
            else if (unit.UnitMeshManager.leftHeldItem == this)
                Anim.Play("LowerWeapon_1H_L");
        }

        public void Recoil()
        {
            if (unit.UnitMeshManager.rightHeldItem == this)
            {
                if (ItemData.Item.Weapon.IsTwoHanded)
                    Anim.Play("BlockRecoil_2H");
                else
                    Anim.Play("BlockRecoil_1H_R");
            }
            else if (unit.UnitMeshManager.leftHeldItem == this)
                Anim.Play("BlockRecoil_1H_L");
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

            while (unit.UnitActionHandler.IsAttacking)
            {
                transform.parent.localRotation = Quaternion.Slerp(transform.parent.localRotation, targetRotation, rotateSpeed * Time.deltaTime);
                yield return null;
            }

            transform.parent.localRotation = targetRotation;
        }

        protected override float GetFumbleChance()
        {
            Item_MeleeWeapon weapon = ItemData.Item as Item_MeleeWeapon;

            float fumbleChance = (0.5f - (unit.Stats.WeaponSkill(weapon) / 100f)) * 0.4f; // Weapon skill modifier
            fumbleChance += weapon.Weight / unit.Stats.Strength.GetValue() / 100f * 15f; // Weapon weight to strength ratio modifier

            if (fumbleChance < 0f)
                fumbleChance = 0f;
            else
            {
                // Less likely to fumble when two-handing a melee weapon
                if (weapon.IsTwoHanded || CurrentHeldItemStance == HeldItemStance.Versatile)
                    fumbleChance *= 0.8f;
            }
            // Debug.Log(unit.name + " fumble chance: " + fumbleChance);
            return fumbleChance;
        }
    }
}
