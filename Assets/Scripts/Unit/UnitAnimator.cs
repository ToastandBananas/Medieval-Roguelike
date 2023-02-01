using System;
using System.Collections;
using UnityEngine;

public class UnitAnimator : MonoBehaviour
{
    [SerializeField] Transform headTransform;
    [SerializeField] Animator unitAnim;

    public Animator leftHeldItemAnim { get; private set; }
    public Animator rightHeldItemAnim { get; private set; }

    /*void Awake()
    {
        if (TryGetComponent(out MoveAction moveAction))
        {
            //moveAction.OnStartMoving += MoveAction_OnStartMoving;
          moveAction.OnStopMoving += MoveAction_OnStopMoving;
        }
    }*/

    public void StartMovingForward()
    {
        unitAnim.SetBool("isMoving", true);
    }

    public void StopMovingForward()
    {
        unitAnim.SetBool("isMoving", false);
    }

    public void StartMeleeAttack()
    {
        unitAnim.Play("Melee Attack");
    }

    public void StartDualMeleeAttack()
    {
        unitAnim.Play("Dual Melee Attack");
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

    public void SetLeftHeldItemAnim(Animator leftHeldItemAnim)
    {
        this.leftHeldItemAnim = leftHeldItemAnim;
    }

    public void SetRightHeldItemAnim(Animator rightHeldItemAnim)
    {
        this.rightHeldItemAnim = rightHeldItemAnim;
    }

    public void SetLeftHeldItemAnimController(RuntimeAnimatorController animController)
    {
        leftHeldItemAnim.runtimeAnimatorController = animController;
    }

    public void SetRightHeldItemAnimController(RuntimeAnimatorController animController)
    {
        rightHeldItemAnim.runtimeAnimatorController = animController;
    }
}
