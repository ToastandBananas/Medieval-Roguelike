using System;
using System.Collections;
using UnityEngine;

public class UnitAnimator : MonoBehaviour
{
    [SerializeField] Transform headTransform;
    Animator unitAnim;

    public Animator leftHeldItemAnim { get; private set; }
    public Animator rightHeldItemAnim { get; private set; }

    Unit unit;

    HeldItem itemBlockedWith;
    bool unarmedAttackBlocked;

    void Awake()
    {
        unit = transform.parent.GetComponent<Unit>();
        unitAnim = GetComponent<Animator>();
        /*if (TryGetComponent(out MoveAction moveAction))
        {
            //moveAction.OnStartMoving += MoveAction_OnStartMoving;
          moveAction.OnStopMoving += MoveAction_OnStopMoving;
        }*/
    }

    public void StartMovingForward() => unitAnim.SetBool("isMoving", true);

    public void StopMovingForward() => unitAnim.SetBool("isMoving", false);

    public void StartMeleeAttack() => unitAnim.Play("Melee Attack");

    public void StartDualMeleeAttack() => unitAnim.Play("Dual Melee Attack");

    public void DoUnarmedAttack(bool unarmedAttackBlocked, HeldItem itemBlockedWith)
    {
        Unit targetUnit = unit.unitActionHandler.targetEnemyUnit;
        this.itemBlockedWith = itemBlockedWith;
        this.unarmedAttackBlocked = unarmedAttackBlocked;

        if (unarmedAttackBlocked)
        {
            // Target Unit rotates towards this Unit & does block animation
            StartCoroutine(targetUnit.unitActionHandler.GetAction<TurnAction>().RotateTowards_AttackingTargetUnit(unit, false));

            if (itemBlockedWith is HeldShield)
                targetUnit.GetShield().RaiseShield();
            else
            {
                HeldMeleeWeapon heldWeapon = itemBlockedWith as HeldMeleeWeapon;
                heldWeapon.RaiseWeapon();
            }
        }

        unitAnim.Play("Unarmed Attack");
    }

    // Used in animation Key Frame
    void DamageTargetUnit_UnarmedAttack()
    {
        unit.unitActionHandler.GetAction<MeleeAction>().DamageTarget(null, unarmedAttackBlocked, itemBlockedWith);
        itemBlockedWith = null;
        unarmedAttackBlocked = false; // Reset this bool for the next attack
    }

    public void Die()
    {
        int random = UnityEngine.Random.Range(0, 2);
        if (random == 0)
            unitAnim.Play("Die Backward");
        else
            unitAnim.Play("Die Forward");

        // StartCoroutine(Die_RotateHead());
    }

    IEnumerator Die_RotateHead()
    {
        float targetRotation = UnityEngine.Random.Range(-65f, 65f);

        if (targetRotation <= 1f)
            yield break;

        float rotateSpeed = 75f;
        while (Mathf.Abs(headTransform.localEulerAngles.y - MathF.Abs(targetRotation)) > 0.5f)
        {
            headTransform.Rotate(0f, rotateSpeed * Time.deltaTime, 0f);
            yield return null;
        }
    }

    public void SetLeftHeldItemAnim(Animator leftHeldItemAnim) => this.leftHeldItemAnim = leftHeldItemAnim;

    public void SetRightHeldItemAnim(Animator rightHeldItemAnim) => this.rightHeldItemAnim = rightHeldItemAnim;

    public void SetLeftHeldItemAnimController(RuntimeAnimatorController animController) => leftHeldItemAnim.runtimeAnimatorController = animController;

    public void SetRightHeldItemAnimController(RuntimeAnimatorController animController) => rightHeldItemAnim.runtimeAnimatorController = animController;
}
