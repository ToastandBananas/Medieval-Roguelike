using System;
using System.Collections;
using UnityEngine;

public class MeleeWeapon : HeldItem
{
    MeleeAction meleeAction;

    public override void DoDefaultAttack()
    {
        // TODO: Determine attack animation based on melee weapon type
        anim.Play("Attack_1H");
        myUnit.UnitAnimator().Play("MeleeAttack");
        if (myUnit.LeftHeldItem() != null && myUnit.LeftHeldItem() is Shield)
            myUnit.LeftHeldItem().HeldItemAnimator().Play("MeleeAttack_OtherHand");

        StartCoroutine(RotateMeleeWeaponTowardsTarget(myUnit.GetAction<MeleeAction>().TargetUnit().GridPosition()));
    }

    void DamageTargetUnit()
    {
        // TODO: Determine damage from weapon data and attacking Unit's stats/perks
        meleeAction.TargetUnit().TakeDamage(40);
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

    IEnumerator RotateMeleeWeaponTowardsTarget(GridPosition targetGridPosition)
    {
        Vector3 lookPos = (targetGridPosition.WorldPosition() - transform.parent.position).normalized;
        Quaternion targetRotation = Quaternion.LookRotation(lookPos);
        targetRotation = Quaternion.Euler(new Vector3(-targetRotation.eulerAngles.x, 0f, 0f));

        float rotateSpeed = 10f;
        while (transform.parent.localRotation != targetRotation)
        {
            Debug.Log("Rotating");
            transform.parent.localRotation = Quaternion.Slerp(transform.parent.localRotation, targetRotation, rotateSpeed * Time.deltaTime);
            yield return null;
        }

        transform.parent.localRotation = targetRotation;
    }

    public override void SetupBaseActions()
    {
        meleeAction = transform.parent.parent.parent.parent.parent.GetComponent<MeleeAction>();
        meleeAction.OnStartAttack += MeleeAction_OnStartAttack;
        meleeAction.OnStopAttack += MeleeAction_OnStopAttack;
    }

    public override void RemoveHeldItem()
    {
        meleeAction.OnStartAttack -= MeleeAction_OnStartAttack;
        meleeAction.OnStopAttack -= MeleeAction_OnStopAttack;
    }

    void MeleeAction_OnStartAttack(object sender, EventArgs e)
    {
        DoDefaultAttack();
    }

    void MeleeAction_OnStopAttack(object sender, EventArgs e)
    {
        UnitActionSystemUI.Instance.UpdateActionVisuals();
    }
}
