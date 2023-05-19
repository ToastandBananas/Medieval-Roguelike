using System.Collections;
using UnityEngine;

public class UnitAnimator : MonoBehaviour
{
    [SerializeField] Transform headTransform, bodyTransform;
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
        Unit targetUnit = unit.unitActionHandler.targetEnemyUnit;
        HeldItem itemBlockedWith = null;

        // The targetUnit tries to block and if they're successful, the weapon/shield they blocked with is added as a corresponding Value in the attacking Unit's targetUnits dictionary
        bool attackBlocked = targetUnit.TryBlockMeleeAttack(unit);
        unit.unitActionHandler.targetUnits.TryGetValue(targetUnit, out itemBlockedWith);

        if (attackBlocked)
        {
            // Target Unit rotates towards this Unit & does block animation
            targetUnit.unitActionHandler.GetAction<TurnAction>().RotateTowards_Unit(unit, false);

            if (itemBlockedWith is HeldShield)
                targetUnit.GetShield().RaiseShield();
            else
            {
                HeldMeleeWeapon heldWeapon = itemBlockedWith as HeldMeleeWeapon;
                heldWeapon.RaiseWeapon();
            }
        }

        unitAnim.Play("Unarmed Attack");
    }

    // Used in animation Key Frame
    void DamageTargetUnit_UnarmedAttack()
    {
        unit.unitActionHandler.GetAction<MeleeAction>().DamageTargets(null);
    }

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
        while (elapsedTime < returnDuration && unit.health.IsDead() == false && unit.unitActionHandler.GetAction<MoveAction>().isMoving == false)
        {
            elapsedTime += Time.deltaTime;
            unit.transform.position = Vector3.Lerp(knockbackTargetPosition, originalPosition, elapsedTime / returnDuration);
            yield return null;
        }
    }

    public void Die(Transform attackerTransform)
    {
        bool diedForward = Random.value <= 0.5f;
        if (diedForward)
            unitAnim.Play("Die Forward");
        else
            unitAnim.Play("Die Backward");

        StartCoroutine(Die_RotateHead(diedForward));
        StartCoroutine(Die_RotateBody(diedForward));

        if (unit.leftHeldItem != null)
            Die_DropHeldItem(unit.leftHeldItem, attackerTransform, diedForward);

        if (unit.rightHeldItem != null)
            Die_DropHeldItem(unit.rightHeldItem, attackerTransform, diedForward);
    }

    void Die_DropHeldItem(HeldItem heldItem, Transform attackerTransform, bool diedForward)
    {
        LooseItem looseProjectile = null;
        LooseItem looseWeapon = LooseItemPool.Instance.GetLooseItemFromPool();
        Item item = heldItem.itemData.item;

        SetupItemDrop(heldItem.transform, looseWeapon, item);

        if (heldItem is HeldRangedWeapon)
        {
            HeldRangedWeapon heldRangedWeapon = heldItem as HeldRangedWeapon;
            if (heldRangedWeapon.isLoaded)
            {
                looseProjectile = LooseItemPool.Instance.GetLooseItemFromPool();
                Item projectileItem = heldRangedWeapon.loadedProjectile.itemData.item;
                SetupItemDrop(heldRangedWeapon.loadedProjectile.transform, looseProjectile, projectileItem);
                heldRangedWeapon.loadedProjectile.Disable();
            }
        }

        // Remove references to weapon renderers for the dying unit
        unit.RemoveAllWeaponRenderers();

        // Get rid of the HeldItem
        Destroy(heldItem.gameObject);

        float randomForceMagnitude = Random.Range(100f, 600f);
        float randomAngleRange = Random.Range(-25f, 25f); // Random angle range in degrees

        // Get the attacker's position and the character's position
        Vector3 attackerPosition = attackerTransform.position;
        Vector3 unitPosition = transform.parent.position;

        // Calculate the force direction (depending on whether they fall forward or backward)
        Vector3 forceDirection;
        if (diedForward)
            forceDirection = (attackerPosition - unitPosition).normalized;
        else
            forceDirection = (unitPosition - attackerPosition).normalized;

        // Add some randomness to the force direction
        Quaternion randomRotation = Quaternion.Euler(0, randomAngleRange, 0);
        forceDirection = randomRotation * forceDirection;

        // Get the Rigidbody component(s) and apply force
        looseWeapon.RigidBody().AddForce(forceDirection * randomForceMagnitude, ForceMode.Impulse);
        if (looseProjectile != null)
            looseProjectile.RigidBody().AddForce(forceDirection * randomForceMagnitude, ForceMode.Impulse);

        if (unit != UnitManager.Instance.player && UnitManager.Instance.player.vision.IsVisible(unit) == false)
        {
            looseWeapon.HideMeshRenderer();
            if (looseProjectile != null)
                looseProjectile.HideMeshRenderer();
        }

        // StartCoroutine(DelayStopPhysicsMovements(looseItem));
    }

    void SetupItemDrop(Transform itemDropTransform, LooseItem looseItem, Item item)
    {
        if (item.pickupMesh != null)
            looseItem.SetupMesh(item.pickupMesh, item.pickupMeshRendererMaterial);
        else if (item.meshes[0] != null)
            looseItem.SetupMesh(item.meshes[0], item.meshRendererMaterials[0]);
        else
            Debug.LogWarning("Mesh Info has not been set on the ScriptableObject for: " + item.name);

        // Set the LooseItem's position to match the HeldItem before we add force
        looseItem.transform.position = itemDropTransform.position;
        looseItem.gameObject.SetActive(true);
    }

    IEnumerator DelayStopPhysicsMovements(LooseItem looseItem)
    {
        Rigidbody rigidBody = looseItem.RigidBody();
        float threshold = 0.01f;
        float timer = 1f;

        while (true)
        {
            if (timer > 0)
                timer -= Time.deltaTime;
            else if (rigidBody.velocity.sqrMagnitude < threshold * threshold && rigidBody.angularVelocity.sqrMagnitude < threshold * threshold)
            {
                rigidBody.isKinematic = true;
                rigidBody.collisionDetectionMode = CollisionDetectionMode.Discrete;
                break;
            }

            yield return null;
        }
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
