using System.Collections;
using UnityEngine;
using UnitSystem;
using GridSystem;
using InteractableObjects;
using UnitSystem.ActionSystem;

namespace InventorySystem
{
    public enum HeldItemStance { Default, Versatile, RaiseShield, SpearWall }

    public abstract class HeldItem : MonoBehaviour
    {
        [Header("Mesh Components")]
        [SerializeField] protected MeshRenderer[] meshRenderers;
        [SerializeField] protected MeshFilter[] meshFilters;

        public HeldItemStance CurrentHeldItemStance { get; protected set; }

        public Animator Anim { get; private set; }
        public ItemData ItemData { get; private set; }

        public bool IsBlocking { get; protected set; }

        protected Unit unit;

        readonly Vector3 femaleHeldItemOffset = new(0f, 0.02f, 0f);

        void Awake()
        {
            Anim = GetComponent<Animator>();
        }

        // Used in animation keyframe
        public virtual IEnumerator ResetToIdleRotation()
        {
            Quaternion defaultRotation;
            if (this == unit.unitMeshManager.leftHeldItem)
                defaultRotation = Quaternion.Euler(ItemData.Item.HeldEquipment.IdleRotation_LeftHand);
            else
                defaultRotation = Quaternion.Euler(ItemData.Item.HeldEquipment.IdleRotation_RightHand);

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

        public void StartThrow()
        {
            if (ItemData.Item is Weapon && ItemData.Item.Weapon.IsTwoHanded)
                Anim.CrossFadeInFixedTime("Throw_Spear_2H", 0.1f);
            else
                Anim.CrossFadeInFixedTime("Throw_Spear", 0.1f);
        }

        ///<summary>Only used in keyframe animations.</summary>
        protected void Throw()
        {
            ThrowAction throwAction = unit.unitActionHandler.GetAction<ThrowAction>();
            if (throwAction == null)
            {
                if (unit.IsPlayer)
                    unit.unitActionHandler.PlayerActionHandler.SetDefaultSelectedAction();
                unit.unitActionHandler.FinishAction();
                TurnManager.Instance.StartNextUnitsTurn(unit);
                Anim.CrossFadeInFixedTime("Idle", 0.1f);
                return;
            }

            Projectile projectile = ProjectilePool.Instance.GetProjectileFromPool();
            projectile.SetupThrownItem(throwAction.ItemDataToThrow, unit, transform);
            projectile.AddDelegate(delegate { Projectile_OnProjectileBehaviourComplete(throwAction.TargetEnemyUnit); });

            bool hitTarget = throwAction.TryHitTarget(projectile.ItemData, throwAction.targetGridPosition);
            unit.StartCoroutine(projectile.ShootProjectile_AtTargetUnit(throwAction.TargetEnemyUnit, throwAction, hitTarget));

            if (unit.UnitEquipment.ItemDataEquipped(throwAction.ItemDataToThrow))
            {
                //unit.UnitEquipment.GetEquipmentSlot(unit.UnitEquipment.GetEquipSlotFromItemData(throwAction.ItemDataToThrow)).ClearItem
                unit.UnitEquipment.RemoveEquipment(throwAction.ItemDataToThrow);
            }
        }

        protected void Projectile_OnProjectileBehaviourComplete(Unit targetUnit)
        {
            if (targetUnit != null && !targetUnit.health.IsDead)
                targetUnit.unitAnimator.StopBlocking();
        }

        public void TryFumbleHeldItem()
        {
            if (Random.Range(0f, 1f) <= GetFumbleChance())
            {
                if (unit.UnitEquipment.ItemDataEquipped(ItemData) == false)
                    return;

                unit.unitActionHandler.SetIsAttacking(false);

                if (unit.IsNPC && unit.health.IsDead == false) // NPCs will try to pick the item back up immediately
                {
                    Unit myUnit = unit; // unit will become null after dropping, so we need to create a reference to it in order to queue the IntaractAction
                    LooseItem looseItem = DropItemManager.DropItem(myUnit.UnitEquipment, myUnit.UnitEquipment.GetEquipSlotFromItemData(ItemData));
                    myUnit.unitActionHandler.ClearActionQueue(true);
                    myUnit.unitActionHandler.interactAction.QueueAction(looseItem);
                }
                else
                    DropItemManager.DropItem(unit.UnitEquipment, unit.UnitEquipment.GetEquipSlotFromItemData(ItemData));
            }
        }

        protected abstract float GetFumbleChance();

        public virtual void SetupHeldItem(ItemData itemData, Unit unit, EquipSlot equipSlot)
        {
            ItemData = itemData;
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

            transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.Euler(Vector3.zero));
            transform.localScale = Vector3.one;

            gameObject.SetActive(true);
            Anim.Play("Idle");
        }

        protected void UpdateActionIcons()
        {
            if (unit.IsPlayer)
            {
                for (int i = 0; i < ItemData.Item.Equipment.ActionTypes.Length; i++)
                {
                    BaseAction baseAction = unit.unitActionHandler.GetActionFromType(ItemData.Item.Equipment.ActionTypes[i]);
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
                        if (j > ItemData.Item.HeldEquipment.MeshRendererMaterials.Length - 1)
                            materials[j] = ItemData.Item.HeldEquipment.MeshRendererMaterials[ItemData.Item.HeldEquipment.MeshRendererMaterials.Length - 1];
                        else
                            materials[j] = ItemData.Item.HeldEquipment.MeshRendererMaterials[j];
                    }

                    meshRenderers[i].materials = materials;
                }
                else // For items like the bow that consist of multiple meshes
                    meshRenderers[i].material = ItemData.Item.HeldEquipment.MeshRendererMaterials[i];

                meshFilters[i].mesh = ItemData.Item.HeldEquipment.Meshes[i];
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

        public void SetDefaultWeaponStance()
        {
            if (CurrentHeldItemStance == HeldItemStance.Versatile)
                CurrentHeldItemStance = HeldItemStance.Default;
            Anim.SetBool("versatileStance", false);
        }

        public void SetVersatileWeaponStance()
        {
            if (CurrentHeldItemStance == HeldItemStance.Default)
                CurrentHeldItemStance = HeldItemStance.Versatile;
            Anim.SetBool("versatileStance", true);
        }

        public void SetHeldItemStance(HeldItemStance heldItemStance) => CurrentHeldItemStance = heldItemStance;

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
            ItemData = null;
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
