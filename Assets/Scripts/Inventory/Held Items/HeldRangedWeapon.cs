using System.Collections;
using UnityEngine;

public class HeldRangedWeapon : HeldItem
{
    [Header("Line Renderer")]
    [SerializeField] BowLineRenderer bowLineRenderer;
    [SerializeField] LineRenderer lineRenderer;

    public Projectile loadedProjectile { get; private set; }
    public bool isLoaded { get; private set; }

    public override void DoDefaultAttack()
    {
        Unit targetUnit = unit.unitActionHandler.targetEnemyUnit;

        // Setup the delegate that gets the targetUnit to stop blocking once the projectile lands (if they were blocking)
        loadedProjectile.AddDelegate(delegate { Projectile_OnProjectileBehaviourComplete(targetUnit); });

        // The targetUnit tries to block and if they're successful, the weapon/shield they blocked with is added as a corresponding Value in the attacking Unit's targetUnits dictionary
        bool attackBlocked = targetUnit.unitActionHandler.TryBlockRangedAttack(unit);
        unit.unitActionHandler.targetUnits.TryGetValue(targetUnit, out HeldItem itemBlockedWith);

        if (attackBlocked)
        {
            // Target Unit rotates towards this Unit & does block animation, moving shield in path of Projectile
            targetUnit.unitActionHandler.GetAction<TurnAction>().RotateTowards_Unit(unit, false);
            if (targetUnit.CharacterEquipment.ShieldEquipped())
                targetUnit.unitMeshManager.GetHeldShield().RaiseShield();
        }

        isLoaded = false;
        bowLineRenderer.StringStartFollowingTargetPositions();
        anim.Play("Shoot");

        StartCoroutine(RotateRangedWeapon(targetUnit.gridPosition));
    }

    public void LoadProjectile()
    {
        if (unit.CharacterEquipment.HasValidAmmunitionEquipped() == false)
            return;

        Projectile projectile = ProjectilePool.Instance.GetProjectileFromPool();
        ItemData projectileItemData = unit.CharacterEquipment.GetEquippedProjectile(itemData.Item.RangedWeapon.ProjectileType);
        projectile.Setup(projectileItemData, unit, bowLineRenderer.GetStringCenterTarget());

        // Subtract 1 from the item data's stack size and remove the item from its inventory/equipment if its stack size becomes 0
        unit.CharacterEquipment.OnReloadProjectile(projectileItemData);

        loadedProjectile = projectile;
        isLoaded = true;
    }

    public void UnloadProjectile()
    {
        if (unit.TryAddItemToInventories(loadedProjectile.ItemData) == false)
            DropItemManager.DropItem(unit, null, loadedProjectile.ItemData);

        loadedProjectile.Disable();
        loadedProjectile = null;
        isLoaded = false;

        ActionSystemUI.UpdateActionVisuals();
    }

    void Projectile_OnProjectileBehaviourComplete(Unit targetUnit)
    {
        if (targetUnit != null && targetUnit.health.IsDead() == false)
            targetUnit.unitAnimator.StopBlocking();
    }

    public void ShootProjectile()
    {
        StartCoroutine(loadedProjectile.ShootProjectile_AtTargetUnit(unit.unitActionHandler.targetEnemyUnit, unit.unitActionHandler.GetAction<ShootAction>().MissedTarget()));
        loadedProjectile = null;
    }

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
        float distanceXZ = TacticsPathfindingUtilities.CalculateWorldSpaceDistance_XZ(unit.gridPosition, targetGridPosition);
        float distanceY = unit.gridPosition.y - targetGridPosition.y;
        float rotateFactor = 5f;

        float zRotation = distanceXZ * rotateFactor;
        zRotation += distanceY * rotateFactor;

        float maxZRotation = 60f;
        zRotation = Mathf.Clamp(zRotation, -maxZRotation, maxZRotation);

        // Debug.Log("Z Rotation: " + -zRotation);
        return -zRotation;
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
