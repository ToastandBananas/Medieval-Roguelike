using System;
using System.Collections;
using UnityEngine;
using InteractableObjects;
using GridSystem;
using UnitSystem.ActionSystem;
using EffectsSystem;
using UnitSystem;
using Utilities;
using Random = UnityEngine.Random;

namespace InventorySystem
{
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

        Unit shooter, targetUnit;
        BaseAttackAction attackActionUsed;

        Vector3 targetPosition, movementDirection;

        public bool shouldMoveProjectile { get; private set; }
        int speed;
        float currentVelocity;

        readonly int defaultThrowSpeed = 15;
        readonly float defaultThrowArcMultiplier = 1f;

        Action onProjectileBehaviourComplete;

        void Awake()
        {
            projectileCollider.enabled = false;
            trailRenderer.enabled = false;

            itemData.RandomizeData();
            itemData.SetCurrentStackSize(1);
        }

        public void SetupAmmunition(ItemData itemData, Unit shooter, Transform parentTransform)
        {
            if (this.itemData == null)
                this.itemData = new ItemData(itemData);
            else
                this.itemData.TransferData(itemData);
            
            this.itemData.SetCurrentStackSize(1);
            this.shooter = shooter;

            Ammunition ammunitionItem = this.itemData.Item.Ammunition;
            speed = ammunitionItem.Speed;

            Material[] materials = meshRenderer.materials;
            for (int i = 0; i < materials.Length; i++)
            {
                if (i > ammunitionItem.PickupMeshRendererMaterials.Length - 1)
                    materials[i] = null;
                else
                    materials[i] = ammunitionItem.PickupMeshRendererMaterials[i];
            }

            meshFilter.mesh = ammunitionItem.PickupMesh;
            meshRenderer.materials = materials;

            projectileCollider.center = ammunitionItem.CapsuleColliderCenter;
            projectileCollider.radius = ammunitionItem.CapsuleColliderRadius;
            projectileCollider.height = ammunitionItem.CapsuleColliderHeight;
            projectileCollider.direction = ammunitionItem.CapsuleColliderDirection;

            transform.parent = parentTransform;
            transform.localPosition = Vector3.zero;
            transform.localEulerAngles = Vector3.zero;

            if (!shooter.IsPlayer && !shooter.unitMeshManager.IsVisibleOnScreen)
                meshRenderer.enabled = false;

            gameObject.SetActive(true);
        }

        public void SetupThrownItem(ItemData itemData, Unit thrower, Transform heldItemTransform)
        {
            if (this.itemData == null)
                this.itemData = new ItemData(itemData);
            else
                this.itemData.TransferData(itemData);

            this.itemData.SetCurrentStackSize(1);
            shooter = thrower;
            speed = defaultThrowSpeed;

            Item item = itemData.Item;

            Material[] materials = meshRenderer.materials;
            for (int i = 0; i < materials.Length; i++)
            {
                if (i > item.PickupMeshRendererMaterials.Length - 1)
                    materials[i] = null;
                else
                    materials[i] = item.PickupMeshRendererMaterials[i];
            }

            meshFilter.mesh = item.PickupMesh;
            meshRenderer.materials = materials;

            Vector3 meshSize = meshFilter.sharedMesh.bounds.size;
            projectileCollider.height = meshSize.y * 0.8f;
            projectileCollider.radius = Mathf.Min(meshSize.x, meshSize.z) / 2f;
            projectileCollider.center = new(0f, 0f, projectileCollider.height / 2f);

            transform.position = heldItemTransform.position;
            transform.eulerAngles = heldItemTransform.eulerAngles;

            if (!shooter.IsPlayer && !shooter.unitMeshManager.IsVisibleOnScreen)
                meshRenderer.enabled = false;

            gameObject.SetActive(true);
        }

        public void AddDelegate(Action delegateAction) => onProjectileBehaviourComplete += delegateAction;

        public IEnumerator ShootProjectile_AtTargetUnit(Unit targetUnit, BaseAttackAction attackActionUsed, bool hitTarget)
        {
            this.targetUnit = targetUnit;
            this.attackActionUsed = attackActionUsed;
            ReadyProjectile();

            if (targetUnit == null && LevelGrid.HasUnitAtGridPosition(attackActionUsed.TargetGridPosition, out Unit unitAtGridPosition))
                targetUnit = unitAtGridPosition;

            if (targetUnit != null)
            {
                bool attackDodged = false;
                if (hitTarget)
                    attackDodged = targetUnit.unitActionHandler.TryDodgeAttack(shooter, shooter.unitMeshManager.GetHeldRangedWeapon(), attackActionUsed, false);

                if (attackDodged)
                {
                    targetPosition = targetUnit.transform.position - (targetUnit.transform.forward * 0.5f);
                    targetUnit.unitAnimator.DoDodge(shooter, null, this);
                }
                else if (targetUnit.unitMeshManager.leftHeldItem != null && targetUnit.unitMeshManager.leftHeldItem.IsBlocking)
                    targetPosition = targetUnit.unitMeshManager.leftHeldItem.transform.position;
                else if (targetUnit.unitMeshManager.rightHeldItem != null && targetUnit.unitMeshManager.rightHeldItem.IsBlocking)
                    targetPosition = targetUnit.unitMeshManager.rightHeldItem.transform.position;
                else
                    targetPosition = targetUnit.WorldPosition;
            }
            else
                targetPosition = attackActionUsed.TargetGridPosition.WorldPosition;

            Vector3 startPos = transform.position;
            Vector3 offset = GetOffset(attackActionUsed.TargetGridPosition, attackActionUsed, hitTarget);

            float arcHeight;
            if (itemData.Item is Ammunition)
                arcHeight = MathParabola.CalculateParabolaArcHeight(shooter.GridPosition, targetUnit.GridPosition) * itemData.Item.Ammunition.ArcMultiplier;
            else
                arcHeight = MathParabola.CalculateParabolaArcHeight(shooter.GridPosition, targetUnit.GridPosition) * defaultThrowArcMultiplier;

            float animationTime = 0f;
            while (shouldMoveProjectile)
            {
                animationTime += speed * Time.deltaTime;
                Vector3 nextPosition = MathParabola.Parabola(startPos, targetPosition + offset, arcHeight, animationTime / 5f);

                // Current velocity & movement direction needed if this is a blunt projectile (so that it can properly bounce off the target when it becomes a LooseItem)
                float displacement = Vector3.Distance(transform.position, nextPosition);
                currentVelocity = displacement / Time.deltaTime;
                movementDirection = (nextPosition - transform.position).normalized;

                RotateTowardsNextPosition(nextPosition);

                transform.position = nextPosition;

                if (transform.position.y < -30f)
                {
                    shooter.unitActionHandler.SetIsAttacking(false);
                    Disable();
                }

                yield return null;
            }
        }

        void ReadyProjectile()
        {
            transform.parent = ProjectilePool.Instance.transform;
            projectileCollider.enabled = true;
            trailRenderer.enabled = true;
            meshRenderer.enabled = true;
            shouldMoveProjectile = true;

            SetupTrail();
        }

        Vector3 GetOffset(GridPosition targetGridPosition, BaseAttackAction attackActionUsed, bool hitTarget)
        {
            Vector3 shootOffset = Vector3.zero;
            if (hitTarget == false) // If the shooter is missing
            {
                float distToEnemy = Vector3.Distance(shooter.WorldPosition, shooter.unitActionHandler.TargetEnemyUnit.WorldPosition);
                float rangedAccuracy = shooter.stats.RangedAccuracy(shooter.unitMeshManager.GetHeldRangedWeapon(), targetGridPosition, attackActionUsed);

                float minOffset = 0.35f;
                float maxOffset = 1.35f;
                float offsetReduction = rangedAccuracy - (distToEnemy * 0.025f); // More accurate Units will miss by a smaller margin. Distance to the enemy also plays a factor.
                if (offsetReduction > maxOffset - minOffset)
                    offsetReduction = 0f;

                shootOffset = (shooter.transform.right * Random.Range(minOffset, maxOffset - offsetReduction)) + (shooter.transform.forward * Random.Range(-maxOffset, maxOffset - offsetReduction)); // minOffset is unnecessary for randomizing the forward direction, so use -maxOffset instead
            }
            else // If the shooter is hitting the target, even if they dodge it, create a slight offset so they don't hit the same exact spot every time
            {
                float maxOffset = 0.15f;
                shootOffset.Set(Random.Range(-maxOffset, maxOffset), 0f, Random.Range(-maxOffset, maxOffset));
            }

            int randomX = Random.Range(0, 2);
            int randomZ = Random.Range(0, 2);

            // Randomize left/right of target
            if (randomX == 0)
                shootOffset.x *= -1f;

            // Randomize towards/away from shooter
            if (randomZ == 0)
                shootOffset.z *= -1f;

            return shootOffset;
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
            shooter.unitActionHandler.SetIsAttacking(false);

            shouldMoveProjectile = false;
            projectileCollider.enabled = false;
            trailRenderer.enabled = false;

            ProjectileType projectileType;
            if (itemData.Item is Ammunition)
                projectileType = itemData.Item.Ammunition.ProjectileType;
            else
                projectileType = itemData.Item.ThrownProjectileType;

            if (projectileType == ProjectileType.BluntObject)
            {
                SetupNewLooseItem(false, out LooseItem looseProjectile);
                float forceMagnitude = looseProjectile.RigidBody.mass * currentVelocity;
                looseProjectile.RigidBody.AddForce(movementDirection * forceMagnitude, ForceMode.Impulse);
            }
            else if (projectileType == ProjectileType.Arrow || projectileType == ProjectileType.Bolt || projectileType == ProjectileType.Spear || projectileType == ProjectileType.ThrowingKnife || projectileType == ProjectileType.Axe)
            {
                // Debug.Log(collisionTransform.name + " hit by projectile");
                SetupNewLooseItem(true, out LooseItem looseProjectile);
                if (collisionTransform != null)
                {
                    looseProjectile.MeshCollider.isTrigger = true;
                    looseProjectile.transform.SetParent(collisionTransform, true);
                }
            }
            else if (projectileType == ProjectileType.Explosive)
            {
                float damageRadius = 2f;
                Collider[] colliderArray = Physics.OverlapSphere(targetPosition, damageRadius);

                foreach (Collider collider in colliderArray)
                {
                    if (Vector3.Distance(LevelGrid.GetGridPosition(collider.transform.localPosition).WorldPosition, LevelGrid.GetGridPosition(targetPosition).WorldPosition) <= damageRadius)
                    {
                        float sphereCastRadius = 0.1f;
                        Vector3 heightOffset = Vector3.up * shooter.ShoulderHeight;
                        Vector3 shootDir = (targetPosition + heightOffset - (collider.transform.localPosition + heightOffset)).normalized;

                        if (Physics.SphereCast(collider.transform.localPosition + heightOffset, sphereCastRadius, shootDir, out RaycastHit hit, Vector3.Distance(collider.transform.localPosition + heightOffset, targetPosition + heightOffset), obstaclesMask))
                            continue; // Explosion blocked by an obstacle

                        if (collider.TryGetComponent(out Unit targetUnit))
                        {
                            shooter.vision.BecomeVisibleUnitOfTarget(targetUnit, true);

                            // TODO: Less damage the further away from explosion
                            targetUnit.health.TakeDamage(30, shooter);
                        }
                    }
                }

                // Explosion
                ParticleSystem explosion = ExplosionPool.Instance.GetExplosionFromPool();
                explosion.transform.localPosition = targetPosition + (Vector3.up * 0.5f);
                explosion.gameObject.SetActive(true);

                // Screen shake
                OnExplosion?.Invoke(this, EventArgs.Empty);
            }

            Disable();
        }

        public void SetupNewLooseItem(bool preventMovement, out LooseItem looseProjectile)
        {
            looseProjectile = LooseItemPool.Instance.GetLooseItemFromPool();

            ItemData newItemData = new ItemData();
            newItemData.TransferData(itemData);
            looseProjectile.SetItemData(newItemData);

            looseProjectile.SetupMesh();
            looseProjectile.name = newItemData.Item.name;
            looseProjectile.transform.SetPositionAndRotation(transform.position, Quaternion.Euler(transform.eulerAngles + meshFilter.transform.localEulerAngles));

            if (preventMovement)
            {
                looseProjectile.RigidBody.isKinematic = true;
                looseProjectile.RigidBody.useGravity = false;
            }

            looseProjectile.gameObject.SetActive(true);

            shooter.vision.AddVisibleLooseItem(looseProjectile);
            if (targetUnit != null)
                targetUnit.vision.AddVisibleLooseItem(looseProjectile);
        }

        void SetupTrail()
        {
            if (itemData.Item is Ammunition == false)
            {
                trailRenderer.time = 0.065f;
                trailRenderer.startWidth = 0.015f;
                return;
            }

            switch (itemData.Item.Ammunition.ProjectileType)
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
            // Target unit stops blocking
            onProjectileBehaviourComplete?.Invoke();
            onProjectileBehaviourComplete = null;

            itemData = null;
            shouldMoveProjectile = false;
            shooter = null;
            attackActionUsed = null;
            targetUnit = null;
            projectileCollider.enabled = false;
            trailRenderer.enabled = false;

            ProjectilePool.ReturnToPool(this);
        }

        void OnTriggerEnter(Collider collider)
        {
            if (collider.isTrigger == false)
            {
                // Debug.Log("Projectile stuck in: " + collider.gameObject.name);
                if (collider.CompareTag("Unit Body"))
                {
                    Unit targetUnit = collider.GetComponentInParent<Unit>();
                    HeldRangedWeapon heldRangedWeapon = shooter.unitMeshManager.GetHeldRangedWeapon();

                    if (targetUnit != shooter)
                    {
                        attackActionUsed.DamageTarget(targetUnit, heldRangedWeapon, itemData, null, false);
                        Arrived(collider.transform);
                    }
                }
                else if (collider.CompareTag("Unit Head"))
                {
                    Unit targetUnit = collider.GetComponentInParent<Unit>();
                    HeldRangedWeapon heldRangedWeapon = shooter.unitMeshManager.GetHeldRangedWeapon();

                    if (targetUnit != shooter)
                    {
                        attackActionUsed.DamageTarget(targetUnit, heldRangedWeapon, itemData, null, true);
                        Arrived(collider.transform);
                    }
                }
                else if (collider.CompareTag("Shield"))
                {
                    HeldShield heldShield = collider.GetComponent<HeldShield>();
                    HeldRangedWeapon heldRangedWeapon = shooter.unitMeshManager.GetHeldRangedWeapon();

                    // DamageTarget will take into account whether the Unit blocked or not
                    attackActionUsed.DamageTarget(collider.GetComponentInParent<Unit>(), heldRangedWeapon, itemData, heldShield, false);
                    Arrived(collider.transform);
                }
                else if (collider.CompareTag("Loose Item") == false)
                    Arrived(null);
            }
        }

        public ItemData ItemData => itemData;

        public MeshRenderer MeshRenderer => meshRenderer;
    }
}
