using UnityEngine;
using GridSystem;

public class Health : MonoBehaviour
{
    [Header("Health")]
    [SerializeField] int maxHealth = 100;
    [SerializeField] int currentHealth = -1;

    Unit unit;

    void Awake()
    {
        unit = GetComponent<Unit>();

        if (currentHealth == -1)
            currentHealth = maxHealth;
    }

    public void TakeDamage(int damageAmount, Unit attacker)
    {
        if (damageAmount <= 0)
            return;

        currentHealth -= damageAmount;

        if (currentHealth < 0)
            currentHealth = 0;

        if (unit.IsPlayer)
            ActionSystemUI.UpdateHealthText();

        // SpawnBlood(attackerTransform);

        if (currentHealth == 0)
            Die(attacker);
        else
            unit.unitAnimator.DoSlightKnockback(attacker.transform);
    }

    public void IncreaseHealth(int healAmount)
    {
        currentHealth += healAmount;
        if (currentHealth > maxHealth)
            currentHealth = maxHealth;

        if (unit.IsPlayer)
            ActionSystemUI.UpdateHealthText();
    }

    void SpawnBlood(Transform attackerTransform)
    {
        ParticleSystem blood = ParticleEffectPool.Instance.GetParticleEffectFromPool(ParticleSystemData.ParticleSystemType.BloodSpray);
        blood.transform.position = unit.transform.position + new Vector3(0, unit.ShoulderHeight, 0);
        
        // Calculate the hit direction from the unit to the enemy.
        Vector3 hitDirection = (unit.transform.position - attackerTransform.position).normalized;

        // Calculate the angle between the forward vector and the hit direction on the XZ plane.
        float angle = Mathf.Atan2(hitDirection.x, hitDirection.z) * Mathf.Rad2Deg;

        // Apply the rotation to the blood particle system.
        blood.transform.rotation = Quaternion.Euler(0, angle, 0);

        blood.gameObject.SetActive(true);
        blood.Play();
    }

    void Die(Unit attacker)
    {
        UnitManager.deadNPCs.Add(unit);
        UnitManager.livingNPCs.Remove(unit);
        LevelGrid.Instance.RemoveUnitAtGridPosition(unit.GridPosition());

        unit.UnblockCurrentPosition();
        unit.deadUnit.enabled = true;

        unit.unitAnimator.Die(attacker.transform);

        if (attacker.IsPlayer)
            attacker.unitActionHandler.SetDefaultSelectedAction();
    }

    public bool IsDead() => currentHealth <= 0;

    public float CurrentHealthNormalized() => (float)currentHealth / maxHealth;

    public int MaxHealth() => maxHealth;

    public int CurrentHealth() => currentHealth;
}
