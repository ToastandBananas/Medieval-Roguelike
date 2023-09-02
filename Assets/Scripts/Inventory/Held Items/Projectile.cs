using System;
using System.Collections;
using UnityEngine;

public class Projectile : MonoBehaviour
{
    public static event EventHandler OnExplosion;

    [SerializeField] MeshFilter meshFilter;
    [SerializeField] MeshRenderer meshRenderer;
    [SerializeField] CapsuleCollider projectileCollider;
    [SerializeField] LayerMask obstaclesMask;

    [Header("VFX")]
    [SerializeField] TrailRenderer trailRenderer;

    [Header("Item Data")]
    [SerializeField] ItemData itemData;

    Unit shooter;

    Vector3 targetPosition;

    int speed;
    bool moveProjectile;

    Action onProjectileBehaviourComplete;

    void Awake()
    {
        projectileCollider.enabled = false;
        trailRenderer.enabled = false;

        itemData.RandomizeData();
    }

    public void Setup(ItemData itemData, Unit shooter, Transform parentTransform, Action onProjectileBehaviourComplete)
    {
        this.shooter = shooter;
        this.onProjectileBehaviourComplete = onProjectileBehaviourComplete;

        this.itemData = itemData;
        Ammunition ammunitionItem = this.itemData.Item().Ammunition();

        speed = ammunitionItem.Speed();

        meshFilter.mesh = ammunitionItem.AmmunitionMesh();
        meshRenderer.material = ammunitionItem.AmmunitionMaterial();

        projectileCollider.center = ammunitionItem.CapsuleColliderCenter();
        projectileCollider.radius = ammunitionItem.CapsuleColliderRadius();
        projectileCollider.height = ammunitionItem.CapsuleColliderHeight();
        projectileCollider.direction = ammunitionItem.CapsuleColliderDirection();

        transform.parent = parentTransform;
        transform.localPosition = ammunitionItem.AmmunitionPositionOffset();
        transform.localEulerAngles = ammunitionItem.AmmunitionRotation();
        transform.localScale = ammunitionItem.AmmunitionScale();

        gameObject.SetActive(true);
    }

    public IEnumerator ShootProjectile_AtTargetUnit(Unit targetUnit, bool missedTarget)
    {
        ReadyProjectile();

        targetPosition = targetUnit.WorldPosition();

        Vector3 startPos = transform.position;
        Vector3 offset = GetOffset(missedTarget);

        float arcHeight = CalculateProjectileArcHeight(shooter.gridPosition, targetUnit.gridPosition) * itemData.Item().Ammunition().ArcMultiplier();
        float animationTime = 0f;

        while (moveProjectile)
        {
            animationTime += speed * Time.deltaTime;
            Vector3 nextPosition = MathParabola.Parabola(startPos, targetUnit.transform.position + offset, arcHeight, animationTime / 5f);

            RotateTowardsNextPosition(nextPosition);

            transform.position = nextPosition;

            if (transform.position.y < -20f)
                Disable();

            yield return null;
        }

        CameraController.Instance.StopFollowingTarget();
    }

    void ReadyProjectile()
    {
        transform.parent = ProjectilePool.Instance.transform;
        projectileCollider.enabled = true;
        trailRenderer.enabled = true;
        moveProjectile = true;

        SetupTrail();

        //if (shooter.IsPlayer())
            //StartCoroutine(CameraController.Instance.FollowTarget(transform, false, 10f));
    }

    float CalculateProjectileArcHeight(GridPosition startGridPosition, GridPosition targetGridPosition)
    {
        float distanceXZ = TacticsPathfindingUtilities.CalculateWorldSpaceDistance_XZ(startGridPosition, targetGridPosition);
        float distanceY = startGridPosition.y - targetGridPosition.y;
        float arcHeightFactor = 0.1f;

        float arcHeight = distanceXZ * arcHeightFactor;
        arcHeight += distanceY * arcHeightFactor;

        float maxArcHeight = 3f;
        arcHeight = Mathf.Clamp(arcHeight, 0f, maxArcHeight);

        // Debug.Log("Arc Height: " + arcHeight);
        return arcHeight;
    }

    Vector3 GetOffset(bool missedTarget)
    {
        float offsetX, offsetZ;

        // If the shooter is missing
        if (missedTarget)
        {
            float rangedAccuracy = shooter.stats.RangedAccuracy(shooter.unitMeshManager.GetRangedWeapon().ItemData());
            float minOffset = 0.35f;
            float maxOffset = 1.35f;
            float distToEnemy = Vector3.Distance(shooter.WorldPosition(), shooter.unitActionHandler.targetEnemyUnit.WorldPosition());
            offsetX = UnityEngine.Random.Range(minOffset, maxOffset - (rangedAccuracy * 0.01f) - (distToEnemy * 0.1f)); // More accurate Units will miss by a smaller margin. Distance to the enemy also plays a factor.
            offsetZ = UnityEngine.Random.Range(minOffset, maxOffset - (rangedAccuracy * 0.01f) - (distToEnemy * 0.1f));

            if (offsetX < minOffset) offsetX = minOffset;
            if (offsetZ < minOffset) offsetZ = minOffset;

            // Randomize whether the offsets will be negative or positive values
            int randomX = UnityEngine.Random.Range(0, 2);
            int randomZ = UnityEngine.Random.Range(0, 2);

            if (randomX == 0)
                offsetX *= -1f;
            if (randomZ == 0)
                offsetZ *= -1f;
        }
        else // If the shooter is hitting the target, create a slight offset so they don't hit the same exact spot every time
        {
            offsetX = UnityEngine.Random.Range(-0.1f, 0.1f);
            offsetZ = UnityEngine.Random.Range(-0.1f, 0.1f);
        }

        return new Vector3(offsetX, 0f, offsetZ);
    }

    void RotateTowardsNextPosition(Vector3 nextPosition)
    {
        float rotateSpeed = 100f;
        Vector3 lookPos = (nextPosition - transform.localPosition).normalized;
        Quaternion rotation = Quaternion.LookRotation(lookPos);
        transform.localRotation = Quaternion.Slerp(transform.localRotation, rotation, rotateSpeed * Time.deltaTime);
    }

    void Arrived(Transform collisionTransform)
    {
        shooter.unitActionHandler.targetUnits.Clear();

        TurnManager.Instance.StartNextUnitsTurn(shooter);

        moveProjectile = false;
        projectileCollider.enabled = false;
        trailRenderer.enabled = false;

        ProjectileType projectileType = itemData.Item().Ammunition().ProjectileType();
        if (projectileType == ProjectileType.Arrow || projectileType == ProjectileType.Bolt)
        {
            // Debug.Log(collisionTransform.name + " hit by projectile");
            //if (collisionTransform != null)
                //transform.SetParent(collisionTransform, true);

            if (onProjectileBehaviourComplete != null)
                onProjectileBehaviourComplete();
        }
        else if (projectileType == ProjectileType.BluntObject)
        {
            if (onProjectileBehaviourComplete != null)
                onProjectileBehaviourComplete();
        }
        else if (projectileType == ProjectileType.Explosive)
        {
            float damageRadius = 2f;
            Collider[] colliderArray = Physics.OverlapSphere(targetPosition, damageRadius);

            foreach (Collider collider in colliderArray)
            {
                if (TacticsPathfindingUtilities.CalculateWorldSpaceDistance_XYZ(LevelGrid.GetGridPosition(collider.transform.localPosition), LevelGrid.GetGridPosition(targetPosition)) <= damageRadius)
                {
                    float sphereCastRadius = 0.1f;
                    Vector3 heightOffset = Vector3.up * shooter.ShoulderHeight();
                    Vector3 shootDir = ((targetPosition + heightOffset) - (collider.transform.localPosition + heightOffset)).normalized;

                    if (Physics.SphereCast(collider.transform.localPosition + heightOffset, sphereCastRadius, shootDir, out RaycastHit hit, Vector3.Distance(collider.transform.localPosition + heightOffset, targetPosition + heightOffset), obstaclesMask))
                        continue; // Explosion blocked by an obstacle

                    if (collider.TryGetComponent(out Unit targetUnit))
                    {
                        shooter.unitActionHandler.GetAction<ShootAction>().BecomeVisibleEnemyOfTarget(targetUnit);

                        // TODO: Less damage the further away from explosion
                        targetUnit.health.TakeDamage(30, shooter.transform);
                    }
                }
            }

            // Explosion
            ParticleSystem explosion = ExplosionPool.Instance.GetExplosionFromPool();
            explosion.transform.localPosition = targetPosition + (Vector3.up * 0.5f);
            explosion.gameObject.SetActive(true);

            // Screen shake
            OnExplosion?.Invoke(this, EventArgs.Empty);

            // Throw Action: CompleteAction()
            if (onProjectileBehaviourComplete != null)
                onProjectileBehaviourComplete();

            Disable();
        }
    }

    void SetupTrail()
    {
        switch (itemData.Item().Ammunition().ProjectileType())
        {
            case ProjectileType.Arrow:
                trailRenderer.time = 0.065f;
                trailRenderer.startWidth = 0.015f;
                break;
            case ProjectileType.Bolt:
                trailRenderer.time = 0.065f;
                trailRenderer.startWidth = 0.015f;
                break;
            case ProjectileType.BluntObject:
                trailRenderer.time = 0.15f;
                trailRenderer.startWidth = 0.075f;
                break;
            case ProjectileType.Explosive:
                trailRenderer.time = 0.2f;
                trailRenderer.startWidth = 0.1f;
                break;
            default:
                break;
        }
    }

    public void Disable()
    {
        moveProjectile = false;
        shooter = null;
        projectileCollider.enabled = false;
        trailRenderer.enabled = false;
        transform.parent = ProjectilePool.Instance.transform;
        gameObject.SetActive(false);
    }

    void OnTriggerEnter(Collider collider)
    {
        if (collider.isTrigger == false)
        {
            if (collider.CompareTag("Unit Body"))
            {
                Unit targetUnit = collider.transform.parent.parent.GetComponent<Unit>();
                if (targetUnit != shooter)
                {
                    bool attackBlocked = false;
                    if (shooter.unitActionHandler.targetUnits.ContainsKey(targetUnit))
                    {
                        shooter.unitActionHandler.targetUnits.TryGetValue(targetUnit, out HeldItem itemBlockedAttackWith);
                        if (itemBlockedAttackWith != null)
                            attackBlocked = true;
                    }

                    HeldRangedWeapon rangedWeapon = shooter.unitMeshManager.leftHeldItem as HeldRangedWeapon;
                    if (attackBlocked == false || targetUnit != shooter.unitActionHandler.targetEnemyUnit)
                        shooter.unitActionHandler.GetAction<ShootAction>().DamageTargets(rangedWeapon);

                    Arrived(collider.transform);
                    Disable();
                }
            }
            else if (collider.CompareTag("Unit Head"))
            {
                Unit targetUnit = collider.transform.parent.parent.parent.GetComponent<Unit>();
                if (targetUnit != shooter)
                {
                    bool attackBlocked = false;
                    if (shooter.unitActionHandler.targetUnits.ContainsKey(targetUnit))
                    {
                        shooter.unitActionHandler.targetUnits.TryGetValue(targetUnit, out HeldItem itemBlockedAttackWith);
                        if (itemBlockedAttackWith != null)
                            attackBlocked = true;
                    }

                    HeldRangedWeapon rangedWeapon = shooter.unitMeshManager.leftHeldItem as HeldRangedWeapon;
                    if (attackBlocked == false || targetUnit != shooter.unitActionHandler.targetEnemyUnit)
                        shooter.unitActionHandler.GetAction<ShootAction>().DamageTargets(rangedWeapon);

                    Arrived(collider.transform);
                    Disable();
                }
            }
            else if (collider.CompareTag("Shield"))
            {
                Unit targetUnit = collider.transform.parent.parent.parent.parent.parent.GetComponent<Unit>();
                HeldRangedWeapon rangedWeapon = shooter.unitMeshManager.leftHeldItem as HeldRangedWeapon;
                shooter.unitActionHandler.GetAction<ShootAction>().DamageTargets(rangedWeapon);

                Arrived(collider.transform);
                Disable();
            }
            else
                Arrived(null);
        }
    }

    public ItemData ItemData() => itemData;
}
