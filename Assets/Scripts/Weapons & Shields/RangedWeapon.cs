using System;
using System.Collections;
using UnityEngine;

public class RangedWeapon : HeldItem
{
    [SerializeField] BowLineRenderer bowLineRenderer;

    Projectile loadedProjectile;
    bool isLoaded = false;

    public override void DoDefaultAttack()
    {
        if (isLoaded)
        {
            isLoaded = false;
            bowLineRenderer.StringStartFollowingTargetPositions();
            anim.Play("Shoot");

            StartCoroutine(RotateRangedWeapon(myUnit.GetAction<ShootAction>().TargetUnit().GridPosition()));
        }
    }

    public override void SetupBaseActions()
    {
        myUnit.GetAction<ShootAction>().OnStartShooting += ShootAction_OnStartShooting;
        myUnit.GetAction<ShootAction>().OnStopShooting += ShootAction_OnStopShooting;

        myUnit.GetAction<ReloadAction>().OnStartReload += ReloadAction_OnStartReload;
        myUnit.GetAction<ReloadAction>().OnStopReload += ReloadAction_OnStopReload;
    }

    public override void RemoveHeldItem()
    {
        myUnit.GetAction<ShootAction>().OnStartShooting -= ShootAction_OnStartShooting;
        myUnit.GetAction<ShootAction>().OnStopShooting -= ShootAction_OnStopShooting;

        myUnit.GetAction<ReloadAction>().OnStartReload -= ReloadAction_OnStartReload;
        myUnit.GetAction<ReloadAction>().OnStopReload -= ReloadAction_OnStopReload;
    }

    void LoadProjectile()
    {
        Projectile projectile = ProjectilePool.Instance.GetProjectileFromPool();
        projectile.Setup(ProjectilePool.Instance.Arrow_SO(), myUnit, bowLineRenderer.GetStringCenterTarget(), null);
        loadedProjectile = projectile;
        isLoaded = true;
    }

    public void ShootProjectile()
    {
        StartCoroutine(loadedProjectile.ShootProjectile_AtTargetUnit(myUnit.GetAction<ShootAction>().TargetUnit(), myUnit.GetAction<ShootAction>().MyUnit()));
        loadedProjectile = null;
    }

    IEnumerator ResetToIdleRotation()
    {
        Quaternion idleRotation = Quaternion.Euler(IdleRotation());
        float rotateSpeed = 5f;
        while (transform.localRotation != idleRotation)
        {
            transform.localRotation = Quaternion.Slerp(transform.localRotation, idleRotation, rotateSpeed * Time.deltaTime);
            yield return null;
        }

        transform.localRotation = idleRotation;
    }

    IEnumerator RotateRangedWeapon(GridPosition targetGridPosition)
    {
        Quaternion targetRotation = Quaternion.Euler(0f, IdleRotation().y, CalculateZRotation(targetGridPosition));
        float rotateSpeed = 5f;
        while (transform.localRotation != targetRotation)
        {
            transform.localRotation = Quaternion.Slerp(transform.localRotation, targetRotation, rotateSpeed * Time.deltaTime);
            yield return null;
        }

        transform.localRotation = targetRotation;
    }

    float CalculateZRotation(GridPosition targetGridPosition)
    {
        float distanceXZ = TacticsPathfindingUtilities.CalculateWorldSpaceDistance_XZ(myUnit.GridPosition(), targetGridPosition);
        float distanceY = myUnit.GridPosition().y - targetGridPosition.y;
        float rotateFactor = 5f;

        float zRotation = distanceXZ * rotateFactor;
        zRotation += distanceY * rotateFactor;

        float maxZRotation = 60f;
        zRotation = Mathf.Clamp(zRotation, -maxZRotation, maxZRotation);

        // Debug.Log("Z Rotation: " + -zRotation);
        return -zRotation;
    }

    public bool IsLoaded() => isLoaded;

    void ReloadAction_OnStartReload(object sender, EventArgs e)
    {
        LoadProjectile();
    }

    void ReloadAction_OnStopReload(object sender, EventArgs e)
    {
        UnitActionSystemUI.Instance.UpdateActionVisuals();
    }

    void ShootAction_OnStartShooting(object sender, EventArgs e)
    {
        DoDefaultAttack();
    }

    void ShootAction_OnStopShooting(object sender, EventArgs e)
    {
        bowLineRenderer.StringStopFollowingTargetPositions();

        UnitActionSystemUI.Instance.UpdateActionVisuals();
    }
}
