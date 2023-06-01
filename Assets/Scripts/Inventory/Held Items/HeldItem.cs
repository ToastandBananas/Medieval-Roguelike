using System.Collections;
using UnityEngine;

// If a class is abstract, this means that the class cannot be placed on a GameObject
public abstract class HeldItem : MonoBehaviour
{
    [Header("Default Positioning")]
    [SerializeField] Vector3 idlePosition = new Vector3(-0.23f, -0.3f, -0.23f);
    [SerializeField] Vector3 idleRotation = new Vector3(0f, 90f, 0f);

    [Header("Item Data")]
    [SerializeField] protected ItemData itemData;

    public Animator anim { get; private set; }

    protected Unit unit;

    void Awake()
    {
        anim = GetComponent<Animator>();

        itemData.InitializeData();

        SetItemPosition();
        SetItemRotation();

        SetUnit();
    }

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

    public virtual IEnumerator ResetToIdleRotation()
    {
        Quaternion defaultRotation = Quaternion.Euler(Vector3.zero);
        Quaternion startRotation = transform.parent.localRotation;
        float time = 0f;
        float duration = 0.25f;
        while (time < duration)
        {
            transform.parent.localRotation = Quaternion.Slerp(startRotation, defaultRotation, time / duration);
            yield return null;
            time += Time.deltaTime;
        }

        transform.parent.localRotation = defaultRotation;
    }

    public abstract void DoDefaultAttack();

    public IEnumerator DelayDoDefaultAttack()
    {
        yield return new WaitForSeconds((AnimationTimes.Instance.DefaultWeaponAttackTime(unit.rightHeldItem.itemData.Item() as Weapon) / 2f) + 0.05f);
        DoDefaultAttack();
    }

    public ItemData ItemData() => itemData;
}