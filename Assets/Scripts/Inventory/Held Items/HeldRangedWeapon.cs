using System.Collections;
using UnityEngine;
using GridSystem;
using UnitSystem;
using UnitSystem.ActionSystem;
using UnitSystem.ActionSystem.UI;
using Utilities;

namespace InventorySystem
{
    public class HeldRangedWeapon : HeldItem
    {
        [Header("Line Renderer")]
        [SerializeField] BowLineRenderer bowLineRenderer;
        [SerializeField] LineRenderer lineRenderer;

        public Projectile loadedProjectile { get; private set; }
        public bool isLoaded { get; private set; }

        public override void DoDefaultAttack(GridPosition targetGridPosition)
        {
            // Setup the delegate that gets the targetUnit to stop blocking once the projectile lands (if they were blocking)
            Unit targetEnemyUnit = unit.unitActionHandler.targetEnemyUnit;
            loadedProjectile.AddDelegate(delegate { Projectile_OnProjectileBehaviourComplete(targetEnemyUnit); });

            isLoaded = false;
            bowLineRenderer.StringStartFollowingTargetPositions();
            anim.Play("Shoot");

            StartCoroutine(RotateRangedWeapon(unit.unitActionHandler.targetEnemyUnit.GridPosition));
        }

        public void LoadProjectile(ItemData projectileItemData)
        {
            if (isLoaded || unit.UnitEquipment.HasValidAmmunitionEquipped() == false)
                return;

            Projectile projectile = ProjectilePool.Instance.GetProjectileFromPool();
            if (projectileItemData == null)
                projectileItemData = unit.UnitEquipment.GetEquippedProjectile(itemData.Item.RangedWeapon.ProjectileType);
            
            projectile.Setup(projectileItemData, unit, bowLineRenderer.GetStringCenterTarget());

            // Subtract 1 from the item data's stack size and remove the item from its inventory/equipment if its stack size becomes 0
            if (projectileItemData.MyInventory != null)
                projectileItemData.MyInventory.OnReloadProjectile(projectileItemData);
            else
                unit.UnitEquipment.OnReloadProjectile(projectileItemData);

            loadedProjectile = projectile;
            isLoaded = true;
        }

        public void UnloadProjectile()
        {
            if (unit.UnitEquipment.TryAddToEquippedAmmunition(loadedProjectile.ItemData) == false)
            {
                if (unit.UnitInventoryManager.TryAddItemToInventories(loadedProjectile.ItemData) == false)
                    DropItemManager.DropItem(null, unit, loadedProjectile.ItemData);
            }

            RemoveProjectile();

            ActionSystemUI.UpdateActionVisuals();
        }

        public void RemoveProjectile()
        {
            if (loadedProjectile != null)
            {
                loadedProjectile.Disable();
                loadedProjectile = null;
            }

            isLoaded = false;
        }

        void Projectile_OnProjectileBehaviourComplete(Unit targetUnit)
        {
            if (targetUnit != null && targetUnit.health.IsDead == false)
                targetUnit.unitAnimator.StopBlocking();
        }

        // Used in keyframe animation
        void ShootProjectile()
        {
            ShootAction shootAction = unit.unitActionHandler.GetAction<ShootAction>();
            unit.StartCoroutine(loadedProjectile.ShootProjectile_AtTargetUnit(unit.unitActionHandler.targetEnemyUnit, shootAction, shootAction.TryHitTarget(unit.unitActionHandler.targetEnemyUnit.GridPosition)));
            loadedProjectile = null;

            TryFumbleHeldItem();
        }

        // Used in animation keyframe
        public override IEnumerator ResetToIdleRotation()
        {
            Quaternion defaultRotation = Quaternion.Euler(itemData.Item.HeldEquipment.IdleRotation_RightHand);
            Quaternion startRotation = transform.parent.localRotation;
            float time = 0f;
            float duration = 0.25f;
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

            while (unit.unitActionHandler.isAttacking)
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
            RangedWeapon weapon = itemData.Item as RangedWeapon;

            float fumbleChance = (0.5f - (unit.stats.WeaponSkill(weapon) / 100f)) * 0.4f; // Weapon skill modifier
            fumbleChance += weapon.Weight / unit.stats.Strength.GetValue() / 100f * 15f; // Weapon weight to strength ratio modifier

            if (fumbleChance < 0f)
                fumbleChance = 0f;

            // Debug.Log(unit.name + " fumble chance: " + fumbleChance);
            return fumbleChance;
        }

        public float MaxRange(GridPosition shooterGridPosition, GridPosition targetGridPosition, bool accountForHeight)
        {
            if (accountForHeight == false)
                return itemData.Item.Weapon.MaxRange;

            float maxRange = itemData.Item.Weapon.MaxRange + (shooterGridPosition.y - targetGridPosition.y);
            if (maxRange < 0f) maxRange = 0f;
            return maxRange;
        }

        public override void HideMeshes()
        {
            base.HideMeshes();

            lineRenderer.enabled = false;
            if (loadedProjectile != null)
                loadedProjectile.MeshRenderer.enabled = false;
        }

        public override void ShowMeshes()
        {
            base.ShowMeshes();

            lineRenderer.enabled = true;
            if (loadedProjectile != null)
                loadedProjectile.MeshRenderer.enabled = true;
        }

        public override void ResetHeldItem()
        {
            if (loadedProjectile != null)
                UnloadProjectile();

            base.ResetHeldItem();
        }
    }
}
