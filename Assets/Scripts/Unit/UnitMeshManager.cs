using System.Collections.Generic;
using UnityEngine;
using InventorySystem;

namespace UnitSystem
{
    public class UnitMeshManager : MonoBehaviour
    {
        [SerializeField] Unit myUnit;

        [Header("Parent Transforms")]
        [SerializeField] Transform leftHeldItemParent;
        [SerializeField] Transform rightHeldItemParent;

        [Header("Mesh Renderers")]
        [SerializeField] MeshRenderer baseMeshRenderer;
        [SerializeField] MeshRenderer bodyMeshRenderer, headMeshRenderer, hairMeshRenderer, helmMeshRenderer, shirtMeshRenderer, bodyArmorMeshRenderer;
        List<MeshRenderer> meshRenderers = new List<MeshRenderer>();

        [Header("Mesh Filters")]
        [SerializeField] MeshFilter baseMeshFilter;
        [SerializeField] MeshFilter bodyMeshFilter, headMeshFilter, hairMeshFilter, helmMeshFilter, shirtMeshFilter, bodyArmorMeshFilter;

        [Header("Meshes")]
        [SerializeField] Mesh baseMesh;

        public HeldItem leftHeldItem { get; private set; }
        public HeldItem rightHeldItem { get; private set; }

        public bool meshesHidden { get; private set; }

        void Awake()
        {
            myUnit = GetComponent<Unit>();

            if (baseMeshRenderer != null)
                meshRenderers.Add(baseMeshRenderer);
            if (bodyMeshRenderer != null)
                meshRenderers.Add(bodyMeshRenderer);
            if (headMeshRenderer != null)
                meshRenderers.Add(headMeshRenderer);
            if (hairMeshRenderer != null)
                meshRenderers.Add(hairMeshRenderer);
            if (helmMeshRenderer != null)
                meshRenderers.Add(helmMeshRenderer);
            if (shirtMeshRenderer != null)
                meshRenderers.Add(shirtMeshRenderer);
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

            if (myUnit.health.IsDead == false)
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

        public HeldMeleeWeapon GetPrimaryHeldMeleeWeapon()
        {
            if (rightHeldItem != null && rightHeldItem.itemData.Item is MeleeWeapon)
                return rightHeldItem as HeldMeleeWeapon;
            else if (leftHeldItem != null && leftHeldItem.itemData.Item is MeleeWeapon)
                return leftHeldItem as HeldMeleeWeapon;
            return null;
        }

        public HeldItem GetHeldItemFromItemData(ItemData itemData)
        {
            if (rightHeldItem != null && rightHeldItem.itemData == itemData)
                return rightHeldItem;

            if (leftHeldItem != null && leftHeldItem.itemData == itemData)
                return leftHeldItem;
            return null;
        }

        public HeldRangedWeapon GetHeldRangedWeapon()
        {
            if (rightHeldItem != null && rightHeldItem.itemData.Item is RangedWeapon)
                return rightHeldItem as HeldRangedWeapon;
            return null;
        }

        public HeldMeleeWeapon GetLeftHeldMeleeWeapon()
        {
            if (leftHeldItem != null && leftHeldItem.itemData.Item is MeleeWeapon)
                return leftHeldItem as HeldMeleeWeapon;
            return null;
        }

        public HeldMeleeWeapon GetRightHeldMeleeWeapon()
        {
            if (rightHeldItem != null && rightHeldItem.itemData.Item is MeleeWeapon)
                return rightHeldItem as HeldMeleeWeapon;
            return null;
        }

        public HeldShield GetHeldShield()
        {
            if (leftHeldItem != null && leftHeldItem.itemData.Item is Shield)
                return leftHeldItem as HeldShield;
            else if (rightHeldItem != null && rightHeldItem.itemData.Item is Shield)
                return rightHeldItem as HeldShield;
            return null;
        }

        public void SetupWearableMesh(EquipSlot equipSlot, VisibleArmor wearable)
        {
            if (wearable == null)
                return;

            switch (equipSlot)
            {
                case EquipSlot.Helm:
                    AssignMeshAndMaterials(helmMeshFilter, helmMeshRenderer, wearable);
                    break;
                case EquipSlot.BodyArmor:
                    AssignMeshAndMaterials(bodyArmorMeshFilter, bodyArmorMeshRenderer, wearable);
                    break;
                case EquipSlot.Shirt:
                    AssignMeshAndMaterials(shirtMeshFilter, shirtMeshRenderer, wearable);
                    break;
                default:
                    break;
            }

            if (myUnit.IsPlayer == false && IsVisibleOnScreen == false)
                HideMesh(equipSlot);
        }

        void AssignMeshAndMaterials(MeshFilter meshFilter, MeshRenderer meshRenderer, VisibleArmor wearable)
        {
            if (myUnit.Gender == Gender.Male)
            {
                meshFilter.mesh = wearable.Meshes_Male[0];

                Material[] materials = meshRenderer.materials;
                for (int i = 0; i < materials.Length; i++)
                {
                    if (i > wearable.MeshRendererMaterials_Male.Length - 1)
                        materials[i] = null;
                    else
                        materials[i] = wearable.MeshRendererMaterials_Male[i];
                }

                meshRenderer.materials = materials;
            }
            else // Female
            {
                if (wearable.Meshes_Female.Length > 0)
                    meshFilter.mesh = wearable.Meshes_Female[0];
                else
                    meshFilter.mesh = wearable.Meshes_Male[0];

                Material[] materials = meshRenderer.materials;
                for (int i = 0; i < materials.Length; i++)
                {
                    if (wearable.MeshRendererMaterials_Female.Length > 0)
                    {
                        if (i > wearable.MeshRendererMaterials_Female.Length - 1)
                            materials[i] = null;
                        else
                            materials[i] = wearable.MeshRendererMaterials_Female[i];
                    }
                    else
                    {
                        if (i > wearable.MeshRendererMaterials_Male.Length - 1)
                            materials[i] = null;
                        else
                            materials[i] = wearable.MeshRendererMaterials_Male[i];
                    }
                }

                meshRenderer.materials = materials;
            }
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
                case EquipSlot.Shirt:
                    shirtMeshRenderer.enabled = false;
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
                    break;
                case EquipSlot.BodyArmor:
                    bodyArmorMeshRenderer.material = null;
                    bodyArmorMeshFilter.mesh = null;
                    break;
                case EquipSlot.Shirt:
                    shirtMeshRenderer.material = null;
                    shirtMeshFilter.mesh = null;
                    break;
            }
        }

        public void DisableBaseMeshRenderer() => baseMeshFilter.mesh = null;

        public void ReturnHeldItemToPool(EquipSlot equipSlot)
        {
            if (equipSlot != EquipSlot.LeftHeldItem1 && equipSlot != EquipSlot.RightHeldItem1 && equipSlot != EquipSlot.LeftHeldItem2 && equipSlot != EquipSlot.RightHeldItem2)
                return;

            if (myUnit.UnitEquipment.EquipSlotHasItem(equipSlot) == false)
                return;

            if (equipSlot == EquipSlot.LeftHeldItem1 || equipSlot == EquipSlot.LeftHeldItem2)
            {
                if (leftHeldItem != null && leftHeldItem.itemData == myUnit.UnitEquipment.EquippedItemDatas[(int)equipSlot])
                {
                    leftHeldItem.ResetHeldItem();
                    leftHeldItem = null;
                }
                else if (rightHeldItem != null && rightHeldItem.itemData == myUnit.UnitEquipment.EquippedItemDatas[(int)equipSlot])
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

        public MeshRenderer BodyMeshRenderer => bodyMeshRenderer;
        public MeshRenderer HelmMeshRenderer => helmMeshRenderer;

        public bool IsVisibleOnScreen => meshesHidden == false && bodyMeshRenderer.isVisible && UnitManager.player.vision.IsKnown(myUnit);
    }
}
