using System.Collections.Generic;
using UnityEngine;

public class UnitMeshManager : MonoBehaviour
{
    [Header("Parent Transforms")]
    [SerializeField] Transform leftHeldItemParent;
    [SerializeField] Transform rightHeldItemParent;

    [Header("Mesh Renderers")]
    [SerializeField] MeshRenderer baseMesh;
    [SerializeField] MeshRenderer bodyMesh, headMesh, hairMesh, helmMesh, tunicMesh, bodyArmorMesh;
    List<MeshRenderer> meshRenderers = new List<MeshRenderer>();

    [Header("Mesh Filters")]
    [SerializeField] MeshFilter baseMeshFilter;
    [SerializeField] MeshFilter bodyMeshFilter, headMeshFilter, hairMeshFilter, helmMeshFilter, tunicMeshFilter, bodyArmorMeshFilter;

    public HeldItem leftHeldItem { get; private set; }
    public HeldItem rightHeldItem { get; private set; }

    MeshRenderer leftHeldItemMeshRenderer, rightHeldItemMeshRenderer;
    MeshRenderer[] bowMeshRenderers;
    LineRenderer bowLineRenderer;

    public bool meshesHidden { get; private set; }

    Unit myUnit;

    void Awake()
    {
        myUnit = GetComponent<Unit>();

        SetLeftHeldItem();
        SetRightHeldItem();

        if (baseMesh != null)
            meshRenderers.Add(baseMesh);
        else if(bodyMesh != null)
            meshRenderers.Add(bodyMesh);
        else if (headMesh != null)
            meshRenderers.Add(headMesh);
        else if (hairMesh != null)
            meshRenderers.Add(hairMesh);
        else if (helmMesh != null)
            meshRenderers.Add(helmMesh);
        else if (tunicMesh != null)
            meshRenderers.Add(tunicMesh);
        else if (bodyArmorMesh != null)
            meshRenderers.Add(bodyArmorMesh);
    }

    void Start()
    {
        SetHeldItemMeshRenderers();
    }

    public void SetHeldItemMeshRenderers()
    {
        if (leftHeldItem != null)
        {
            if (leftHeldItem.ItemData().Item().itemType == ItemType.RangedWeapon) // The item is a Bow
            {
                bowMeshRenderers = leftHeldItem.GetComponentsInChildren<MeshRenderer>();
                bowLineRenderer = leftHeldItem.GetComponentInChildren<LineRenderer>();
            }
            else
                leftHeldItemMeshRenderer = leftHeldItem.GetComponentInChildren<MeshRenderer>();
        }

        if (rightHeldItem != null)
            rightHeldItemMeshRenderer = rightHeldItem.GetComponentInChildren<MeshRenderer>();
    }

    public void ShowMeshRenderers()
    {
        if (meshesHidden == false)
            return;

        meshesHidden = false;

        if (meshRenderers != null)
        {
            for (int i = 0; i < meshRenderers.Count; i++)
            {
                meshRenderers[i].shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
            }
        }

        if (leftHeldItemMeshRenderer != null)
            leftHeldItemMeshRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;

        if (rightHeldItemMeshRenderer != null)
            rightHeldItemMeshRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;

        if (bowMeshRenderers != null)
        {
            for (int i = 0; i < bowMeshRenderers.Length; i++)
            {
                bowMeshRenderers[i].shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
            }

            if (bowLineRenderer != null)
                bowLineRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        }
    }

    public void HideMeshRenderers()
    {
        if (meshesHidden)
            return;

        meshesHidden = true;

        if (meshRenderers != null)
        {
            for (int i = 0; i < meshRenderers.Count; i++)
            {
                meshRenderers[i].shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.ShadowsOnly;
            }
        }

        if (leftHeldItemMeshRenderer != null)
            leftHeldItemMeshRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.ShadowsOnly;

        if (rightHeldItemMeshRenderer != null)
            rightHeldItemMeshRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.ShadowsOnly;

        if (bowMeshRenderers != null)
        {
            for (int i = 0; i < bowMeshRenderers.Length; i++)
            {
                bowMeshRenderers[i].shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.ShadowsOnly;
            }

            if (bowLineRenderer != null)
                bowLineRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.ShadowsOnly;
        }
    }

    public void RemoveAllWeaponRenderers()
    {
        leftHeldItemMeshRenderer = null;
        rightHeldItemMeshRenderer = null;
        bowMeshRenderers = null;
        bowLineRenderer = null;
    }

    public void SetLeftHeldItem()
    {
        if (leftHeldItemParent.childCount > 0)
        {
            leftHeldItem = leftHeldItemParent.GetChild(0).GetComponent<HeldItem>();
            myUnit.unitAnimator.SetLeftHeldItemAnim(leftHeldItem.GetComponent<Animator>());
        }
    }

    public void SetRightHeldItem()
    {
        if (rightHeldItemParent.childCount > 0)
        {
            rightHeldItem = rightHeldItemParent.GetChild(0).GetComponent<HeldItem>();
            myUnit.unitAnimator.SetRightHeldItemAnim(rightHeldItem.GetComponent<Animator>());
        }
    }

    public HeldMeleeWeapon GetPrimaryMeleeWeapon()
    {
        if (rightHeldItem != null && rightHeldItem.ItemData().Item().IsMeleeWeapon())
            return rightHeldItem as HeldMeleeWeapon;
        else if (leftHeldItem != null && leftHeldItem.ItemData().Item().IsMeleeWeapon())
            return leftHeldItem as HeldMeleeWeapon;
        return null;
    }

    public HeldItem GetHeldItemFromItemData(ItemData itemData)
    {
        if (rightHeldItem.ItemData() == itemData)
            return rightHeldItem;

        if (leftHeldItem.ItemData() == itemData)
            return leftHeldItem;
        return null;
    }

    public HeldRangedWeapon GetRangedWeapon() => leftHeldItem == null ? null : leftHeldItem as HeldRangedWeapon;

    public HeldMeleeWeapon GetLeftMeleeWeapon() => leftHeldItem == null ? null : leftHeldItem as HeldMeleeWeapon;

    public HeldMeleeWeapon GetRightMeleeWeapon() => rightHeldItem == null ? null : rightHeldItem as HeldMeleeWeapon;

    public HeldShield GetShield()
    {
        if (leftHeldItem != null && leftHeldItem.ItemData().Item().IsShield())
            return leftHeldItem as HeldShield;
        else if (rightHeldItem != null && rightHeldItem.ItemData().Item().IsShield())
            return rightHeldItem as HeldShield;
        return null;
    }


    public bool IsVisibleOnScreen() => bodyMesh.isVisible && CanSeeMeshRenderers() && UnitManager.Instance.player.vision.IsKnown(myUnit);

    public bool CanSeeMeshRenderers() => bodyMesh.shadowCastingMode == UnityEngine.Rendering.ShadowCastingMode.On;
}
