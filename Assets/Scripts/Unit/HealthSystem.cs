using System;
using System.Collections;
using UnityEditor.Experimental.GraphView;
using UnityEngine;

public class HealthSystem : MonoBehaviour
{
    public event EventHandler OnHealthChanged;
    public event EventHandler OnDead;

    [SerializeField] int maxHealth = 100;
    [SerializeField] int currentHealth;

    Unit unit;

    void Awake()
    {
        unit = GetComponent<Unit>();

        if (currentHealth == 0)
            currentHealth = maxHealth;
    }

    public void TakeDamage(int damageAmount, Transform attackerTransform)
    {
        if (damageAmount <= 0)
            return;

        currentHealth -= damageAmount;

        if (currentHealth < 0)
            currentHealth = 0;

        OnHealthChanged?.Invoke(this, EventArgs.Empty);

        // SpawnBlood(attackerTransform);

        if (currentHealth == 0)
            Die(attackerTransform);
        else
            StartCoroutine(SlightKnockback(attackerTransform));
    }

    IEnumerator SlightKnockback(Transform attackerTransform)
    {
        float knockbackForce = 0.25f;
        float knockbackDuration = 0.1f;
        float returnDuration = 0.1f;
        float elapsedTime = 0;

        Vector3 originalPosition = unit.gridPosition.WorldPosition();
        Vector3 knockbackDirection = (originalPosition - attackerTransform.position).normalized;
        Vector3 knockbackTargetPosition = originalPosition + knockbackDirection * knockbackForce;

        // Knockback
        while (elapsedTime < knockbackDuration && IsDead() == false)
        {
            elapsedTime += Time.deltaTime;
            unit.transform.position = Vector3.Lerp(originalPosition, knockbackTargetPosition, elapsedTime / knockbackDuration);
            yield return null;
        }

        // Reset the elapsed time for the return movement
        elapsedTime = 0;

        // Return to original position
        while (elapsedTime < returnDuration && IsDead() == false)
        {
            elapsedTime += Time.deltaTime;
            unit.transform.position = Vector3.Lerp(knockbackTargetPosition, originalPosition, elapsedTime / returnDuration);
            yield return null;
        }
    }

    void SpawnBlood(Transform attackerTransform)
    {
        ParticleSystem blood = ParticleEffectPool.Instance.GetParticleEffectFromPool(ParticleSystemData.ParticleSystemType.BloodSpray);
        blood.transform.position = unit.transform.position + new Vector3(0, unit.ShoulderHeight(), 0);
        
        // Calculate the hit direction from the unit to the enemy.
        Vector3 hitDirection = (unit.transform.position - attackerTransform.position).normalized;

        // Calculate the angle between the forward vector and the hit direction on the XZ plane.
        float angle = Mathf.Atan2(hitDirection.x, hitDirection.z) * Mathf.Rad2Deg;

        // Apply the rotation to the blood particle system.
        blood.transform.rotation = Quaternion.Euler(0, angle, 0);

        blood.gameObject.SetActive(true);
        blood.Play();
    }

    void Die(Transform attackerTransform)
    {
        UnitManager.Instance.deadNPCs.Add(unit);
        UnitManager.Instance.livingNPCs.Remove(unit);

        OnDead?.Invoke(this, EventArgs.Empty);

        unit.unitAnimator.Die(attackerTransform);
    }

    public bool IsDead() => currentHealth <= 0;

    public float CurrentHealthNormalized() => (float)currentHealth / maxHealth;

    public int MaxHealth() => maxHealth;

    public int CurrentHealth() => currentHealth;
}
