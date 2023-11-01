using System.Collections;
using UnityEngine;
using UnitSystem;
using Utilities;
using GridSystem;

namespace InventorySystem
{
    public abstract class HeldItem : MonoBehaviour
    {
        [Header("Mesh Components")]
        [SerializeField] protected MeshRenderer[] meshRenderers;
        [SerializeField] protected MeshFilter[] meshFilters;

        public Animator anim { get; private set; }
        public ItemData itemData { get; private set; }

        public bool isBlocking { get; protected set; }

        protected Unit unit;

        readonly Vector3 femaleHeldItemOffset = new Vector3(0f, 0.02f, 0f);

        void Awake()
        {
            anim = GetComponent<Animator>();
        }

        // Used in animation keyframe
        public virtual IEnumerator ResetToIdleRotation()
        {
            Quaternion defaultRotation;
            if (this == unit.unitMeshManager.leftHeldItem)
                defaultRotation = Quaternion.Euler(itemData.Item.HeldEquipment.IdleRotation_LeftHand);
            else
                defaultRotation = Quaternion.Euler(itemData.Item.HeldEquipment.IdleRotation_RightHand);

            Quaternion startRotation = transform.parent.localRotation;
            float time = 0f;
            float duration = 0.35f;
            while (time < duration)
            {
                transform.parent.localRotation = Quaternion.Slerp(startRotation, defaultRotation, time / duration);
                yield return null;
                time += Time.deltaTime;
            }

            transform.parent.localRotation = defaultRotation;
        }

        public abstract void DoDefaultAttack(GridPosition targetGridPosition);

        public IEnumerator DelayDoDefaultAttack(GridPosition targetGridPosition)
        {
            yield return new WaitForSeconds((AnimationTimes.Instance.DefaultWeaponAttackTime(unit.unitMeshManager.rightHeldItem.itemData.Item as Weapon) / 2f) + 0.05f);
            DoDefaultAttack(targetGridPosition);
        }

        public virtual void SetupHeldItem(ItemData itemData, Unit unit, EquipSlot equipSlot)
        {
            this.itemData = itemData;
            this.unit = unit;
            name = itemData.Item.name;

            if (equipSlot == EquipSlot.RightHeldItem1 || equipSlot == EquipSlot.RightHeldItem2 || (itemData.Item is Weapon && itemData.Item.Weapon.IsTwoHanded))
            {
                transform.SetParent(unit.unitMeshManager.RightHeldItemParent);
                transform.parent.localPosition = itemData.Item.HeldEquipment.IdlePosition_RightHand;
                if (unit.Gender == Gender.Female)
                    transform.parent.localPosition += femaleHeldItemOffset;

                transform.parent.localRotation = Quaternion.Euler(itemData.Item.HeldEquipment.IdleRotation_RightHand);
                unit.unitMeshManager.SetRightHeldItem(this);
            }
            else
            {
                transform.SetParent(unit.unitMeshManager.LeftHeldItemParent);
                transform.parent.localPosition = itemData.Item.HeldEquipment.IdlePosition_LeftHand;
                if (unit.Gender == Gender.Female)
                    transform.parent.localPosition += femaleHeldItemOffset;

                transform.parent.localRotation = Quaternion.Euler(itemData.Item.HeldEquipment.IdleRotation_LeftHand);
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
                if (meshRenderers.Length == 1) // For items that have one mesh, but one or more materials (like an arrow with a metallic tip and non-metallic shaft)
                {
                    Material[] materials = meshRenderers[i].materials;
                    for (int j = 0; j < materials.Length; j++)
                    {
                        if (j > itemData.Item.MeshRendererMaterials.Length - 1)
                            materials[j] = itemData.Item.MeshRendererMaterials[itemData.Item.MeshRendererMaterials.Length - 1];
                        else
                            materials[j] = itemData.Item.MeshRendererMaterials[j];
                    }

                    meshRenderers[i].materials = materials;
                }
                else // For items like the bow that consist of multiple meshes
                    meshRenderers[i].material = itemData.Item.MeshRendererMaterials[i];

                meshFilters[i].mesh = itemData.Item.Meshes[i];
                if (unit.IsPlayer || unit.unitMeshManager.IsVisibleOnScreen)
                    meshRenderers[i].enabled = true;
                else
                {
                    HideMeshes();
                    break;
                }
            }
        }

        public virtual void BlockAttack(Unit attackingUnit)
        { 
            // Target Unit rotates towards this Unit & does block animation with shield or weapon
            unit.unitActionHandler.turnAction.RotateTowards_Unit(attackingUnit, false);
        }

        public virtual void StopBlocking() { }

        public virtual void HideMeshes()
        {
            for (int i = 0; i < meshRenderers.Length; i++)
            {
                meshRenderers[i].enabled = false;
            }
        }

        public virtual void ShowMeshes()
        {
            for (int i = 0; i < meshRenderers.Length; i++)
            {
                meshRenderers[i].enabled = true;
            }
        }

        public virtual void ResetHeldItem()
        {
            unit = null;
            itemData = null;
            HeldItemBasePool.ReturnToPool(this);
        }
    }
}
