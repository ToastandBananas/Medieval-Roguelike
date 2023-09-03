using System.Collections.Generic;
using UnityEngine;

public class UnitMeshManager : MonoBehaviour
{
    [Header("Parent Transforms")]
    [SerializeField] Transform leftHeldItemParent;
    [SerializeField] Transform rightHeldItemParent;

    [Header("Mesh Renderers")]
    [SerializeField] MeshRenderer baseMeshRenderer;
    [SerializeField] MeshRenderer bodyMeshRenderer, headMeshRenderer, hairMeshRenderer, helmMeshRenderer, tunicMeshRenderer, bodyArmorMeshRenderer;
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

        if (baseMeshRenderer != null)
            meshRenderers.Add(baseMeshRenderer);
        else if(bodyMeshRenderer != null)
            meshRenderers.Add(bodyMeshRenderer);
        else if (headMeshRenderer != null)
            meshRenderers.Add(headMeshRenderer);
        else if (hairMeshRenderer != null)
            meshRenderers.Add(hairMeshRenderer);
        else if (helmMeshRenderer != null)
            meshRenderers.Add(helmMeshRenderer);
        else if (tunicMeshRenderer != null)
            meshRenderers.Add(tunicMeshRenderer);
        else if (bodyArmorMeshRenderer != null)
            meshRenderers.Add(bodyArmorMeshRenderer);
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

    public void SetLeftHeldItem(HeldItem heldItem) => leftHeldItem = heldItem;

    public void SetRightHeldItem(HeldItem heldItem) => rightHeldItem = heldItem;

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

    public void SetupMesh(EquipSlot equipSlot, Equipment equipment)
    {
        switch (equipSlot)
        {
            case EquipSlot.Helm:
                helmMeshFilter.mesh = equipment.meshes[0];
                helmMeshRenderer.material = equipment.meshRendererMaterials[0];
                break;
            case EquipSlot.BodyArmor:
                bodyArmorMeshFilter.mesh = equipment.meshes[0];
                bodyArmorMeshRenderer.material = equipment.meshRendererMaterials[0];
                break;
            default:
                break;
        }

        if (myUnit.IsPlayer() == false && IsVisibleOnScreen() == false)
            HideMesh(equipSlot);
    }

    public void HideMesh(EquipSlot equipSlot)
    {
        switch (equipSlot)
        {
            case EquipSlot.LeftHeldItem:
                leftHeldItem.HideMesh();
                break;
            case EquipSlot.RightHeldItem:
                rightHeldItem.HideMesh();
                break;
            case EquipSlot.Helm:
                helmMeshFilter.mesh = null;
                break;
            case EquipSlot.BodyArmor:
                bodyArmorMeshFilter.mesh = null;
                break;
        }
    }

    public Transform LeftHeldItemParent => leftHeldItemParent;

    public Transform RightHeldItemParent => rightHeldItemParent;

    public bool IsVisibleOnScreen() => bodyMeshRenderer.isVisible && CanSeeMeshRenderers() && UnitManager.Instance.player.vision.IsKnown(myUnit);

    public bool CanSeeMeshRenderers() => bodyMeshRenderer.shadowCastingMode == UnityEngine.Rendering.ShadowCastingMode.On;
}
