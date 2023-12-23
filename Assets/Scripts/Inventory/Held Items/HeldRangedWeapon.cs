using System.Collections;
using UnityEngine;
using GridSystem;
using UnitSystem;
using UnitSystem.ActionSystem.UI;
using Utilities;
using UnitSystem.ActionSystem.Actions;

namespace InventorySystem
{
    public class HeldRangedWeapon : HeldItem
    {
        [Header("Line Renderer")]
        [SerializeField] BowLineRenderer bowLineRenderer;
        [SerializeField] LineRenderer lineRenderer;

        public Projectile LoadedProjectile { get; private set; }
        public bool IsLoaded { get; private set; }

        Action_BaseRangedAttack attackActionUsed;

        public override void DoDefaultAttack(GridPosition targetGridPosition)
        {
            Debug.LogWarning("The other version of DoDefaultAttack should be used for ranged weapons. Defaulting to Shoot Action for now...");
            DoDefaultAttack(targetGridPosition, unit.UnitActionHandler.GetAction<Action_Shoot>());
        }

        public void DoDefaultAttack(GridPosition targetGridPosition, Action_BaseAttack attackActionUsed)
        {
            if (attackActionUsed == null)
            {
                TurnManager.Instance.FinishTurn(unit);
                return;
            }

            // Setup the delegate that gets the targetUnit to stop blocking once the projectile lands (if they were blocking)
            Unit targetEnemyUnit = attackActionUsed.TargetEnemyUnit;
            if (targetEnemyUnit == null)
                targetEnemyUnit = unit.UnitActionHandler.TargetEnemyUnit;

            this.attackActionUsed = attackActionUsed as Action_BaseRangedAttack;
            LoadedProjectile.AddDelegate(delegate { Projectile_OnProjectileBehaviourComplete(targetEnemyUnit); });

            IsLoaded = false;
            bowLineRenderer.StringStartFollowingTargetPositions();
            Anim.Play("Shoot");

            StartCoroutine(RotateRangedWeapon(targetGridPosition));
        }

        public void LoadProjectile(ItemData projectileItemData)
        {
            if (IsLoaded || !unit.UnitEquipment.HasValidAmmunitionEquipped())
                return;

            Projectile projectile = Pool_Projectiles.Instance.GetProjectileFromPool();
            projectileItemData ??= unit.UnitEquipment.GetEquippedProjectile(ItemData.Item.RangedWeapon.ProjectileType);
            
            projectile.SetupAmmunition(projectileItemData, unit, bowLineRenderer.GetStringCenterTarget());

            // Subtract 1 from the item data's stack size and remove the item from its inventory/equipment if its stack size becomes 0
            if (projectileItemData.MyInventory != null)
                projectileItemData.MyInventory.OnReloadProjectile(projectileItemData);
            else
                unit.UnitEquipment.OnReloadProjectile(projectileItemData);

            LoadedProjectile = projectile;
            IsLoaded = true;
        }

        public void UnloadProjectile()
        {
            if (!unit.UnitEquipment.TryAddToEquippedAmmunition(LoadedProjectile.ItemData))
            {
                if (!unit.UnitInventoryManager.TryAddItemToInventories(LoadedProjectile.ItemData))
                    DropItemManager.DropItem(null, unit, LoadedProjectile.ItemData);
            }

            RemoveProjectile();

            ActionSystemUI.UpdateActionVisuals();
        }

        public void RemoveProjectile()
        {
            if (LoadedProjectile != null)
            {
                LoadedProjectile.Disable();
                LoadedProjectile = null;
            }

            IsLoaded = false;
        }

        public void SetLoadedProjectile(Projectile projectile) => LoadedProjectile = projectile;

        /// <summary> Used in keyframe animation.</summary>
        void ShootProjectile()
        {
            LoadedProjectile.ShootProjectileAtTarget(unit.UnitActionHandler.TargetEnemyUnit, this, attackActionUsed, attackActionUsed.TryHitTarget(unit.UnitActionHandler.TargetEnemyUnit.GridPosition), false);
            // LoadedProjectile = null;
            attackActionUsed = null;

            TryFumbleHeldItem();
        }

        /// <summary> Used in keyframe animation.</summary>
        public override IEnumerator ResetToIdleRotation()
        {
            Quaternion defaultRotation = Quaternion.Euler(ItemData.Item.HeldEquipment.IdleRotation_RightHand);
            Quaternion startRotation = transform.parent.localRotation;
            float time = 0f;
            float duration = 0.25f;

            // Wait for the attack rotation to finish
            while (unit.UnitActionHandler.IsAttacking)
                yield return null;

            while (time < duration)
            {
                transform.parent.localRotation = Quaternion.Slerp(startRotation, defaultRotation, time / duration);
                yield return null;
                time += Time.deltaTime;
            }

            transform.parent.localRotation = defaultRotation;
        }

        IEnumerator RotateRangedWeapon(GridPosition targetGridPosition)
        {
            Quaternion targetRotation = Quaternion.Euler(0f, -90f, CalculateZRotation(targetGridPosition));
            float rotateSpeed = 5f;

            while (unit.UnitActionHandler.IsAttacking)
            {
                transform.parent.localRotation = Quaternion.Slerp(transform.parent.localRotation, targetRotation, rotateSpeed * Time.deltaTime);
                yield return null;
            }

            transform.parent.localRotation = targetRotation;
        }

        float CalculateZRotation(GridPosition targetGridPosition)
        {
            float distanceXZ = TacticsUtilities.CalculateDistance_XZ(unit.GridPosition, targetGridPosition);
            float distanceY = unit.GridPosition.y - targetGridPosition.y;
            float rotateFactor = 5f; // The degree to which we rotate the weapon per 1 distance

            float zRotation = distanceXZ * rotateFactor;
            zRotation += distanceY * rotateFactor;

            float maxZRotation = 60f;
            zRotation = Mathf.Clamp(zRotation, -maxZRotation, maxZRotation);

            // Debug.Log("Z Rotation: " + -zRotation);
            return -zRotation;
        }

        protected override float GetFumbleChance()
        {
            Item_RangedWeapon weapon = ItemData.Item as Item_RangedWeapon;

            float fumbleChance = (0.5f - (unit.Stats.WeaponSkill(weapon) / 100f)) * 0.4f; // Weapon skill modifier
            fumbleChance += weapon.Weight / unit.Stats.Strength.GetValue() / 100f * 15f; // Weapon weight to strength ratio modifier

            if (fumbleChance < 0f)
                fumbleChance = 0f;

            // Debug.Log(unit.name + " fumble chance: " + fumbleChance);
            return fumbleChance;
        }

        public float MaxRange(GridPosition shooterGridPosition, GridPosition targetGridPosition, bool accountForHeight)
        {
            if (!accountForHeight)
                return ItemData.Item.Weapon.MaxRange;

            float maxRange = ItemData.Item.Weapon.MaxRange + (shooterGridPosition.y - targetGridPosition.y);
            if (maxRange < 0f) maxRange = 0f;
            return maxRange;
        }

        public override void HideMeshes()
        {
            base.HideMeshes();

            lineRenderer.enabled = false;
            if (LoadedProjectile != null)
                LoadedProjectile.MeshRenderer.enabled = false;
        }

        public override void ShowMeshes()
        {
            base.ShowMeshes();

            lineRenderer.enabled = true;
            if (LoadedProjectile != null)
                LoadedProjectile.MeshRenderer.enabled = true;
        }

        public override void ResetHeldItem()
        {
            if (LoadedProjectile != null)
                UnloadProjectile();

            base.ResetHeldItem();
        }
    }
}
