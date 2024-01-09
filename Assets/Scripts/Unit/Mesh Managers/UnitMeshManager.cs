using System.Collections.Generic;
using UnityEngine;
using InventorySystem;

namespace UnitSystem
{
    public class UnitMeshManager : MonoBehaviour
    {
        [SerializeField] protected Unit myUnit;

        [Header("Meshes")]
        [SerializeField] Mesh baseMesh;

        [Header("Mesh Renderers")]
        [SerializeField] MeshRenderer baseMeshRenderer;
        [SerializeField] MeshRenderer bodyMeshRenderer, headMeshRenderer;
        readonly protected List<MeshRenderer> meshRenderers = new();

        [Header("Mesh Filters")]
        [SerializeField] MeshFilter baseMeshFilter;
        [SerializeField] MeshFilter bodyMeshFilter, headMeshFilter;

        public bool MeshesHidden { get; private set; }

        protected virtual void Awake()
        {
            myUnit = GetComponent<Unit>();

            if (baseMeshRenderer != null)
                meshRenderers.Add(baseMeshRenderer);
            if (bodyMeshRenderer != null)
                meshRenderers.Add(bodyMeshRenderer);
            if (headMeshRenderer != null)
                meshRenderers.Add(headMeshRenderer);
        }

        public virtual void ShowMeshRenderers()
        {
            if (MeshesHidden == false)
                return;
            
            MeshesHidden = false;

            for (int i = 0; i < meshRenderers.Count; i++)
            {
                meshRenderers[i].enabled = true;
            }

            if (myUnit.HealthSystem.IsDead == false)
                baseMeshFilter.mesh = baseMesh;
        }

        public virtual void HideMeshRenderers()
        {
            if (MeshesHidden)
                return;

            MeshesHidden = true;

            for (int i = 0; i < meshRenderers.Count; i++)
                meshRenderers[i].enabled = false;

            baseMeshFilter.mesh = null;
        }

        public virtual void SetupWearableMesh(EquipSlot equipSlot, Item_VisibleArmor wearable) { }

        protected void AssignMeshAndMaterials(MeshFilter meshFilter, MeshRenderer meshRenderer, Item_VisibleArmor wearable)
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

        public virtual HeldItem LeftHeldItem => null;
        public virtual HeldItem RightHeldItem => null;

        public virtual HeldItem GetHeldItemFromItemData(ItemData itemData) => null;
        public virtual HeldMeleeWeapon GetPrimaryHeldMeleeWeapon() => null;
        public virtual HeldRangedWeapon GetHeldRangedWeapon() => null;
        public virtual HeldMeleeWeapon GetLeftHeldMeleeWeapon() => null;
        public virtual HeldMeleeWeapon GetRightHeldMeleeWeapon() => null;

        public virtual HeldShield GetHeldShield() => null;

        public virtual void HideMesh(EquipSlot equipSlot) { }

        public virtual void RemoveMesh(EquipSlot equipSlot) { }

        public void DisableBaseMeshRenderer() => baseMeshFilter.mesh = null;

        public MeshRenderer BodyMeshRenderer => bodyMeshRenderer;

        public bool IsVisibleOnScreen => !MeshesHidden && bodyMeshRenderer.isVisible && UnitManager.player.Vision.IsKnown(myUnit);

        public UnitMeshManager_Humanoid HumanoidMeshManager => this as UnitMeshManager_Humanoid;
    }
}
