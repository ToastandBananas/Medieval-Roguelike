using System.Collections;
using UnityEngine;
using ActionSystem;
using InventorySystem;
using Utilities;

namespace UnitSystem
{
    public class UnitAnimator : MonoBehaviour
    {
        [SerializeField] Transform headTransform, bodyTransform;
        [SerializeField] CapsuleCollider baseCapsuleCollider;
        Animator unitAnim;

        public Animator leftHeldItemAnim { get; private set; }
        public Animator rightHeldItemAnim { get; private set; }

        Unit unit;

        void Awake()
        {
            unit = transform.parent.GetComponent<Unit>();
            unitAnim = GetComponent<Animator>();
        }

        public void StartMovingForward() => unitAnim.SetBool("isMoving", true);

        public void StopMovingForward() => unitAnim.SetBool("isMoving", false);

        public void StartMeleeAttack() => unitAnim.Play("Melee Attack");

        public void StartDualMeleeAttack() => unitAnim.Play("Dual Melee Attack");

        public void DoDefaultUnarmedAttack()
        {
            unitAnim.Play("Unarmed Attack");
        }

        // Used in animation Key Frame
        void StopAttacking() => unit.unitActionHandler.SetIsAttacking(false);

        public void DoSlightKnockback(Transform attackerTransform) => StartCoroutine(SlightKnockback(attackerTransform));

        IEnumerator SlightKnockback(Transform attackerTransform)
        {
            float knockbackForce = 0.25f;
            float knockbackDuration = 0.1f;
            float returnDuration = 0.1f;
            float elapsedTime = 0;

            Vector3 originalPosition = unit.transform.position;
            Vector3 knockbackDirection = (originalPosition - attackerTransform.position).normalized;
            Vector3 knockbackTargetPosition = originalPosition + knockbackDirection * knockbackForce;

            // Knockback
            while (elapsedTime < knockbackDuration && unit.health.IsDead() == false)
            {
                elapsedTime += Time.deltaTime;
                unit.transform.position = Vector3.Lerp(originalPosition, knockbackTargetPosition, elapsedTime / knockbackDuration);
                yield return null;
            }

            // Reset the elapsed time for the return movement
            elapsedTime = 0;

            // Return to original position
            while (elapsedTime < returnDuration && unit.health.IsDead() == false && unit.unitActionHandler.isMoving == false)
            {
                elapsedTime += Time.deltaTime;
                unit.transform.position = Vector3.Lerp(knockbackTargetPosition, originalPosition, elapsedTime / returnDuration);
                yield return null;
            }
        }

        public void StopBlocking()
        {
            if (unit.unitMeshManager.leftHeldItem != null && unit.unitMeshManager.leftHeldItem.isBlocking)
                unit.unitMeshManager.leftHeldItem.StopBlocking();
            else if (unit.unitMeshManager.rightHeldItem != null && unit.unitMeshManager.rightHeldItem.isBlocking)
                unit.unitMeshManager.rightHeldItem.StopBlocking();
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

            // Calculate a direction away from the attacker
            Vector3 directionAwayFromAttacker = (transform.position - attackerTransform.position).normalized;

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
                if (helm.FallOffOnDeathChance > 0f && Random.Range(0f, 100f) <= helm.FallOffOnDeathChance)
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

        public void SetLeftHeldItemAnim(Animator leftHeldItemAnim) => this.leftHeldItemAnim = leftHeldItemAnim;

        public void SetRightHeldItemAnim(Animator rightHeldItemAnim) => this.rightHeldItemAnim = rightHeldItemAnim;

        public void SetLeftHeldItemAnimController(RuntimeAnimatorController animController) => leftHeldItemAnim.runtimeAnimatorController = animController;

        public void SetRightHeldItemAnimController(RuntimeAnimatorController animController) => rightHeldItemAnim.runtimeAnimatorController = animController;
    }
}
