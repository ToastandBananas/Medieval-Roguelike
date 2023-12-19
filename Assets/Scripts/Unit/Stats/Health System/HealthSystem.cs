using UnityEngine;
using GridSystem;
using UnitSystem.ActionSystem.UI;
using EffectsSystem;
using UnitSystem.ActionSystem.Actions;
using UnityEditor.Timeline.Actions;

namespace UnitSystem
{
    public class HealthSystem : MonoBehaviour
    {
        public delegate void TakeMeleeDamageHandler();
        public event TakeMeleeDamageHandler OnTakeDamageFromMeleeAttack;

        [SerializeField] float minFallDamageDistance = 1f; // No damage under this distance
        [SerializeField] BodyPart[] bodyParts;

        public bool IsDead { get; private set; }

        float mobilityPercent = 1f;

        Unit unit;

        void Awake()
        {
            unit = GetComponent<Unit>();

            for (int i = 0; i < bodyParts.Length; i++)
                bodyParts[i].InitializeHealth(this);

            if (GetBodyPart(BodyPartType.Head).IsDisabled || GetBodyPart(BodyPartType.Torso).IsDisabled)
                IsDead = true;
        }

        public void DamageAllBodyParts(int damage, Unit attacker)
        {
            // Damage each body part using the ratio of the body part's max health to the torso's max health (the torso will generally have the highest health of any body part)
            float torsoMaxHealth = GetBodyPart(BodyPartType.Torso).MaxHealth.GetValue();
            for (int i = 0; i < bodyParts.Length; i++)
                bodyParts[i].TakeDamage(Mathf.RoundToInt(damage * (bodyParts[i].MaxHealth.GetValue() / torsoMaxHealth)), attacker);
        }

        public void OnHitByMeleeAttack() => OnTakeDamageFromMeleeAttack?.Invoke();

        public void TakeFallDamage(float fallDistance)
        {
            int fallDamage = CalculateFallDamage(fallDistance);
            BodyPart torso = GetBodyPart(BodyPartType.Torso);
            float torsoMaxHealth = torso.MaxHealth.GetValue();

            torso.TakeDamage(fallDamage, null);
            for (int i = 0; i < bodyParts.Length; i++)
            {
                if (bodyParts[i].BodyPartType == BodyPartType.Leg || bodyParts[i].BodyPartType == BodyPartType.Foot)
                    bodyParts[i].TakeDamage(Mathf.RoundToInt(fallDamage * (bodyParts[i].MaxHealth.GetValue() / torsoMaxHealth)), null);
            }
        }

        int CalculateFallDamage(float fallDistance)
        {
            float modifiedMaxFallDistance = CalculateModifiedMaxFallDistance();

            if (fallDistance <= minFallDamageDistance) return 0;
            if (fallDistance >= modifiedMaxFallDistance) return GetBodyPart(BodyPartType.Torso).MaxHealth.GetValue(); // Instant death

            // Calculate the damage percentage based on fall distance
            float damagePercent = (fallDistance - minFallDamageDistance) / (modifiedMaxFallDistance - minFallDamageDistance);
            int damage = Mathf.RoundToInt(damagePercent * GetBodyPart(BodyPartType.Torso).MaxHealth.GetValue());
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
            float strengthBonus = unit.Stats.Strength.GetValue() * strengthFactor;

            // Calculate the carry weight ratio. This can exceed 1 if carrying more than max capacity
            float carryWeightRatio = unit.Stats.CarryWeightRatio;

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

        public void Die(Unit attacker)
        {
            IsDead = true;

            UnitManager.deadNPCs.Add(unit);
            UnitManager.livingNPCs.Remove(unit);
            LevelGrid.RemoveUnitAtGridPosition(unit.GridPosition);

            unit.UnblockCurrentPosition();
            unit.OpportunityAttackTrigger.gameObject.SetActive(false);
            if (unit.UnitInteractable != null)
                unit.UnitInteractable.enabled = true;

            unit.UnitActionHandler.ClearActionQueue(true, true);

            if (attacker != null)
            {
                unit.UnitAnimator.Die(attacker.transform);
                if (attacker.IsPlayer)
                    attacker.UnitActionHandler.PlayerActionHandler.SetDefaultSelectedAction();
            }
            else // Took damage from something like a fall, status effect, etc.
                unit.UnitAnimator.Die(null);
        }

        public void Resurrect()
        {
            IsDead = false;

            UnitManager.deadNPCs.Remove(unit);
            UnitManager.livingNPCs.Add(unit);

            // TODO: Resurrect animation
            // TODO: Get nearest valid position to resurrect on and move them to it

            unit.UpdateGridPosition();
            LevelGrid.AddUnitAtGridPosition(unit.GridPosition, unit);
        }

        public BodyPart GetBodyPart(BodyPartType type, BodyPartSide side = BodyPartSide.NotApplicable, BodyPartIndex bodyPartIndex = BodyPartIndex.Only)
        {
            for (int i = 0; i < bodyParts.Length; i++)
            {
                if (bodyParts[i].BodyPartType == type && bodyParts[i].BodyPartSide == side && bodyParts[i].BodyPartIndex == bodyPartIndex)
                    return bodyParts[i];
            }
            return null;
        }

        public int LegCount()
        {
            int count = 0;
            for (int i = 0; i < bodyParts.Length; i++)
                if (bodyParts[i].BodyPartType == BodyPartType.Leg) count++;
            return count;
        }

        public int FootCount()
        {
            int count = 0;
            for (int i = 0; i < bodyParts.Length; i++)
                if (bodyParts[i].BodyPartType == BodyPartType.Foot) count++;
            return count;
        }

        public BodyPart GetRandomBodyPartToHit(Action_BaseAttack attackAction)
        {
            float totalHitChanceWeight = TotalHitChanceWeight(attackAction);
            float random = Random.Range(0f, totalHitChanceWeight);
            float hitChanceIndex = 0f;

            for (int i = 0; i < bodyParts.Length; i++)
            {
                if (bodyParts[i].IsDisabled) // This body part's health is already at 0, so skip it
                    continue;

                bool weightAdded = false;
                if (attackAction != null && attackAction.AdjustedHitChance_BodyPartTypes != null && attackAction.AdjustedHitChance_Weights != null)
                {
                    for (int j = 0; j < attackAction.AdjustedHitChance_BodyPartTypes.Length; j++)
                    {
                        if (attackAction.AdjustedHitChance_BodyPartTypes[j] == bodyParts[i].BodyPartType)
                        {
                            if (attackAction.AdjustedHitChance_Weights.Length > j)
                                hitChanceIndex += Mathf.RoundToInt(bodyParts[i].HitChanceWeight * attackAction.AdjustedHitChance_Weights[j]);
                            else
                                hitChanceIndex += bodyParts[i].HitChanceWeight;

                            weightAdded = true;
                            break;
                        }
                    }
                }

                if (!weightAdded)
                    hitChanceIndex += bodyParts[i].HitChanceWeight;

                if (random < hitChanceIndex)
                    return bodyParts[i];
            }

            return GetBodyPart(BodyPartType.Torso);
        }

        float TotalHitChanceWeight(Action_BaseAttack attackAction)
        {
            float total = 0f;
            for (int i = 0; i < bodyParts.Length; i++)
            {
                if (bodyParts[i].IsDisabled) // This body part's health is already at 0, so don't try to hit it
                    continue;

                bool weightAdded = false;
                if (attackAction != null && attackAction.AdjustedHitChance_BodyPartTypes != null && attackAction.AdjustedHitChance_Weights != null)
                {
                    for (int j = 0; j < attackAction.AdjustedHitChance_BodyPartTypes.Length; j++)
                    {
                        if (attackAction.AdjustedHitChance_BodyPartTypes[j] == bodyParts[i].BodyPartType)
                        {
                            if (attackAction.AdjustedHitChance_Weights.Length > j)
                                total += Mathf.RoundToInt(bodyParts[i].HitChanceWeight * attackAction.AdjustedHitChance_Weights[j]);
                            else
                                break;

                            weightAdded = true;
                            break;
                        }
                    }
                }

                if (!weightAdded)
                    total += bodyParts[i].HitChanceWeight;
            }

            return total;
        }

        public void AdjustMobilityPercent(float percentAdjustment)
        {
            mobilityPercent += percentAdjustment;
            mobilityPercent = Mathf.Clamp(mobilityPercent, 0f, 1f);
        }

        public BodyPart[] BodyParts => bodyParts;
        public Unit Unit => unit;

        public float MoveCostMobilityMultiplier => 1f + (1f - Unit.HealthSystem.mobilityPercent);

        public bool IsImmobile => mobilityPercent <= 0f;

        public float MinFallDamageDistance => minFallDamageDistance;
    }
}
