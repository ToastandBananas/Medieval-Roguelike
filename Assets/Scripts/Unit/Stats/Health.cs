using UnityEngine;
using GridSystem;
using UnitSystem.ActionSystem.UI;
using EffectsSystem;

namespace UnitSystem
{
    public class Health : MonoBehaviour
    {
        public delegate void TakeMeleeDamageHandler();
        public event TakeMeleeDamageHandler OnTakeDamageFromMeleeAttack;

        [Header("Health")]
        [SerializeField] int maxHealth = 100;
        [SerializeField] int currentHealth = -1;

        Unit unit;

        public static float minFallDistance = 1f; // No damage under this distance

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
            else if (attacker != null)
                unit.unitAnimator.DoSlightKnockback(attacker.transform);
        }

        public void OnHitByMeleeAttack() => OnTakeDamageFromMeleeAttack?.Invoke();

        public void TakeFallDamage(float fallDistance) => TakeDamage(CalculateFallDamage(fallDistance), null);

        int CalculateFallDamage(float fallDistance)
        {
            float modifiedMaxFallDistance = CalculateModifiedMaxFallDistance();

            if (fallDistance <= minFallDistance) return 0;
            if (fallDistance >= modifiedMaxFallDistance) return unit.health.MaxHealth; // Instant death

            // Calculate the damage percentage based on fall distance
            float damagePercent = (fallDistance - minFallDistance) / (modifiedMaxFallDistance - minFallDistance);
            int damage = Mathf.RoundToInt(damagePercent * unit.health.MaxHealth);
            if (damage < 1)
                damage = 1;

            //Debug.Log(unit.name + " fell " + fallDistance + " units for " + damage + " damage");
            return damage;
        }

        float CalculateModifiedMaxFallDistance()
        {
            float baseLethalFallDistance = 6f;
            float strengthFactor = 0.035f;
            float carryWeightFactor = 2f;
            float strengthBonus = unit.stats.Strength.GetValue() * strengthFactor;

            // Calculate the carry weight ratio. This can exceed 1 if carrying more than max capacity
            float carryWeightRatio = unit.stats.CarryWeightRatio;

            // Adjust carry weight penalty to be more severe if carrying beyond max capacity
            float carryWeightPenalty;
            if (carryWeightRatio <= 1) // Up to 100% capacity, use a linear scale
                carryWeightPenalty = carryWeightRatio * carryWeightFactor;
            else // Beyond 100% capacity, you can increase the penalty exponentially
                carryWeightPenalty = Mathf.Pow(carryWeightRatio, 2) * carryWeightFactor;

            //Debug.Log("Strength bonus: " + strengthBonus);
            //Debug.Log("Carry weight penalty: " + carryWeightPenalty);
            //Debug.Log("Max Fall Distance: " + Mathf.Max(baseLethalFallDistance + strengthBonus - carryWeightPenalty, 1));
            return Mathf.Max(baseLethalFallDistance + strengthBonus - carryWeightPenalty, 1); // Ensure it doesn't go below 1
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
            LevelGrid.RemoveUnitAtGridPosition(unit.GridPosition);

            unit.UnblockCurrentPosition();
            unit.unitInteractable.enabled = true;
            unit.opportunityAttackTrigger.gameObject.SetActive(false);

            unit.unitActionHandler.ClearActionQueue(true, true);

            if (attacker != null)
            {
                unit.unitAnimator.Die(attacker.transform);
                if (attacker.IsPlayer)
                    attacker.unitActionHandler.PlayerActionHandler.SetDefaultSelectedAction();
            }
            else // Took damage from something like a fall, status effect, etc.
                unit.unitAnimator.Die(null);
        }

        public bool IsDead => currentHealth <= 0;

        public float CurrentHealthNormalized => (float)currentHealth / maxHealth;

        public int MaxHealth => maxHealth;

        public int CurrentHealth => currentHealth;
    }
}
