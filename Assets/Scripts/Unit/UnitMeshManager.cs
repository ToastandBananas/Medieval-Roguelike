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

    [Header("Meshes")]
    [SerializeField] Mesh baseMesh;

    public HeldItem leftHeldItem { get; private set; }
    public HeldItem rightHeldItem { get; private set; }

    public bool meshesHidden { get; private set; }

    Unit myUnit;

    void Awake()
    {
        myUnit = GetComponent<Unit>();

        if (baseMeshRenderer != null)
            meshRenderers.Add(baseMeshRenderer);
        if(bodyMeshRenderer != null)
            meshRenderers.Add(bodyMeshRenderer);
        if (headMeshRenderer != null)
            meshRenderers.Add(headMeshRenderer);
        if (hairMeshRenderer != null)
            meshRenderers.Add(hairMeshRenderer);
        if (helmMeshRenderer != null)
            meshRenderers.Add(helmMeshRenderer);
        if (tunicMeshRenderer != null)
            meshRenderers.Add(tunicMeshRenderer);
        if (bodyArmorMeshRenderer != null)
            meshRenderers.Add(bodyArmorMeshRenderer);
    }

    public void ShowMeshRenderers()
    {
        if (meshesHidden == false)
            return;

        meshesHidden = false;

        for (int i = 0; i < meshRenderers.Count; i++)
        {
            meshRenderers[i].enabled = true;
        }

        baseMeshFilter.mesh = baseMesh;

        if (leftHeldItem != null)
            leftHeldItem.ShowMeshes();

        if (rightHeldItem != null)
            rightHeldItem.ShowMeshes();
    }

    public void HideMeshRenderers()
    {
        if (meshesHidden)
            return;

        meshesHidden = true;

        for (int i = 0; i < meshRenderers.Count; i++)
        {
            meshRenderers[i].enabled = false;
        }

        baseMeshFilter.mesh = null;

        if (leftHeldItem != null)
            leftHeldItem.HideMeshes();

        if (rightHeldItem != null)
            rightHeldItem.HideMeshes();
    }

    public void SetLeftHeldItem(HeldItem heldItem) => leftHeldItem = heldItem;

    public void SetRightHeldItem(HeldItem heldItem) => rightHeldItem = heldItem;

    public HeldMeleeWeapon GetPrimaryMeleeWeapon()
    {
        if (rightHeldItem != null && rightHeldItem.ItemData.Item.IsMeleeWeapon())
            return rightHeldItem as HeldMeleeWeapon;
        else if (leftHeldItem != null && leftHeldItem.ItemData.Item.IsMeleeWeapon())
            return leftHeldItem as HeldMeleeWeapon;
        return null;
    }

    public HeldItem GetHeldItemFromItemData(ItemData itemData)
    {
        if (rightHeldItem != null && rightHeldItem.ItemData == itemData)
            return rightHeldItem;

        if (leftHeldItem != null && leftHeldItem.ItemData == itemData)
            return leftHeldItem;
        return null;
    }

    public HeldRangedWeapon GetRangedWeapon() => rightHeldItem == null ? null : rightHeldItem as HeldRangedWeapon;

    public HeldMeleeWeapon GetLeftMeleeWeapon() => leftHeldItem == null ? null : leftHeldItem as HeldMeleeWeapon;

    public HeldMeleeWeapon GetRightMeleeWeapon() => rightHeldItem == null ? null : rightHeldItem as HeldMeleeWeapon;

    public HeldShield GetShield()
    {
        if (leftHeldItem != null && leftHeldItem.ItemData.Item.IsShield())
            return leftHeldItem as HeldShield;
        else if (rightHeldItem != null && rightHeldItem.ItemData.Item.IsShield())
            return rightHeldItem as HeldShield;
        return null;
    }

    public void SetupMesh(EquipSlot equipSlot, Equipment equipment)
    {
        if (equipment == null)
            return;

        switch (equipSlot)
        {
            case EquipSlot.Helm:
                helmMeshFilter.mesh = equipment.Meshes[0];
                helmMeshRenderer.material = equipment.MeshRendererMaterials[0];
                break;
            case EquipSlot.BodyArmor:
                bodyArmorMeshFilter.mesh = equipment.Meshes[0];
                bodyArmorMeshRenderer.material = equipment.MeshRendererMaterials[0];
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
            case EquipSlot.Helm:
                helmMeshRenderer.enabled = false;
                break;
            case EquipSlot.BodyArmor:
                bodyArmorMeshRenderer.enabled = false;
                break;
        }
    }

    public void RemoveMesh(EquipSlot equipSlot)
    {
        switch (equipSlot)
        {
            case EquipSlot.Helm:
                helmMeshRenderer.material = null;
                helmMeshFilter.mesh = null;
                helmMeshRenderer.enabled = false;
                break;
            case EquipSlot.BodyArmor:
                bodyArmorMeshRenderer.material = null;
                bodyArmorMeshFilter.mesh = null;
                bodyArmorMeshRenderer.enabled = false;
                break;
        }
    }

    public void ReturnHeldItemToPool(EquipSlot equipSlot)
    {
        if (equipSlot != EquipSlot.LeftHeldItem1 && equipSlot != EquipSlot.RightHeldItem1 && equipSlot != EquipSlot.LeftHeldItem2 && equipSlot != EquipSlot.RightHeldItem2)
            return;

        if (myUnit.CharacterEquipment.EquipSlotHasItem(equipSlot) == false)
            return;

        if (equipSlot == EquipSlot.LeftHeldItem1 || equipSlot == EquipSlot.LeftHeldItem2)
        {
            if (leftHeldItem != null && leftHeldItem.itemData == myUnit.CharacterEquipment.EquippedItemDatas[(int)equipSlot])
            {
                leftHeldItem.ResetHeldItem();
                leftHeldItem = null;
            }
            else if (rightHeldItem != null && rightHeldItem.itemData == myUnit.CharacterEquipment.EquippedItemDatas[(int)equipSlot])
            {
                rightHeldItem.ResetHeldItem();
                rightHeldItem = null;
            }
        }
        else if (rightHeldItem != null)
        {
            rightHeldItem.ResetHeldItem();
            rightHeldItem = null;
        }
    }

    public Transform LeftHeldItemParent => leftHeldItemParent;

    public Transform RightHeldItemParent => rightHeldItemParent;

    public bool IsVisibleOnScreen() => bodyMeshRenderer.isVisible && meshesHidden == false && UnitManager.Instance.player.vision.IsKnown(myUnit);
}
