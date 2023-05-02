using System.Collections;
using UnityEngine;

public class HeldMeleeWeapon : HeldItem
{
    public override void DoDefaultAttack()
    {
        Unit targetUnit = unit.unitActionHandler.targetEnemyUnit;
        HeldItem itemBlockedWith = null;

        // The targetUnit tries to block and if they're successful, the targetUnit and the weapon/shield they blocked with are added to the targetUnits dictionary
        bool attackBlocked = targetUnit.TryBlockMeleeAttack(unit);
        unit.unitActionHandler.targetUnits.TryGetValue(targetUnit, out itemBlockedWith);

        // If the target is successfully blocking the attack
        if (attackBlocked)
            BlockAttack(targetUnit, itemBlockedWith);

        // Determine attack animation based on melee weapon type
        if (this == unit.rightHeldItem)
        {
            if (itemData.item.Weapon().isTwoHanded)
                anim.Play("DefaultAttack_2H");
            else
                anim.Play("DefaultAttack_1H_R");

            if (unit.leftHeldItem != null && unit.leftHeldItem.itemData.item is Shield) 
                unit.leftHeldItem.anim.Play("MeleeAttack_OtherHand_L"); 
        }
        else if (this == unit.leftHeldItem)
        {
            if (itemData.item.Weapon().isTwoHanded == false)
                anim.Play("DefaultAttack_1H_L");

            if (unit.rightHeldItem != null && unit.rightHeldItem.itemData.item is Shield)
                unit.rightHeldItem.anim.Play("MeleeAttack_OtherHand_R");
        }

        // Rotate the weapon towards the target, just in case they are above or below this Unit's position
        StartCoroutine(RotateWeaponTowardsTarget(targetUnit.gridPosition));
    }

    public void DoSwipeAttack()
    {
        foreach (GridPosition gridPosition in unit.unitActionHandler.GetAction<SwipeAction>().GetActionAreaGridPositions(unit.unitActionHandler.targetAttackGridPosition))
        {
            if (LevelGrid.Instance.HasAnyUnitOnGridPosition(gridPosition) == false)
                continue;

            Unit targetUnit = LevelGrid.Instance.GetUnitAtGridPosition(gridPosition);
            HeldItem itemBlockedWith = null;

            // The targetUnit tries to block and if they're successful, the targetUnit and the weapon/shield they blocked with are added to the targetUnits dictionary
            bool attackBlocked = targetUnit.TryBlockMeleeAttack(unit);
            unit.unitActionHandler.targetUnits.TryGetValue(targetUnit, out itemBlockedWith);

            // If the target is successfully blocking the attack
            if (attackBlocked)
                BlockAttack(targetUnit, itemBlockedWith);
        }

        // Play the Swipe animation
        anim.Play("SwipeAttack_2H");

        // Rotate the weapon towards the target, just in case they are above or below this Unit's position
        StartCoroutine(RotateWeaponTowardsTarget(unit.unitActionHandler.targetAttackGridPosition));
    }

    void BlockAttack(Unit blockingUnit, HeldItem itemBlockedWith)
    {
        // Target Unit rotates towards this Unit & does block animation with shield or weapon
        StartCoroutine(blockingUnit.unitActionHandler.GetAction<TurnAction>().RotateTowards_AttackingTargetUnit(unit, false));
        if (itemBlockedWith is HeldShield)
            blockingUnit.GetShield().RaiseShield();
        else
        {
            HeldMeleeWeapon heldWeapon = itemBlockedWith as HeldMeleeWeapon;
            heldWeapon.RaiseWeapon();
        }
    }

    public void RaiseWeapon()
    {
        if (unit.rightHeldItem == this)
        {
            if (itemData.item.Weapon().isTwoHanded)
                Debug.LogWarning("Animation not created yet.");
            else
                anim.Play("RaiseWeapon_1H_R");
        }
        else if (unit.leftHeldItem == this)
        {
            if (itemData.item.Weapon().isTwoHanded)
                Debug.LogWarning("Animation not created yet.");
            else
                anim.Play("RaiseWeapon_1H_L");
        }
    }

    public void LowerWeapon()
    {
        if (unit.rightHeldItem == this)
        {
            if (itemData.item.Weapon().isTwoHanded)
                Debug.LogWarning("Animation not created yet.");
            else
                anim.Play("LowerWeapon_1H_R");
        }
        else if (unit.leftHeldItem == this)
        {
            if (itemData.item.Weapon().isTwoHanded)
                Debug.LogWarning("Animation not created yet.");
            else
                anim.Play("LowerWeapon_1H_L");
        }
    }

    // Used in animation Key Frame
    void DamageTargetUnits()
    {
        switch (unit.unitActionHandler.lastQueuedAction)
        {
            case MeleeAction meleeAction:
                meleeAction.DamageTargets(this);
                break;
            case ShootAction shootAction:
                shootAction.DamageTargets(this);
                break;
            case SwipeAction swipeAction:
                swipeAction.DamageTargets(this);
                break;
            default:
                break;
        }
        //unit.unitActionHandler.lastQueuedAction.DamageTargets(this);
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

    public int DamageAmount()
    {
        int damageAmount = itemData.damage;
        if (unit.IsDualWielding())
        {
            if (this == unit.GetRightMeleeWeapon())
                damageAmount = Mathf.RoundToInt(damageAmount * GameManager.dualWieldPrimaryEfficiency);
            else
                damageAmount = Mathf.RoundToInt(damageAmount * GameManager.dualWieldSecondaryEfficiency);
        }

        return damageAmount;
    }

    public float MaxRange(GridPosition attackerGridPosition, GridPosition targetGridPosition, bool accountForHeight)
    {
        if (accountForHeight == false)
            return itemData.item.Weapon().maxRange;

        float maxRange = itemData.item.Weapon().maxRange - Mathf.Abs(targetGridPosition.y - attackerGridPosition.y);
        if (maxRange < 0f) maxRange = 0f;
        return maxRange;
    }
}
