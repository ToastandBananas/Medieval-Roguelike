using System.Collections;
using UnityEngine;
using UnitSystem;
using Utilities;
using GridSystem;
using InteractableObjects;
using UnitSystem.ActionSystem.UI;
using UnitSystem.ActionSystem;

namespace InventorySystem
{
    public enum HeldItemStance { Default, Versatile, RaiseShield, SpearWall }

    public abstract class HeldItem : MonoBehaviour
    {
        [Header("Mesh Components")]
        [SerializeField] protected MeshRenderer[] meshRenderers;
        [SerializeField] protected MeshFilter[] meshFilters;

        public HeldItemStance currentHeldItemStance { get; private set; }

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

        public void TryFumbleHeldItem()
        {
            if (Random.Range(0f, 1f) <= GetFumbleChance())
            {
                if (unit.UnitEquipment.ItemDataEquipped(itemData) == false)
                    return;

                unit.unitActionHandler.SetIsAttacking(false);

                if (unit.IsNPC && unit.health.IsDead == false) // NPCs will try to pick the item back up immediately
                {
                    Unit myUnit = unit; // unit will become null after dropping, so we need to create a reference to it in order to queue the IntaractAction
                    LooseItem looseItem = DropItemManager.DropItem(myUnit.UnitEquipment, myUnit.UnitEquipment.GetEquipSlotFromItemData(itemData));
                    myUnit.unitActionHandler.ClearActionQueue(true);
                    myUnit.unitActionHandler.interactAction.QueueAction(looseItem);
                }
                else
                    DropItemManager.DropItem(unit.UnitEquipment, unit.UnitEquipment.GetEquipSlotFromItemData(itemData));
            }
        }

        protected abstract float GetFumbleChance();

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

            HeldItem oppositeHeldItem = GetOppositeHeldItem();
            if (oppositeHeldItem != null && oppositeHeldItem is HeldMeleeWeapon)
            {
                oppositeHeldItem.HeldMeleeWeapon.SetDefaultWeaponStance();
                oppositeHeldItem.UpdateActionIcons();
            }

            SetUpMeshes();

            transform.localPosition = Vector3.zero;
            transform.localRotation = Quaternion.Euler(Vector3.zero);
            transform.localScale = Vector3.one;

            gameObject.SetActive(true);
        }

        protected void UpdateActionIcons()
        {
            if (unit.IsPlayer)
            {
                for (int i = 0; i < itemData.Item.Equipment.ActionTypes.Length; i++)
                {
                    BaseAction baseAction = unit.unitActionHandler.GetActionFromType(itemData.Item.Equipment.ActionTypes[i]);
                    if (baseAction != null && baseAction.actionBarSlot != null)
                        baseAction.actionBarSlot.UpdateIcon();
                }
            }
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
                        if (j > itemData.Item.HeldEquipment.MeshRendererMaterials.Length - 1)
                            materials[j] = itemData.Item.HeldEquipment.MeshRendererMaterials[itemData.Item.HeldEquipment.MeshRendererMaterials.Length - 1];
                        else
                            materials[j] = itemData.Item.HeldEquipment.MeshRendererMaterials[j];
                    }

                    meshRenderers[i].materials = materials;
                }
                else // For items like the bow that consist of multiple meshes
                    meshRenderers[i].material = itemData.Item.HeldEquipment.MeshRendererMaterials[i];

                meshFilters[i].mesh = itemData.Item.HeldEquipment.Meshes[i];
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

        public void SwitchVersatileStance()
        {
            if (currentHeldItemStance == HeldItemStance.Versatile)
                SetDefaultWeaponStance();
            else
                SetVersatileWeaponStance();
        }

        public void SetDefaultWeaponStance()
        {
            currentHeldItemStance = HeldItemStance.Default;
            anim.SetBool("versatileStance", false);
        }

        public void SetVersatileWeaponStance()
        {
            currentHeldItemStance = HeldItemStance.Versatile;
            anim.SetBool("versatileStance", true);
        }

        public void SetHeldItemStance(HeldItemStance heldItemStance) => currentHeldItemStance = heldItemStance;

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

        public HeldItem GetOppositeHeldItem()
        {
            if (unit == null)
            {
                Debug.LogWarning($"Unit for {name} is null...");
                return null;
            }

            if (this == unit.unitMeshManager.leftHeldItem)
                return unit.unitMeshManager.rightHeldItem;
            else if (this == unit.unitMeshManager.rightHeldItem)
                return unit.unitMeshManager.leftHeldItem;
            else
                return null;
        }

        public HeldMeleeWeapon HeldMeleeWeapon => this as HeldMeleeWeapon;
        public HeldRangedWeapon HeldRangedWeapon => this as HeldRangedWeapon;
        public HeldShield HeldShield => this as HeldShield;
    }
}
