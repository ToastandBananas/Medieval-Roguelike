using System.Collections;
using UnityEngine;

// If a class is abstract, this means that the class cannot be placed on a GameObject
public abstract class HeldItem : MonoBehaviour
{
    [Header("Mesh Components")]
    [SerializeField] MeshRenderer meshRenderer;
    [SerializeField] MeshFilter meshFilter;

    public ItemData itemData { get; private set; }

    public Animator anim { get; private set; }

    protected Unit unit;

    void Awake()
    {
        anim = GetComponent<Animator>();
    }

    void SetUnit(Unit unit) => this.unit = unit;

    public virtual IEnumerator ResetToIdleRotation()
    {
        Quaternion defaultRotation;
        if (this == unit.unitMeshManager.leftHeldItem)
            defaultRotation = Quaternion.Euler(itemData.Item().Weapon().IdleRotation_LeftHand);
        else
            defaultRotation = Quaternion.Euler(itemData.Item().Weapon().IdleRotation_RightHand);

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
        yield return new WaitForSeconds((AnimationTimes.Instance.DefaultWeaponAttackTime(unit.unitMeshManager.rightHeldItem.itemData.Item() as Weapon) / 2f) + 0.05f);
        DoDefaultAttack();
    }

    public ItemData ItemData() => itemData;

    public void SetupHeldItem(ItemData itemData, Unit unit, EquipSlot equipSlot)
    {
        this.itemData = itemData;
        this.unit = unit;

        if (equipSlot == EquipSlot.RightHeldItem || (itemData.Item().IsWeapon() && itemData.Item().Weapon().isTwoHanded))
        {
            transform.parent = unit.unitMeshManager.RightHeldItemParent;
            transform.parent.localPosition = itemData.Item().Weapon().IdlePosition_RightHand;
            transform.parent.localRotation = Quaternion.Euler(itemData.Item().Weapon().IdleRotation_RightHand);
            unit.unitMeshManager.SetRightHeldItem(this);
        }
        else
        {
            transform.parent = unit.unitMeshManager.LeftHeldItemParent;
            transform.parent.localPosition = itemData.Item().Weapon().IdlePosition_LeftHand;
            transform.parent.localRotation = Quaternion.Euler(itemData.Item().Weapon().IdleRotation_LeftHand);
            unit.unitMeshManager.SetLeftHeldItem(this);
        }

        SetUpMesh();

        transform.localPosition = Vector3.zero;
        gameObject.SetActive(true);
    }

    public virtual void SetUpMesh()
    {
        if (unit.IsPlayer() || unit.unitMeshManager.IsVisibleOnScreen())
            meshFilter.mesh = itemData.Item().meshes[0];
        else
            HideMesh();

        meshRenderer.material = itemData.Item().meshRendererMaterials[0];
    }

    public void HideMesh() => meshFilter.mesh = null;
}