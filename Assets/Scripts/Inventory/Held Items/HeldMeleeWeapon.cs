using System.Collections;
using UnityEngine;

public class HeldMeleeWeapon : HeldItem
{
    public override void DoDefaultAttack()
    {
        // TODO: Determine attack animation based on melee weapon type
        anim.Play("Attack_1H");
        if (unit.leftHeldItem != null && unit.leftHeldItem.itemData.item is Shield)
            unit.leftHeldItem.anim.Play("MeleeAttack_OtherHand");

        StartCoroutine(RotateTowardsTarget(unit.unitActionHandler.targetEnemyUnit.gridPosition));
    }

    void DamageTargetUnit()
    {
        // TODO: Determine damage from weapon data and attacking Unit's stats/perks
        unit.unitActionHandler.targetEnemyUnit.healthSystem.TakeDamage(itemData.damage);
    }

    IEnumerator ResetToIdleRotation()
    {
        Quaternion idleRotation = Quaternion.Euler(Vector3.zero);
        float rotateSpeed = 10f;
        while (transform.parent.localRotation != idleRotation)
        {
            transform.parent.localRotation = Quaternion.Slerp(transform.parent.localRotation, idleRotation, rotateSpeed * Time.deltaTime);
            yield return null;
        }

        transform.parent.localRotation = idleRotation;
    }

    IEnumerator RotateTowardsTarget(GridPosition targetGridPosition)
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
}
