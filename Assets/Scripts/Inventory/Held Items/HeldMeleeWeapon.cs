using System.Collections;
using UnityEngine;

public class HeldMeleeWeapon : HeldItem
{
    bool attackBlocked;

    public override void DoDefaultAttack(bool attackBlocked)
    {
        Unit targetUnit = unit.unitActionHandler.targetEnemyUnit;
        this.attackBlocked = attackBlocked;

        if (attackBlocked)
        {
            // Target Unit rotates towards this Unit & does block animation
            StartCoroutine(targetUnit.unitActionHandler.GetAction<TurnAction>().RotateTowards_AttackingTargetUnit(unit, false));
            if (targetUnit.ShieldEquipped())
                targetUnit.GetShield().RaiseShield();
        }

        // TODO: Determine attack animation based on melee weapon type
        if (this == unit.rightHeldItem)
        {
            if (itemData.item.Weapon().isTwoHanded == false)
                anim.Play("Attack_1H_R");

            if (unit.leftHeldItem != null && unit.leftHeldItem.itemData.item is Shield) 
                unit.leftHeldItem.anim.Play("MeleeAttack_OtherHand_L"); 
        }
        else if (this == unit.leftHeldItem)
        {
            if (itemData.item.Weapon().isTwoHanded == false)
                anim.Play("Attack_1H_L");

            if (unit.rightHeldItem != null && unit.rightHeldItem.itemData.item is Shield)
                unit.rightHeldItem.anim.Play("MeleeAttack_OtherHand_R");
        }

        if (unit.IsUnarmed() == false)
            StartCoroutine(RotateWeaponTowardsTarget(targetUnit.gridPosition));
    }

    // Used in animation Key Frame
    void DamageTargetUnit()
    {
        unit.unitActionHandler.GetAction<MeleeAction>().DamageTarget(this, attackBlocked);
        attackBlocked = false; // Reset this bool for the next attack
    }

    IEnumerator RotateWeaponTowardsTarget(GridPosition targetGridPosition)
    {
        if (targetGridPosition.y == unit.gridPosition.y)
            yield break;

        MeleeAction meleeAction = unit.unitActionHandler.GetAction<MeleeAction>();
        Vector3 lookPos = (targetGridPosition.WorldPosition() - transform.parent.position).normalized;
        Quaternion targetRotation = Quaternion.LookRotation(lookPos);
        targetRotation = Quaternion.Euler(new Vector3(-targetRotation.eulerAngles.x, 0f, 0f));
        float rotateSpeed = 10f;

        while (meleeAction.isAttacking)
        {
            transform.parent.localRotation = Quaternion.Slerp(transform.parent.localRotation, targetRotation, rotateSpeed * Time.deltaTime);
            yield return null;
        }

        transform.parent.localRotation = targetRotation;
    }

    public float MaxRange(GridPosition attackerGridPosition, GridPosition targetGridPosition)
    {
        float maxRange = itemData.item.Weapon().maxRange - Mathf.Abs(targetGridPosition.y - attackerGridPosition.y);
        if (maxRange < 0f) maxRange = 0f;
        return maxRange;
    }

    public void ResetAttackBlocked() => attackBlocked = false;
}
