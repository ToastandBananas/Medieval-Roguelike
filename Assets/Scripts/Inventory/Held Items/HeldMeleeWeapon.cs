using System.Collections;
using UnityEngine;

public class HeldMeleeWeapon : HeldItem
{
    public override void DoDefaultAttack()
    {
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

        StartCoroutine(RotateWeaponTowardsTarget(unit.unitActionHandler.targetEnemyUnit.gridPosition));
    }

    // Used in animation Key Frame
    void DamageTargetUnit()
    {
        // TODO: Determine damage from weapon data and attacking Unit's stats/perks
        unit.unitActionHandler.targetEnemyUnit.vision.AddVisibleUnit(unit); // The target Unit becomes aware of this Unit
        unit.unitActionHandler.targetEnemyUnit.health.TakeDamage(itemData.damage);
    }

    IEnumerator RotateWeaponTowardsTarget(GridPosition targetGridPosition)
    {
        Vector3 lookPos = (targetGridPosition.WorldPosition() - transform.parent.position).normalized;
        Quaternion targetRotation = Quaternion.LookRotation(lookPos);
        targetRotation = Quaternion.Euler(new Vector3(-targetRotation.eulerAngles.x, 0f, 0f));

        float rotateSpeed = 10f;
        while (transform.parent.localRotation != targetRotation)
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
}
