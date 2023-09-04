using System.Collections;
using UnityEngine;

// If a class is abstract, this means that the class cannot be placed on a GameObject
public abstract class HeldItem : MonoBehaviour
{
    [Header("Mesh Components")]
    [SerializeField] protected MeshRenderer[] meshRenderers;
    [SerializeField] protected MeshFilter[] meshFilters;

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
            transform.SetParent(unit.unitMeshManager.RightHeldItemParent);
            transform.parent.localPosition = itemData.Item().Weapon().IdlePosition_RightHand;
            transform.parent.localRotation = Quaternion.Euler(itemData.Item().Weapon().IdleRotation_RightHand);
            unit.unitMeshManager.SetRightHeldItem(this);
        }
        else
        {
            transform.SetParent(unit.unitMeshManager.LeftHeldItemParent);
            transform.parent.localPosition = itemData.Item().Weapon().IdlePosition_LeftHand;
            transform.parent.localRotation = Quaternion.Euler(itemData.Item().Weapon().IdleRotation_LeftHand);
            unit.unitMeshManager.SetLeftHeldItem(this);
        }

        SetUpMeshes();

        transform.localPosition = Vector3.zero;
        transform.localRotation = Quaternion.Euler(Vector3.zero);
        transform.localScale = Vector3.one;

        gameObject.SetActive(true);
    }

    public void SetUpMeshes()
    {
        for (int i = 0; i < meshRenderers.Length; i++)
        {
            if (unit.IsPlayer() || unit.unitMeshManager.IsVisibleOnScreen())
                meshFilters[i].mesh = itemData.Item().meshes[i];
            else
            {
                HideMeshes();
                break;
            }

            meshRenderers[i].material = itemData.Item().meshRendererMaterials[i];
        }
    }

    public void HideMeshes()
    {
        for (int i = 0; i < meshRenderers.Length; i++)
        {
            meshFilters[i].mesh = null;
        }
    }
}