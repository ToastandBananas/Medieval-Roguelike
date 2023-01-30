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

    public Vector3 IdlePosition() => idlePosition;

    public Vector3 IdleRotation() => idleRotation;

    public abstract void DoDefaultAttack();

    public IEnumerator DelayDoDefaultAttack()
    {
        yield return new WaitForSeconds(0.5f);
        DoDefaultAttack();
    }
}