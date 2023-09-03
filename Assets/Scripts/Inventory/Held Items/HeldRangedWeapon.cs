using System.Collections;
using UnityEngine;

public class HeldRangedWeapon : HeldItem
{
    [Header("Line Renderer")]
    [SerializeField] BowLineRenderer bowLineRenderer;

    public Projectile loadedProjectile { get; private set; }
    public bool isLoaded { get; private set; }

    public override void DoDefaultAttack()
    {
        Unit targetUnit = unit.unitActionHandler.targetEnemyUnit;

        // The targetUnit tries to block and if they're successful, the weapon/shield they blocked with is added as a corresponding Value in the attacking Unit's targetUnits dictionary
        bool attackBlocked = targetUnit.unitActionHandler.TryBlockRangedAttack(unit);
        unit.unitActionHandler.targetUnits.TryGetValue(targetUnit, out HeldItem itemBlockedWith);

        if (attackBlocked)
        {
            // Target Unit rotates towards this Unit & does block animation, moving shield in path of Projectile
            targetUnit.unitActionHandler.GetAction<TurnAction>().RotateTowards_Unit(unit, false);
            if (targetUnit.CharacterEquipment().ShieldEquipped())
                targetUnit.unitMeshManager.GetShield().RaiseShield();
        }

        isLoaded = false;
        bowLineRenderer.StringStartFollowingTargetPositions();
        anim.Play("Shoot");

        StartCoroutine(RotateRangedWeapon(targetUnit.gridPosition));
    }

    public void LoadProjectile()
    {
        Projectile projectile = ProjectilePool.Instance.GetProjectileFromPool();
        projectile.Setup(projectile.ItemData(), unit, bowLineRenderer.GetStringCenterTarget(), null);
        loadedProjectile = projectile;
        isLoaded = true;
    }

    public void ShootProjectile()
    {
        StartCoroutine(loadedProjectile.ShootProjectile_AtTargetUnit(unit.unitActionHandler.targetEnemyUnit, unit.unitActionHandler.GetAction<ShootAction>().MissedTarget()));
        loadedProjectile = null;
    }

    public override IEnumerator ResetToIdleRotation()
    {
        Quaternion defaultRotation = Quaternion.Euler(Vector3.zero);
        Quaternion startRotation = transform.localRotation;
        float time = 0f;
        float duration = 0.25f;
        while (time < duration)
        {
            transform.localRotation = Quaternion.Slerp(startRotation, defaultRotation, time / duration);
            yield return null;
            time += Time.deltaTime;
        }

        transform.localRotation = defaultRotation;
    }

    IEnumerator RotateRangedWeapon(GridPosition targetGridPosition)
    {
        ShootAction shootAction = unit.unitActionHandler.GetAction<ShootAction>();
        Vector3 startRotation = transform.parent.localEulerAngles;
        Quaternion targetRotation = Quaternion.Euler(startRotation.x, startRotation.y, CalculateZRotation(targetGridPosition));
        float rotateSpeed = 5f;

        while (shootAction.isShooting)
        {
            transform.parent.localRotation = Quaternion.Slerp(transform.parent.localRotation, targetRotation, rotateSpeed * Time.deltaTime);
            yield return null;
        }

        transform.localRotation = targetRotation;
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
            return itemData.Item().Weapon().maxRange;

        float maxRange = itemData.Item().Weapon().maxRange + (shooterGridPosition.y - targetGridPosition.y);
        if (maxRange < 0f) maxRange = 0f;
        return maxRange;
    }

    public override void SetUpMesh()
    {
        
    }
}
