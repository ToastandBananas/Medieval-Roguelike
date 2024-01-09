using System;
using System.Collections;
using UnityEngine;
using InteractableObjects;
using GridSystem;
using EffectsSystem;
using Utilities;
using Random = UnityEngine.Random;
using Unit = UnitSystem.Unit;
using UnitSystem.ActionSystem.Actions;

namespace InventorySystem
{
    public enum ProjectileAnimationType { Straight, EndOverEnd }

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
        Action_BaseAttack attackActionUsed;
        HeldRangedWeapon heldRangedWeaponUsed;

        Vector3 targetPosition, movementDirection;

        public bool ShouldMoveProjectile { get; private set; }
        float currentVelocity, speedMultiplier;

        public static readonly float defaultThrowArcMultiplier = 1.6f;

        Action onProjectileBehaviourComplete;

        void Awake()
        {
            projectileCollider.enabled = false;
            trailRenderer.enabled = false;
        }

        public void SetupAmmunition(ItemData itemData, Unit shooter, Transform parentTransform)
        {
            if (this.itemData == null)
                this.itemData = new ItemData(itemData);
            else
                this.itemData.TransferData(itemData);
            
            this.itemData.SetCurrentStackSize(1);
            this.shooter = shooter;

            Item_Ammunition ammunitionItem = this.itemData.Item.Ammunition;
            speedMultiplier = ammunitionItem.SpeedMultiplier;

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

            Vector3 meshSize = meshFilter.sharedMesh.bounds.size;
            if (ammunitionItem.ProjectileType == ProjectileType.BluntObject || ammunitionItem.ProjectileType == ProjectileType.Explosive)
                projectileCollider.height = meshSize.y;
            else
                projectileCollider.height = meshSize.y * 0.75f;

            projectileCollider.radius = Mathf.Min(meshSize.x, meshSize.z) / 8f;
            projectileCollider.center = new(0f, projectileCollider.height / 2f, 0f);

            transform.parent = parentTransform;
            transform.localPosition = Vector3.zero;
            transform.localEulerAngles = Vector3.zero;

            if (!shooter.IsPlayer && !shooter.UnitMeshManager.IsVisibleOnScreen)
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
            speedMultiplier = itemData.Item.ThrowingSpeedMultiplier;

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
            if (itemData.Item.ThrownProjectileType == ProjectileType.BluntObject || itemData.Item.ThrownProjectileType == ProjectileType.Explosive)
            {
                projectileCollider.height = meshSize.y;
                projectileCollider.radius = Mathf.Min(meshSize.x, meshSize.z) / 4f;
            }
            else
            {
                projectileCollider.height = meshSize.y * 0.65f;
                projectileCollider.radius = Mathf.Min(meshSize.x, meshSize.z) / 8f;
            }

            if (itemData.Item.ThrownAnimationType == ProjectileAnimationType.Straight)
                projectileCollider.center = new(0f, projectileCollider.height / 2f, 0f);
            else
                projectileCollider.center = Vector3.zero;

            transform.position = heldItemTransform.position;
            transform.eulerAngles = heldItemTransform.eulerAngles;

            if (!shooter.IsPlayer && !shooter.UnitMeshManager.IsVisibleOnScreen)
                meshRenderer.enabled = false;

            gameObject.SetActive(true);
        }

        public void AddDelegate(Action delegateAction) => onProjectileBehaviourComplete += delegateAction;

        public void ShootProjectileAtTarget(Unit targetUnit, HeldRangedWeapon heldRangedWeaponUsed, Action_BaseAttack attackActionUsed, bool hitTarget, bool beingThrown)
        {
            if (itemData == null || itemData.Item == null)
            {
                shooter.UnitActionHandler.SetIsAttacking(false);
                Disable();
                return;
            }

            this.targetUnit = targetUnit;
            this.attackActionUsed = attackActionUsed;
            this.heldRangedWeaponUsed = heldRangedWeaponUsed;
            if (targetUnit == null && LevelGrid.HasUnitAtGridPosition(attackActionUsed.TargetGridPosition, out Unit unitAtGridPosition))
                this.targetUnit = unitAtGridPosition;

            SetTargetPosition(hitTarget);
            ReadyProjectile();

            shooter.StartCoroutine(MoveTowardsTarget(hitTarget, beingThrown));

            if (!beingThrown && heldRangedWeaponUsed != null)
            {
                heldRangedWeaponUsed.SetLoadedProjectile(null);
                heldRangedWeaponUsed.ItemData.DamageDurability(shooter, attackActionUsed.WeaponDurabilityDamage());
            }
        }

        void ReadyProjectile()
        {
            transform.parent = Pool_Projectiles.Instance.transform;
            projectileCollider.enabled = true;
            trailRenderer.enabled = true;
            meshRenderer.enabled = true;
            ShouldMoveProjectile = true;

            SetupTrail();
        }

        IEnumerator MoveTowardsTarget(bool hitTarget, bool beingThrown)
        {
            Vector3 startPosition = transform.position;
            Vector3 offset = GetOffset(attackActionUsed.TargetGridPosition, attackActionUsed, hitTarget);
            targetPosition += offset;

            float distance = Vector3.Distance(startPosition, targetPosition);
            float minDistanceForFlipAnim = 2.5f;
            float arcHeight = GetArcHeight();

            float timeScale = distance / 12f;
            float animationTime = 0f;
            zRotationAngle = xRotationAngle = 0f;
            ProjectileAnimationType animationType = GetProjectileAnimationType(beingThrown);

            while (ShouldMoveProjectile)
            {
                animationTime += speedMultiplier * Time.deltaTime;
                Vector3 nextPosition = MathParabola.SampleParabola(startPosition, targetPosition, arcHeight, animationTime / timeScale % 1);

                // Current velocity & movement direction needed if this is a blunt projectile (so that it can properly bounce off the target when it becomes a LooseItem)
                // Movement direction is also needed for setting the rotation on arrival to match the projectile's momentum
                float displacement = Vector3.Distance(transform.position, nextPosition);
                currentVelocity = displacement / Time.deltaTime;
                movementDirection = (nextPosition - transform.position).normalized;

                if (distance > minDistanceForFlipAnim && animationType == ProjectileAnimationType.EndOverEnd)
                    FlipEndOverEnd(distance);
                else
                    RotateTowardsNextPosition(nextPosition);

                transform.position = nextPosition;
                
                if ((transform.position - targetPosition).magnitude <= 0.15f || Vector3.Distance(transform.position, targetPosition) <= 0.1f)
                {
                    Arrived(null, null, true);
                    yield break;
                }

                if (transform.position.y < -30f)
                {
                    shooter.UnitActionHandler.SetIsAttacking(false);
                    Disable();
                    yield break;
                }

                yield return null;
            }
        }

        void RotateTowardsNextPosition(Vector3 nextPosition)
        {
            float rotateSpeed = 150f;
            Vector3 lookPos = (nextPosition - transform.position).normalized;
            Quaternion lookRotation = Quaternion.LookRotation(lookPos);

            // Get the spin rotation
            Quaternion spinRotation = Spin();

            // Combine the target rotation with the spin
            Quaternion combinedRotation = lookRotation * spinRotation;

            // Apply the combined rotation
            transform.rotation = Quaternion.Slerp(transform.rotation, combinedRotation, rotateSpeed * Time.deltaTime);
        }

        float xRotationAngle = 0f;
        Quaternion Spin()
        {
            float spinSpeed = 100f;
            xRotationAngle += spinSpeed * Time.deltaTime;
            xRotationAngle %= 360; // Keep the angle within 0-360 degrees

            // Return the spin rotation as a Quaternion
            return Quaternion.Euler(xRotationAngle, 270, 270);
        }

        float zRotationAngle = 0f;
        void FlipEndOverEnd(float distance)
        {
            float baseRotationSpeedPerUnitDistance = 360f;

            // Calculate the actual rotation speed based on the distance
            float rotationSpeed = baseRotationSpeedPerUnitDistance * distance;

            // Increment the X-axis rotation
            zRotationAngle += rotationSpeed * Time.deltaTime;
            zRotationAngle %= 360; // Keep the angle within 0-360 degrees

            // Calculate the direction to the target position
            Vector3 direction = targetPosition - transform.position;
            Quaternion targetAlignment = Quaternion.LookRotation(direction);
            targetAlignment.x = 0;
            targetAlignment.z = 0;

            // Combine the independent X rotation with Y-axis alignment and fixed Z-axis rotation
            transform.rotation = targetAlignment * Quaternion.Euler(0, 270, -zRotationAngle);
        }

        void SetTargetPosition(bool hitTarget)
        {
            if (targetUnit != null)
            {
                Vector3 directionToTarget = (targetUnit.WorldPosition - shooter.WorldPosition).normalized;
                directionToTarget.y = 0;
                Vector3 offset = Random.Range(0.2f, 0.3f) * directionToTarget;

                bool attackDodged = false;
                if (hitTarget)
                    attackDodged = targetUnit.UnitActionHandler.TryDodgeAttack(shooter, shooter.UnitMeshManager.GetHeldRangedWeapon(), attackActionUsed, false);

                if (attackDodged)
                {
                    offset = Random.Range(0.4f, 0.7f) * directionToTarget;
                    targetPosition = targetUnit.transform.position;
                    targetUnit.UnitAnimator.DoDodge(shooter, null, this);
                }
                else if (targetUnit.UnitMeshManager.LeftHeldItem != null && targetUnit.UnitMeshManager.LeftHeldItem.IsBlocking)
                    targetPosition = targetUnit.UnitMeshManager.LeftHeldItem.transform.position;
                else if (targetUnit.UnitMeshManager.RightHeldItem != null && targetUnit.UnitMeshManager.RightHeldItem.IsBlocking)
                    targetPosition = targetUnit.UnitMeshManager.RightHeldItem.transform.position;
                else
                    targetPosition = targetUnit.WorldPosition;

                targetPosition += offset;
            }
            else
                targetPosition = attackActionUsed.TargetGridPosition.WorldPosition;
        }

        float GetArcHeight()
        {
            if (itemData.Item is Item_Ammunition)
                return MathParabola.CalculateParabolaArcHeight(shooter.GridPosition, targetUnit.GridPosition, itemData.Item.Ammunition.ArcMultiplier);
            else
                return MathParabola.CalculateParabolaArcHeight(shooter.GridPosition, targetUnit.GridPosition, defaultThrowArcMultiplier);
        }

        ProjectileAnimationType GetProjectileAnimationType(bool beingThrown)
        {
            if (beingThrown || itemData.Item is Item_Ammunition == false)
                return itemData.Item.ThrownAnimationType;
            else
                return itemData.Item.Ammunition.ProjectileAnimationType;
        }

        Vector3 GetOffset(GridPosition targetGridPosition, Action_BaseAttack attackActionUsed, bool hitTarget)
        {
            Vector3 shootOffset = Vector3.zero;
            if (hitTarget == false) // If the shooter is missing
            {
                float distToEnemy = Vector3.Distance(shooter.WorldPosition, shooter.UnitActionHandler.TargetEnemyUnit.WorldPosition);
                float rangedAccuracy = shooter.Stats.RangedAccuracy(shooter.UnitMeshManager.GetHeldRangedWeapon(), targetGridPosition, attackActionUsed);

                float minOffset = 0.35f;
                float maxOffset = 1.35f;
                float offsetReduction = rangedAccuracy - (distToEnemy * 0.025f); // More accurate Units will miss by a smaller margin. Distance to the enemy also plays a factor.
                if (offsetReduction > maxOffset - minOffset)
                    offsetReduction = 0f;

                shootOffset = (shooter.transform.right * Random.Range(minOffset, maxOffset - offsetReduction)) + (shooter.transform.forward * Random.Range(-maxOffset, maxOffset - offsetReduction)); // minOffset is unnecessary for randomizing the forward direction, so use -maxOffset instead
            }
            else // If the shooter is hitting the target, even if they dodge it, create a slight offset so they don't hit the same exact spot every time
            {
                float maxOffset = 0.1f;
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

        void Arrived(Collider collisionCollider, Transform collisionTransform, bool forceBluntObjectBehaviour)
        {
            shooter.UnitActionHandler.TargetUnits.Clear();
            shooter.UnitActionHandler.SetIsAttacking(false);

            ShouldMoveProjectile = false;
            projectileCollider.enabled = false;
            trailRenderer.enabled = false;

            ProjectileType projectileType;
            if (itemData.Item is Item_Ammunition)
                projectileType = itemData.Item.Ammunition.ProjectileType;
            else
                projectileType = itemData.Item.ThrownProjectileType;

            if (forceBluntObjectBehaviour || projectileType == ProjectileType.BluntObject)
            {
                Interactable_LooseItem looseProjectile = SetupNewLooseItem(collisionCollider, projectileType, false);
                float forceMagnitude = looseProjectile.RigidBody.mass * currentVelocity;
                looseProjectile.RigidBody.AddForce(movementDirection * forceMagnitude, ForceMode.Impulse);
            }
            else if (projectileType == ProjectileType.Arrow || projectileType == ProjectileType.Bolt || projectileType == ProjectileType.Spear || projectileType == ProjectileType.ThrowingDagger || projectileType == ProjectileType.Axe)
            {
                // Debug.Log(collisionTransform + " hit by projectile");
                Interactable_LooseItem looseProjectile = SetupNewLooseItem(collisionCollider, projectileType, true);
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
                        if (Physics.SphereCast(collider.transform.localPosition + heightOffset, sphereCastRadius, shootDir, out _, Vector3.Distance(collider.transform.localPosition + heightOffset, targetPosition + heightOffset), obstaclesMask))
                            continue; // Explosion blocked by an obstacle

                        if (collider.TryGetComponent(out Unit targetUnit))
                        {
                            shooter.Vision.BecomeVisibleUnitOfTarget(targetUnit, true);

                            // TODO: Less damage the further away from explosion
                            int damage = 30;
                            targetUnit.HealthSystem.DamageAllBodyParts(damage, shooter);
                        }
                    }
                }

                // Explosion
                ParticleSystem explosion = Pool_Explosions.Instance.GetExplosionFromPool();
                explosion.transform.localPosition = targetPosition + (Vector3.up * 0.5f);
                explosion.gameObject.SetActive(true);

                // Screen shake
                OnExplosion?.Invoke(this, EventArgs.Empty);
            }

            Disable();
        }

        public Interactable_LooseItem SetupNewLooseItem(Collider collisionCollider, ProjectileType projectileType, bool preventMovement)
        {
            Interactable_LooseItem looseProjectile = Pool_LooseItems.Instance.GetLooseItemFromPool();

            ItemData newItemData = new();
            newItemData.TransferData(itemData);
            looseProjectile.SetItemData(newItemData);

            looseProjectile.SetupMesh();
            looseProjectile.name = newItemData.Item.name;

            if (collisionCollider != null)
            {
                Quaternion startRotation = transform.rotation;
                if (projectileType == ProjectileType.ThrowingDagger)
                {
                    Quaternion lookRotation = Quaternion.LookRotation(movementDirection);
                    lookRotation.x = lookRotation.y = 0;
                    looseProjectile.transform.SetPositionAndRotation(transform.position, lookRotation * Quaternion.Euler(startRotation.x, startRotation.y, 270));
                }
                else if (projectileType == ProjectileType.Axe)
                {
                    Quaternion lookRotation = Quaternion.LookRotation(movementDirection);
                    lookRotation.x = lookRotation.y = 0;
                    looseProjectile.transform.SetPositionAndRotation(transform.position, lookRotation * Quaternion.Euler(startRotation.x, startRotation.y, 285));
                }
                else
                    looseProjectile.transform.SetPositionAndRotation(transform.position, startRotation);
            }
            else
                looseProjectile.transform.SetPositionAndRotation(transform.position, transform.rotation);

            if (preventMovement)
            {
                looseProjectile.RigidBody.isKinematic = true;
                looseProjectile.RigidBody.useGravity = false;
            }

            looseProjectile.gameObject.SetActive(true);

            shooter.Vision.AddVisibleLooseItem(looseProjectile);
            if (targetUnit != null)
                targetUnit.Vision.AddVisibleLooseItem(looseProjectile);
            return looseProjectile;
        }

        void SetupTrail()
        {
            if (itemData.Item is Item_Ammunition == false)
            {
                if (itemData.Item.ThrownAnimationType == ProjectileAnimationType.EndOverEnd)
                    trailRenderer.enabled = false;
                else
                {
                    trailRenderer.time = 0.065f;
                    trailRenderer.startWidth = 0.015f;
                }
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
            ShouldMoveProjectile = false;
            shooter = null;
            attackActionUsed = null;
            heldRangedWeaponUsed = null;
            targetUnit = null;
            projectileCollider.enabled = false;
            trailRenderer.enabled = false;

            Pool_Projectiles.ReturnToPool(this);
        }

        void OnTriggerEnter(Collider collider)
        {
            if (!collider.isTrigger)
            {
                // Debug.Log("Projectile stuck in: " + collider.gameObject.name);
                if (collider.CompareTag("Unit Body"))
                {
                    Unit targetUnit = collider.GetComponentInParent<Unit>();
                    if (targetUnit == shooter)
                        return;
                    
                    attackActionUsed.DamageBodyPart(targetUnit, targetUnit.HealthSystem.GetBodyPart(UnitSystem.BodyPartType.Torso), heldRangedWeaponUsed, itemData, null);
                    Arrived(collider, collider.transform, false);
                    
                }
                else if (collider.CompareTag("Unit Head"))
                {
                    Unit targetUnit = collider.GetComponentInParent<Unit>();
                    if (targetUnit == shooter)
                        return;
                    
                    attackActionUsed.DamageBodyPart(targetUnit, targetUnit.HealthSystem.GetBodyPart(UnitSystem.BodyPartType.Head), heldRangedWeaponUsed, itemData, null);
                    Arrived(collider, collider.transform, false);
                    
                }
                else if (collider.CompareTag("Shield"))
                {
                    HeldShield heldShield = collider.GetComponent<HeldShield>();
                    if (heldShield == shooter.UnitMeshManager.LeftHeldItem || heldShield == shooter.UnitMeshManager.RightHeldItem)
                        return;

                    // This won't actually do any damage since the attack was blocked, but it will still have a chance to knockback the targetUnit
                    Unit targetUnit = collider.GetComponentInParent<Unit>();
                    attackActionUsed.DamageBodyPart(targetUnit, targetUnit.HealthSystem.GetBodyPart(UnitSystem.BodyPartType.Torso), heldRangedWeaponUsed, itemData, heldShield);
                    Arrived(collider, collider.transform, false);
                    
                }
                else if (!collider.CompareTag("Loose Item"))
                    Arrived(collider, null, false);
            }
        }

        public ItemData ItemData => itemData;

        public MeshRenderer MeshRenderer => meshRenderer;
    }
}
