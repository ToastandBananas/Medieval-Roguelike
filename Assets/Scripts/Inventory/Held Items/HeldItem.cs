using System.Collections;
using UnityEngine;
using UnitSystem;
using GridSystem;
using InteractableObjects;
using UnitSystem.ActionSystem;
using UnitSystem.ActionSystem.Actions;

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
        public ItemData ItemData { get; protected set; }

        public bool IsBlocking { get; protected set; }

        protected Unit unit;

        readonly Vector3 defaultLeftHeldItemPosition = new(0.23f, 0f, -0.23f);
        readonly Vector3 defaultRightHeldItemPosition = new(-0.23f, 0f, -0.23f);
        readonly Vector3 defaultHeldItemRotation = new(0f, 90f, 0f);
        readonly Vector3 femaleHeldItemOffset = new(0f, 0.02f, 0f);

        void Awake()
        {
            Anim = GetComponent<Animator>();
        }

        // Used in animation keyframe
        public virtual IEnumerator ResetToIdleRotation()
        {
            Quaternion defaultRotation;
            if (this == unit.UnitMeshManager.leftHeldItem)
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
            if (ItemData.Item.ThrownProjectileType == ProjectileType.Spear)
            {
                if (ItemData.Item is Item_Weapon && ItemData.Item.Weapon.IsTwoHanded)
                    Anim.CrossFadeInFixedTime("Throw_Spear_2H", 0.1f);
                else
                    Anim.CrossFadeInFixedTime("Throw_Spear", 0.1f);
            }
            else // End-over-end animation
            {
                if (ItemData.Item is Item_Weapon && ItemData.Item.Weapon.IsTwoHanded)
                    Anim.CrossFadeInFixedTime("Throw_2H", 0.1f);
                else
                    Anim.CrossFadeInFixedTime("Throw", 0.1f);
            }
        }

        ///<summary>Only used in keyframe animations.</summary>
        protected void Throw()
        {
            Action_Throw throwAction = unit.UnitActionHandler.GetAction<Action_Throw>();
            if (throwAction == null)
            {
                if (unit.IsPlayer)
                    unit.UnitActionHandler.PlayerActionHandler.SetDefaultSelectedAction();
                unit.UnitActionHandler.FinishAction();
                TurnManager.Instance.StartNextUnitsTurn(unit);
                Anim.CrossFadeInFixedTime("Idle", 0.1f);
                return;
            }

            Projectile projectile = ProjectilePool.Instance.GetProjectileFromPool();
            projectile.SetupThrownItem(throwAction.ItemDataToThrow, unit, transform);
            projectile.AddDelegate(delegate { Projectile_OnProjectileBehaviourComplete(throwAction.TargetEnemyUnit); });

            bool hitTarget = throwAction.TryHitTarget(projectile.ItemData, throwAction.TargetGridPosition);
            projectile.ShootProjectileAtTarget(throwAction.TargetEnemyUnit, throwAction, hitTarget, true);

            throwAction.OnThrowHeldItem();
        }

        protected void Projectile_OnProjectileBehaviourComplete(Unit targetUnit)
        {
            if (targetUnit != null && !targetUnit.HealthSystem.IsDead)
                targetUnit.UnitAnimator.StopBlocking();
        }

        public void TryFumbleHeldItem()
        {
            if (Random.Range(0f, 1f) <= GetFumbleChance())
            {
                if (!unit.UnitEquipment.ItemDataEquipped(ItemData))
                    return;

                unit.UnitActionHandler.SetIsAttacking(false);

                if (unit.IsNPC && !unit.HealthSystem.IsDead) // NPCs will try to pick the item back up immediately
                {
                    Unit myUnit = unit; // unit will become null after dropping, so we need to create a reference to it in order to queue the IntaractAction
                    Interactable_LooseItem looseItem = DropItemManager.DropItem(myUnit.UnitEquipment, myUnit.UnitEquipment.GetEquipSlotFromItemData(ItemData));
                    myUnit.UnitActionHandler.ClearActionQueue(true);
                    myUnit.UnitActionHandler.InteractAction.QueueAction(looseItem);
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
            name = itemData.Item.Name;

            if (equipSlot == EquipSlot.RightHeldItem1 || equipSlot == EquipSlot.RightHeldItem2 || (itemData.Item is Item_Weapon && itemData.Item.Weapon.IsTwoHanded))
            {
                SetupTransform(itemData, unit.UnitMeshManager.RightHeldItemParent);
                unit.UnitMeshManager.SetRightHeldItem(this);
            }
            else
            {
                SetupTransform(itemData, unit.UnitMeshManager.LeftHeldItemParent);
                unit.UnitMeshManager.SetLeftHeldItem(this);
            }

            HeldItem oppositeHeldItem = GetOppositeHeldItem();
            if (oppositeHeldItem != null && oppositeHeldItem is HeldMeleeWeapon)
            {
                oppositeHeldItem.HeldMeleeWeapon.SetDefaultWeaponStance();
                oppositeHeldItem.UpdateActionIcons();
            }

            SetUpMeshes();

            gameObject.SetActive(true);
            Anim.Play("Idle");
        }

        public virtual void SetupItemToThrow(ItemData itemData, Unit unit, Transform heldItemParent)
        {
            ItemData = itemData;
            this.unit = unit;
            name = itemData.Item.Name;

            SetupTransform(itemData, heldItemParent);
            SetUpMeshes();

            gameObject.SetActive(true);
            Anim.Play("Idle");
        }

        void SetupTransform(ItemData itemData, Transform heldItemParent)
        {
            if (heldItemParent == unit.UnitMeshManager.RightHeldItemParent)
            {
                transform.SetParent(unit.UnitMeshManager.RightHeldItemParent);
                if (itemData.Item is Item_HeldEquipment)
                    transform.parent.SetLocalPositionAndRotation(itemData.Item.HeldEquipment.IdlePosition_RightHand, Quaternion.Euler(itemData.Item.HeldEquipment.IdleRotation_RightHand));
                else
                    transform.parent.SetLocalPositionAndRotation(defaultRightHeldItemPosition, Quaternion.Euler(defaultHeldItemRotation));

                if (unit.Gender == Gender.Female)
                    transform.parent.localPosition += femaleHeldItemOffset;

            }
            else if (heldItemParent == unit.UnitMeshManager.LeftHeldItemParent)
            {
                transform.SetParent(unit.UnitMeshManager.LeftHeldItemParent);
                if (itemData.Item is Item_HeldEquipment)
                    transform.parent.SetLocalPositionAndRotation(itemData.Item.HeldEquipment.IdlePosition_LeftHand, Quaternion.Euler(itemData.Item.HeldEquipment.IdleRotation_LeftHand));
                else
                    transform.parent.SetLocalPositionAndRotation(defaultLeftHeldItemPosition, Quaternion.Euler(defaultHeldItemRotation));

                if (unit.Gender == Gender.Female)
                    transform.parent.localPosition += femaleHeldItemOffset;
            }
            else
                Debug.LogWarning("HeldItemParent is not set to left or right...fix me!");

            transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.Euler(Vector3.zero));
            transform.localScale = Vector3.one;
        }

        protected void UpdateActionIcons()
        {
            if (unit.IsPlayer)
            {
                for (int i = 0; i < ItemData.Item.Equipment.ActionTypes.Length; i++)
                {
                    Action_Base baseAction = unit.UnitActionHandler.GetActionFromActionType(ItemData.Item.Equipment.ActionTypes[i]);
                    if (baseAction != null && baseAction.ActionBarSlot != null)
                        baseAction.ActionBarSlot.UpdateIcon();
                }
            }
        }

        public void SetUpMeshes()
        {
            for (int i = 0; i < meshRenderers.Length; i++)
            {
                if (ItemData.Item is Item_HeldEquipment)
                {
                    if (meshRenderers.Length == 1) // For items that have one mesh, but one or more materials (like an arrow with a metallic tip and non-metallic shaft)
                    {
                        Material[] materials = meshRenderers[i].materials;
                        for (int j = 0; j < materials.Length; j++)
                        {
                            if (j > ItemData.Item.HeldEquipment.MeshRendererMaterials.Length - 1)
                                materials[j] = ItemData.Item.HeldEquipment.MeshRendererMaterials[^1]; // Last index in array
                            else
                                materials[j] = ItemData.Item.HeldEquipment.MeshRendererMaterials[j];
                        }

                        meshRenderers[i].materials = materials;
                    }
                    else // For items like the bow that consist of multiple meshes
                        meshRenderers[i].material = ItemData.Item.HeldEquipment.MeshRendererMaterials[i];

                    meshFilters[i].mesh = ItemData.Item.HeldEquipment.Meshes[i];
                    if (unit.IsPlayer || unit.UnitMeshManager.IsVisibleOnScreen)
                        meshRenderers[i].enabled = true;
                    else
                    {
                        HideMeshes();
                        break;
                    }
                }
                else
                {
                    if (meshRenderers.Length == 1) // For items that have one mesh, but one or more materials (like an arrow with a metallic tip and non-metallic shaft)
                    {
                        Material[] materials = meshRenderers[i].materials;
                        for (int j = 0; j < materials.Length; j++)
                        {
                            if (j > ItemData.Item.PickupMeshRendererMaterials.Length - 1)
                                materials[j] = ItemData.Item.PickupMeshRendererMaterials[^1]; // Last index in array
                            else
                                materials[j] = ItemData.Item.PickupMeshRendererMaterials[j];
                        }

                        meshRenderers[i].materials = materials;
                    }
                    else // For items like the bow that consist of multiple meshes
                        meshRenderers[i].material = ItemData.Item.PickupMeshRendererMaterials[i];

                    meshFilters[i].mesh = ItemData.Item.PickupMesh;
                    if (unit.IsPlayer || unit.UnitMeshManager.IsVisibleOnScreen)
                        meshRenderers[i].enabled = true;
                    else
                    {
                        HideMeshes();
                        break;
                    }
                }
            }
        }

        public virtual void BlockAttack(Unit attackingUnit)
        { 
            // Target Unit rotates towards this Unit & does block animation with shield or weapon
            unit.UnitActionHandler.TurnAction.RotateTowards_Unit(attackingUnit, false);
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

            if (this == unit.UnitMeshManager.leftHeldItem)
                return unit.UnitMeshManager.rightHeldItem;
            else if (this == unit.UnitMeshManager.rightHeldItem)
                return unit.UnitMeshManager.leftHeldItem;
            else
                return null;
        }

        public HeldMeleeWeapon HeldMeleeWeapon => this as HeldMeleeWeapon;
        public HeldRangedWeapon HeldRangedWeapon => this as HeldRangedWeapon;
        public HeldShield HeldShield => this as HeldShield;
    }
}
