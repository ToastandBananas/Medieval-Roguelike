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

    Unit shooter;
    Projectile_Item projectileScriptableObject;

    Vector3 targetPosition;

    int speed;
    bool moveProjectile;

    Action onProjectileBehaviourComplete;

    void Awake()
    {
        projectileCollider.enabled = false;
        trailRenderer.enabled = false;
    }

    public void Setup(Projectile_Item projectile_SO, Unit shooter, Transform parentTransform, Action onProjectileBehaviourComplete)
    {
        this.shooter = shooter;
        this.onProjectileBehaviourComplete = onProjectileBehaviourComplete;

        projectileScriptableObject = projectile_SO;
        speed = projectileScriptableObject.Speed();

        meshFilter.mesh = projectileScriptableObject.ProjectileMesh();
        meshRenderer.material = projectileScriptableObject.ProjectileMaterial();

        projectileCollider.center = projectileScriptableObject.CapsuleColliderCenter();
        projectileCollider.radius = projectileScriptableObject.CapsuleColliderRadius();
        projectileCollider.height = projectileScriptableObject.CapsuleColliderHeight();
        projectileCollider.direction = projectileScriptableObject.CapsuleColliderDirection();

        transform.parent = parentTransform;
        transform.localPosition = projectileScriptableObject.ProjectilePositionOffset();
        transform.localEulerAngles = projectileScriptableObject.ProjectileRotation();
        transform.localScale = projectileScriptableObject.ProjectileScale();

        gameObject.SetActive(true);
    }

    public IEnumerator ShootProjectile_AtTargetUnit(Unit targetUnit, Unit shooter)
    {
        ReadyProjectile();

        targetPosition = targetUnit.WorldPosition();

        Vector3 startPos = transform.position;
        Vector3 offset = GetOffset(shooter, true);

        float arcHeight = CalculateProjectileArcHeight(shooter.gridPosition, targetUnit.gridPosition) * projectileScriptableObject.ArcMultiplier();
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

    public IEnumerator ShootProjectile_AtGridPosition(GridPosition targetGridPosition, Unit shooter)
    {
        ReadyProjectile();

        targetPosition = targetGridPosition.WorldPosition();

        Vector3 startPos = transform.position;
        Vector3 offset = GetOffset(shooter, false);

        float arcHeight = CalculateProjectileArcHeight(shooter.gridPosition, targetGridPosition) * projectileScriptableObject.ArcMultiplier();
        float animationTime = 0f;

        while (moveProjectile)
        {
            animationTime += speed * Time.deltaTime;
            Vector3 nextPosition = MathParabola.Parabola(startPos, targetGridPosition.WorldPosition() + offset, arcHeight, animationTime / 5f);

            RotateTowardsNextPosition(nextPosition);

            transform.position = nextPosition;

            if (transform.position.y < -20f)
                Disable();

            yield return null;
        }

        if (shooter.IsPlayer())
            CameraController.Instance.StopFollowingTarget();
    }

    void ReadyProjectile()
    {
        transform.parent = ProjectilePool.Instance.transform;
        projectileCollider.enabled = true;
        trailRenderer.enabled = true;
        moveProjectile = true;

        SetupTrail();

        if (shooter.IsPlayer())
            StartCoroutine(CameraController.Instance.FollowTarget(transform, false, 10f));
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

    Vector3 GetOffset(Unit shootingUnit, bool accountForAccuracy)
    {
        float random = UnityEngine.Random.Range(0f, 100f);
        float offsetX, offsetZ;

        // If the shooter is missing
        if (accountForAccuracy && random > shootingUnit.stats.RangedAccuracy())
        {
            offsetX = UnityEngine.Random.Range(0.35f, 1.35f);
            offsetZ = UnityEngine.Random.Range(0.35f, 1.35f);

            // Randomize whether the offsets will be negative or positive values
            int randomX, randomZ;
            randomX = UnityEngine.Random.Range(0, 2);
            randomZ = UnityEngine.Random.Range(0, 2);

            if (randomX == 0)
                offsetX *= -1f;
            if (randomZ == 0)
                offsetZ *= -1f;

            return new Vector3(offsetX, 0f, offsetZ);
        }
        else // If the shooter is hitting the target, create a slight offset so they don't hit the same exact spot every time
        {
            offsetX = UnityEngine.Random.Range(-0.12f, 0.12f);
            offsetZ = UnityEngine.Random.Range(-0.12f, 0.12f);

            return new Vector3(offsetX, 0f, offsetZ);
        }
    }

    void RotateTowardsNextPosition(Vector3 nextPosition)
    {
        float rotateSpeed = 100f;
        Vector3 lookPos = (nextPosition - transform.position).normalized;
        Quaternion rotation = Quaternion.LookRotation(lookPos);
        transform.rotation = Quaternion.Slerp(transform.rotation, rotation, rotateSpeed * Time.deltaTime);
    }

    void Arrived(Transform collisionTransform)
    {
        StartCoroutine(TurnManager.Instance.StartNextUnitsTurn(shooter));

        moveProjectile = false;
        projectileCollider.enabled = false;
        trailRenderer.enabled = false;

        ProjectileType projectileType = projectileScriptableObject.ProjectilesType();
        if (projectileType == ProjectileType.Arrow || projectileType == ProjectileType.Bolt)
        {
            Debug.Log(collisionTransform.name + " hit by projectile");
            transform.parent = collisionTransform;
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
                if (TacticsPathfindingUtilities.CalculateWorldSpaceDistance_XYZ(LevelGrid.Instance.GetGridPosition(collider.transform.localPosition), LevelGrid.Instance.GetGridPosition(targetPosition)) <= damageRadius)
                {
                    float sphereCastRadius = 0.1f;
                    Vector3 heightOffset = Vector3.up * shooter.ShoulderHeight();
                    Vector3 shootDir = ((targetPosition + heightOffset) - (collider.transform.localPosition + heightOffset)).normalized;

                    if (Physics.SphereCast(collider.transform.localPosition + heightOffset, sphereCastRadius, shootDir, out RaycastHit hit, Vector3.Distance(collider.transform.localPosition + heightOffset, targetPosition + heightOffset), obstaclesMask))
                        continue; // Explosion blocked by an obstacle

                    if (collider.TryGetComponent(out Unit targetUnit))
                    {
                        // TODO: Less damage the further away from explosion
                        targetUnit.healthSystem.TakeDamage(30);
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
        switch (projectileScriptableObject.ProjectilesType())
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

    void Disable()
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
                Unit unit = collider.transform.parent.parent.GetComponent<Unit>();
                if (unit != shooter)
                {
                    unit.healthSystem.TakeDamage(shooter.leftHeldItem.itemData.damage);
                    Arrived(collider.transform);
                }
            }
            else if (collider.CompareTag("Unit Head"))
            {
                Unit unit = collider.transform.parent.parent.parent.GetComponent<Unit>();
                if (unit != shooter)
                {
                    unit.healthSystem.TakeDamage(shooter.leftHeldItem.itemData.damage);
                    Arrived(collider.transform);
                }
            }
            else
                Arrived(collider.transform);
        }
    }
}
