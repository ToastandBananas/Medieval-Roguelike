using System.Collections;
using UnityEngine;
using InventorySystem;
using Utilities;
using GeneralUI;
using GridSystem;
using UnitSystem.ActionSystem;

namespace UnitSystem
{
    public class UnitAnimator : MonoBehaviour
    {
        [SerializeField] Transform headTransform, bodyTransform;
        [SerializeField] CapsuleCollider baseCapsuleCollider;
        Animator anim;

        Unit unit;

        public bool beingKnockedBack { get; private set; }
        public bool isDodging { get; private set; }

        void Awake()
        {
            unit = transform.parent.GetComponent<Unit>();
            anim = GetComponent<Animator>();
        }

        public void StartMovingForward() => anim.SetBool("isMoving", true);

        public void StopMovingForward() => anim.SetBool("isMoving", false);

        public void StartMeleeAttack() => anim.Play("Melee Attack");

        public void StartDualMeleeAttack() => anim.Play("Dual Melee Attack");

        public void DoDefaultUnarmedAttack()
        {
            anim.Play("Unarmed Attack");
        }

        // Used in animation Key Frame
        void StopAttacking() => unit.unitActionHandler.SetIsAttacking(false);

        public void DoDodge(Unit attackingUnit, HeldItem heldItemToDodge, Projectile projectileToDodge) => StartCoroutine(Dodge(attackingUnit, heldItemToDodge, projectileToDodge));

        IEnumerator Dodge(Unit attackingUnit, HeldItem heldItemToDodge, Projectile projectileToDodge)
        {
            isDodging = true;

            // Face the attacker
            if (unit.unitActionHandler.turnAction.IsFacingTarget(attackingUnit.GridPosition) == false)
                unit.unitActionHandler.turnAction.RotateTowards_Unit(attackingUnit, false, 30f);

            while (unit.unitActionHandler.turnAction.isRotating)
                yield return null;

            float dodgeDistance;
            if (projectileToDodge != null)
                dodgeDistance = 0.75f;
            else
                dodgeDistance = 0.5f;

            float dodgeDuration = 0.15f;
            float elapsedTime = 0f;

            Vector3 originalPosition = unit.GridPosition.WorldPosition;

            // Choose whether to dodge left or right
            Vector3 dodgeDirection;
            if (heldItemToDodge == null)
                dodgeDirection = Random.Range(0, 2) == 0 ? -unit.transform.right : unit.transform.right;
            else
            {
                if (heldItemToDodge == attackingUnit.unitMeshManager.leftHeldItem)
                    dodgeDirection = -unit.transform.right;
                else
                    dodgeDirection = unit.transform.right;
            }

            dodgeDirection.Normalize();

            Vector3 dodgeTargetPosition = originalPosition + dodgeDirection * dodgeDistance;

            if (projectileToDodge != null)
            {
                // Wait until the arrow is close enough to start our dodge
                while (projectileToDodge.shouldMoveProjectile && Vector3.Distance(unit.transform.position, projectileToDodge.transform.position) > 2f)
                    yield return null;
            }

            // Dodge
            while (beingKnockedBack == false && elapsedTime < dodgeDuration && unit.health.IsDead == false)
            {
                elapsedTime += Time.deltaTime;
                float t = elapsedTime / dodgeDuration;
                unit.transform.position = Vector3.Lerp(originalPosition, dodgeTargetPosition, t);
                yield return null;
            }

            if (beingKnockedBack == false)
            {
                // Wait until the projectile has landed before moving back
                if (projectileToDodge != null)
                {
                    while (projectileToDodge.shouldMoveProjectile)
                        yield return null;
                }

                StartCoroutine(ReturnToOriginalPosition(dodgeTargetPosition, originalPosition, 0.1f));
            }
            else
                isDodging = false;
        }

        public void DoSlightKnockback(Transform attackerTransform) => StartCoroutine(SlightKnockback(attackerTransform));

        IEnumerator SlightKnockback(Transform attackerTransform)
        {
            float knockbackForce = 0.25f;
            float knockbackDuration = 0.1f;
            float elapsedTime = 0f;

            Vector3 originalPosition = unit.GridPosition.WorldPosition;
            Vector3 knockbackDirection = (originalPosition - attackerTransform.position).normalized;
            knockbackDirection.y = 0;
            Vector3 knockbackTargetPosition = originalPosition + knockbackDirection * knockbackForce;

            // Knockback
            while (beingKnockedBack == false && elapsedTime < knockbackDuration && unit.health.IsDead == false)
            {
                elapsedTime += Time.deltaTime;
                unit.transform.position = Vector3.Lerp(originalPosition, knockbackTargetPosition, elapsedTime / knockbackDuration);
                yield return null;
            }

            if (beingKnockedBack == false)
                StartCoroutine(ReturnToOriginalPosition(knockbackTargetPosition, originalPosition, 0.1f));
        }

        public void Knockback(Unit attackingUnit) => StartCoroutine(DoKnockback(attackingUnit));

        IEnumerator DoKnockback(Unit attackingUnit)
        {
            unit.unitActionHandler.CancelActions();

            Vector3 knockbackTargetPosition;
            if (unit.unitActionHandler.moveAction.isMoving)
                knockbackTargetPosition = unit.unitActionHandler.moveAction.lastGridPosition.WorldPosition;
            else
            {
                Vector3 direction = (unit.WorldPosition - attackingUnit.WorldPosition).normalized;
                direction.y = 0;
                float maxKnockbackY = 0.33f;
                knockbackTargetPosition = unit.transform.position + direction;
                if (Physics.Raycast(knockbackTargetPosition + (maxKnockbackY * Vector3.up), -Vector3.up, out RaycastHit hit, 1000f, WorldMouse.MousePlaneLayerMask))
                    knockbackTargetPosition.Set(Mathf.RoundToInt(knockbackTargetPosition.x), hit.point.y, Mathf.RoundToInt(knockbackTargetPosition.z));
                else
                    knockbackTargetPosition.Set(Mathf.RoundToInt(knockbackTargetPosition.x), knockbackTargetPosition.y, Mathf.RoundToInt(knockbackTargetPosition.z));

                // Don't knockback if the position behind this Unit is obstructed
                if (LevelGrid.GridPositionObstructed(LevelGrid.GetGridPosition(knockbackTargetPosition)))
                    yield break;
            }

            float fallDistance = unit.transform.position.y - knockbackTargetPosition.y;
            if (fallDistance < 0f)
                fallDistance = 0f;

            beingKnockedBack = true;
            Vector3 startPosition = unit.transform.position;
            unit.UnblockCurrentPosition();

            // Wait a frame to allow projectiles to "Arrive" properly before moving
            yield return null;

            StopMovingForward();

            // Don't play the animation if the NPC is off-screen
            if (unit.IsNPC && unit.unitMeshManager.IsVisibleOnScreen == false)
            {
                CompleteKnockback(knockbackTargetPosition, fallDistance);
                yield break;
            }

            float speed = 25f;
            float arcMultiplier = 2f;
            if (fallDistance > 0f)
                arcMultiplier *= 2f;

            float arcHeight = MathParabola.CalculateParabolaArcHeight(unit.transform.position, knockbackTargetPosition) * arcMultiplier;
            float animationTime = 0f;

            while (true)
            {
                animationTime += speed * Time.deltaTime;
                Vector3 nextPosition = MathParabola.Parabola(startPosition, knockbackTargetPosition, arcHeight, animationTime / 5f);
                unit.transform.position = nextPosition;

                if (Vector3.Distance(unit.transform.position, knockbackTargetPosition) <= 0.1f)
                    break;

                yield return null;
            }

            CompleteKnockback(knockbackTargetPosition, fallDistance);
        }

        void CompleteKnockback(Vector3 knockbackTargetPosition, float fallDistance)
        {
            unit.transform.position = knockbackTargetPosition;
            unit.UpdateGridPosition();

            unit.unitActionHandler.moveAction.SetFinalTargetGridPosition(unit.GridPosition);
            unit.unitActionHandler.moveAction.SetTargetGridPosition(unit.GridPosition);

            beingKnockedBack = false;

            if (fallDistance > Health.minFallDistance)
                unit.health.TakeFallDamage(fallDistance);
        }

        IEnumerator ReturnToOriginalPosition(Vector3 currentPosition, Vector3 originalPosition, float returnDuration)
        {
            // Return to original position
            float elapsedTime = 0f;
            while (beingKnockedBack == false && elapsedTime < returnDuration && unit.health.IsDead == false && unit.unitActionHandler.moveAction.isMoving == false)
            {
                elapsedTime += Time.deltaTime;
                unit.transform.position = Vector3.Lerp(currentPosition, originalPosition, elapsedTime / returnDuration);
                yield return null;
            }

            if (isDodging)
                isDodging = false;
        }

        public void StopBlocking()
        {
            if (unit.unitMeshManager.leftHeldItem != null)
            {
                if (unit.unitMeshManager.leftHeldItem.IsBlocking && unit.unitMeshManager.leftHeldItem.CurrentHeldItemStance != HeldItemStance.RaiseShield)
                    unit.unitMeshManager.leftHeldItem.StopBlocking();
            }
            else if (unit.unitMeshManager.rightHeldItem != null)
            {
                if (unit.unitMeshManager.rightHeldItem.IsBlocking && unit.unitMeshManager.rightHeldItem.CurrentHeldItemStance != HeldItemStance.RaiseShield)
                  unit.unitMeshManager.rightHeldItem.StopBlocking();
            }
        }

        public void Die(Transform attackerTransform)
        {
            // Hide the Unit's base
            unit.unitMeshManager.DisableBaseMeshRenderer();
            baseCapsuleCollider.enabled = true;

            float forceMagnitude = Random.Range(100, 300);
            float towardsAttackerChance = 0.33f;
            Vector3 randomDirection = Random.onUnitSphere;
            randomDirection.y = 0; // Ensure the force is applied horizontally

            // Calculate a direction away from the attacker (or choose a random direction if attacker is null)
            Vector3 directionAwayFromAttacker;
            if (attackerTransform != null)
                directionAwayFromAttacker = (transform.position - attackerTransform.position).normalized;
            else
                directionAwayFromAttacker = new Vector3(Random.Range(-1f, 1f), 0f, Random.Range(-1f, 1f)).normalized;

            // Interpolate between randomDirection and directionAwayFromAttacker
            Vector3 forceDirection = Vector3.Lerp(randomDirection, directionAwayFromAttacker, towardsAttackerChance);

            // Check if the character died forward
            bool diedForward = Vector3.Dot(forceDirection, randomDirection) > 0.0f;

            // Apply head rotation
            StartCoroutine(Die_RotateHead(diedForward));

            unit.rigidBody.useGravity = true;
            unit.rigidBody.isKinematic = false;
            
            // Apply force to the character's Rigidbody at the specified position
            unit.rigidBody.AddForce(forceDirection * forceMagnitude, ForceMode.Impulse); 
            
            Vector3 torqueDirection = new Vector3(Random.Range(-1f, 1f), 0, Random.Range(-1f, 1f)).normalized;
            float torqueMagnitude = Random.Range(100, 500); // Adjust this value as needed
            unit.rigidBody.AddTorque(torqueDirection * torqueMagnitude, ForceMode.Impulse);

            if (unit.UnitEquipment.EquipSlotHasItem(EquipSlot.Helm))
            {
                Helm helm = unit.UnitEquipment.EquippedItemDatas[(int)EquipSlot.Helm].Item as Helm;
                if (helm.FallOffOnDeathChance > 0f && Random.Range(0f, 1f) <= helm.FallOffOnDeathChance)
                    DropItemManager.DropHelmOnDeath(unit.UnitEquipment.EquippedItemDatas[(int)EquipSlot.Helm], unit, attackerTransform, diedForward);
            }

            if (unit.unitMeshManager.leftHeldItem != null)
                DropItemManager.DropHeldItemOnDeath(unit.unitMeshManager.leftHeldItem, unit, attackerTransform, diedForward);

            if (unit.unitMeshManager.rightHeldItem != null)
                DropItemManager.DropHeldItemOnDeath(unit.unitMeshManager.rightHeldItem, unit, attackerTransform, diedForward);

            // Swap to the other weapon set so that when we go to loot this Unit's body, it will show the items in their equipment
            unit.UnitEquipment.SwapWeaponSet();
        }

        IEnumerator Die_RotateHead(bool diedForward)
        {
            float targetRotationY;
            if (diedForward)
            {
                if (Random.value <= 0.5f)
                    targetRotationY = Random.Range(-90f, -60f);
                else
                    targetRotationY = Random.Range(60f, 90f);
            }
            else
            {
                if (Random.value <= 0.5f)
                    targetRotationY = Random.Range(-90f, -1f);
                else
                    targetRotationY = Random.Range(1f, 90f);
            }

            // Rotation speed in degrees per second
            float rotateSpeed = 150f;

            // The target rotation
            Quaternion targetRotation = Quaternion.Euler(headTransform.localEulerAngles.x, targetRotationY, headTransform.localEulerAngles.z);

            while (true)
            {
                // Rotate the object towards the target rotation
                headTransform.localRotation = Quaternion.RotateTowards(headTransform.localRotation, targetRotation, rotateSpeed * Time.deltaTime);

                // Check if the object has reached the target rotation
                if (Quaternion.Angle(headTransform.localRotation, targetRotation) < 0.1f)
                {
                    headTransform.localRotation = targetRotation;
                    break;
                }

                yield return null;
            }
        }

        // Actually rotates the base
        IEnumerator Die_RotateBody(bool diedForward)
        {
            float targetRotationY;
            if (Random.value <= 0.5f)
                targetRotationY = Random.Range(-40f, -15f);
            else
                targetRotationY = Random.Range(15f, 40f);

            float targetRotationZ;
            if (diedForward)
                targetRotationZ = 90f;
            else
                targetRotationZ = -90f;

            // Rotation speed in degrees per second
            Vector2 rotationSpeed = new Vector2(
                Mathf.Abs(targetRotationY - bodyTransform.localEulerAngles.y) / AnimationTimes.Instance.DeathTime(),
                Mathf.Abs(targetRotationZ - bodyTransform.localEulerAngles.z) / AnimationTimes.Instance.DeathTime()
            );

            // The target rotation
            Quaternion targetRotation = Quaternion.Euler(bodyTransform.localEulerAngles.x, targetRotationY, targetRotationZ);

            while (true)
            {
                // Rotate the object towards the target rotation
                bodyTransform.localRotation = Quaternion.RotateTowards(bodyTransform.localRotation, targetRotation, Mathf.Max(rotationSpeed.x, rotationSpeed.y) * Time.deltaTime);

                // Check if the object has reached the target rotation
                if (Quaternion.Angle(bodyTransform.localRotation, targetRotation) < 0.1f)
                {
                    bodyTransform.localRotation = targetRotation;
                    break;
                }

                yield return null;
            }
        }
    }
}
