using System.Collections;
using UnityEngine;

public class HeldRangedWeapon : HeldItem
{
    [SerializeField] BowLineRenderer bowLineRenderer;

    Projectile loadedProjectile;
    public bool isLoaded { get; private set; }
    public bool attackBlocked { get; private set; }

    public override void DoDefaultAttack(bool attackBlocked, HeldItem itemBlockedWith)
    {
        Unit targetUnit = unit.unitActionHandler.targetEnemyUnit;
        this.attackBlocked = attackBlocked;

        if (attackBlocked)
        {
            // Target Unit rotates towards this Unit & does block animation, moving shield in path of Projectile
            StartCoroutine(targetUnit.unitActionHandler.GetAction<TurnAction>().RotateTowards_AttackingTargetUnit(unit, false));
            if (targetUnit.ShieldEquipped())
                targetUnit.GetShield().RaiseShield();
        }

        isLoaded = false;
        bowLineRenderer.StringStartFollowingTargetPositions();
        anim.Play("Shoot");

        StartCoroutine(RotateRangedWeapon(targetUnit.gridPosition));
    }

    public void LoadProjectile()
    {
        Projectile projectile = ProjectilePool.Instance.GetProjectileFromPool();
        projectile.Setup(ProjectilePool.Instance.Arrow_SO(), unit, bowLineRenderer.GetStringCenterTarget(), null);
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
        Quaternion defaultRotation = Quaternion.Euler(IdleRotation());
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
        Quaternion targetRotation = Quaternion.Euler(0f, IdleRotation().y, CalculateZRotation(targetGridPosition));
        float rotateSpeed = 5f;

        while (shootAction.isShooting)
        {
            transform.localRotation = Quaternion.Slerp(transform.localRotation, targetRotation, rotateSpeed * Time.deltaTime);
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

    public float MaxRange(GridPosition shooterGridPosition, GridPosition targetGridPosition)
    {
        float maxRange = itemData.item.Weapon().maxRange;
        maxRange += shooterGridPosition.y - targetGridPosition.y;
        if (maxRange < 0f) maxRange = 0f;
        return maxRange;
    }

    public void ResetAttackBlocked() => attackBlocked = false; 
}
