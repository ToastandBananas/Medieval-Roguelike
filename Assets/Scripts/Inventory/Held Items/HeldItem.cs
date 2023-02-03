using System.Collections;
using UnityEngine;

// If a class is abstract, this means that the class cannot be placed on a GameObject
public abstract class HeldItem : MonoBehaviour
{
    [SerializeField] Vector3 idlePosition = new Vector3(-0.23f, -0.3f, -0.23f);
    [SerializeField] Vector3 idleRotation = new Vector3(0f, 90f, 0f);

    public Animator anim { get; private set; }

    protected Unit unit;

    public ItemData itemData { get; private set; }

    void Awake()
    {
        anim = GetComponent<Animator>();
        itemData = GetComponent<ItemData>();

        SetItemPosition();
        SetItemRotation();

        SetUnit();
    }

    public void SetItemData(ItemData itemData) => this.itemData = itemData;

    void SetUnit() => unit = transform.parent.parent.parent.parent.parent.GetComponent<Unit>();

    void SetItemPosition() => transform.parent.localPosition = idlePosition;

    void SetItemRotation() => transform.localEulerAngles = idleRotation;

    public void ResetItemTransform()
    {
        SetItemPosition();
        SetItemRotation();
    }

    public Vector3 IdlePosition() => idlePosition;

    public Vector3 IdleRotation() => idleRotation;

    IEnumerator ResetToIdleRotation()
    {
        Quaternion idleRotation = Quaternion.Euler(IdleRotation());
        float rotateSpeed = 5f;
        while (transform.localRotation != idleRotation)
        {
            transform.localRotation = Quaternion.Slerp(transform.localRotation, idleRotation, rotateSpeed * Time.deltaTime);
            yield return null;
        }

        transform.localRotation = idleRotation;
    }

    public abstract void DoDefaultAttack();

    public IEnumerator DelayDoDefaultAttack()
    {
        yield return new WaitForSeconds(AnimationTimes.Instance.GetWeaponAttackAnimationTime(unit.rightHeldItem.itemData.item as Weapon) / 2f);
        DoDefaultAttack();
    }
}