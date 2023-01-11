using UnityEngine;

// If a class is abstract, this means that the class cannot be placed on a GameObject
public abstract class HeldItem : MonoBehaviour
{
    [SerializeField] Vector3 idlePosition = new Vector3(-0.23f, -0.3f, -0.23f);
    [SerializeField] Vector3 idleRotation = new Vector3(0f, 90f, 0f);

    [SerializeField] protected Animator anim;

    [SerializeField] int damage = 10;

    protected Unit myUnit;

    void Awake()
    {
        SetItemPosition();
        SetItemRotation();

        SetUnit();
    }

    void SetUnit() => myUnit = transform.parent.parent.parent.parent.parent.GetComponent<Unit>();

    void SetItemPosition() => transform.parent.localPosition = idlePosition;

    void SetItemRotation() => transform.localEulerAngles = idleRotation;

    public Vector3 IdlePosition() => idlePosition;

    public Vector3 IdleRotation() => idleRotation;

    public Animator HeldItemAnimator() => anim;

    public void SetDamage(int newDamageAmount) => damage = newDamageAmount;

    public int GetDamage() => damage;

    public abstract void DoDefaultAttack();

    public abstract void SetupBaseActions();

    public abstract void RemoveHeldItem();
}
