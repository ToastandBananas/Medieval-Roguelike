using System.Collections;
using UnityEngine;

public class HeldRangedWeapon : HeldItem
{
    [SerializeField] BowLineRenderer bowLineRenderer;

    Projectile loadedProjectile;
    public bool isLoaded { get; private set; }

    public override void DoDefaultAttack()
    {
        isLoaded = false;
        bowLineRenderer.StringStartFollowingTargetPositions();
        anim.Play("Shoot");

        StartCoroutine(RotateRangedWeapon(unit.unitActionHandler.targetEnemyUnit.gridPosition));
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
        // Something here is Null sometimes. Fix me!
        // Debug.Log("Projectile: " + loadedProjectile.name);
        // Debug.Log("Target Enemy: " + unit.unitActionHandler.targetEnemyUnit.name);
        StartCoroutine(loadedProjectile.ShootProjectile_AtTargetUnit(unit.unitActionHandler.targetEnemyUnit));
        loadedProjectile = null;
    }

    IEnumerator RotateRangedWeapon(GridPosition targetGridPosition)
    {
        Quaternion targetRotation = Quaternion.Euler(0f, IdleRotation().y, CalculateZRotation(targetGridPosition));
        float rotateSpeed = 5f;
        while (unit.unitActionHandler.GetAction<ShootAction>().isShooting)
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
}
