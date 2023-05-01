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

    public void DoDefaultUnarmedAttack()
    {
        Unit targetUnit = unit.unitActionHandler.targetEnemyUnit;
        HeldItem itemBlockedWith = null;

        // The targetUnit tries to block and if they're successful, the weapon/shield they blocked with is added as a corresponding Value in the attacking Unit's targetUnits dictionary
        bool attackBlocked = targetUnit.TryBlockMeleeAttack(unit);
        if (unit.unitActionHandler.targetUnits.ContainsKey(targetUnit))
            unit.unitActionHandler.targetUnits.TryGetValue(targetUnit, out itemBlockedWith);

        if (attackBlocked)
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
        unit.unitActionHandler.GetAction<MeleeAction>().DamageTargets(null);
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
