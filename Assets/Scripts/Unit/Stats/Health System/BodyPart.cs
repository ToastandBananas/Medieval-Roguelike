using InventorySystem;
using UnitSystem.ActionSystem.UI;
using UnityEngine;
using Utilities;

namespace UnitSystem
{
    public enum BodyPartIndex { Only, First, Second, Third, Fourth }
    public enum BodyPartSide { NotApplicable, Left, Right }
    public enum BodyPartType { Head, Torso, Arm, Leg }

    [System.Serializable]
    public class BodyPart
    {
        [SerializeField] BodyPartIndex bodyPartIndex;
        [SerializeField] BodyPartSide bodyPartSide;
        [SerializeField] BodyPartType bodyPartType;

        [Tooltip("This value will be multiplied by the Unit's Vitality to determine the body part's max health.")]
        [SerializeField] float healthModifier = 2f;

        [Tooltip("This value will determine the percent chance of it being hit by an attack. (hitChanceWeight / TotalHitChanceWeight)")]
        [SerializeField] int hitChanceWeight = 1;

        public HealthSystem HealthSystem { get; private set; }

        int currentHealth;
        readonly IntStat maxHealth = new();

        public void TakeDamage(int damageAmount, Unit attacker)
        {
            if (damageAmount <= 0)
                return;

            int startHealth = currentHealth;
            currentHealth -= damageAmount;

            if (currentHealth < 0)
                currentHealth = 0;

            if (HealthSystem.Unit.IsPlayer)
                ActionSystemUI.UpdateHealthText();

            // SpawnBlood(attackerTransform);

            if (currentHealth == 0)
            {
                if (startHealth > 0)
                    OnDisabled(attacker);
            }
            else if (attacker != null)
                HealthSystem.Unit.UnitAnimator.DoSlightKnockback(attacker.transform);
        }

        public void Heal(int healAmount)
        {
            if (healAmount == 0)
                return;

            int startHealth = currentHealth;
            currentHealth += healAmount;
            if (currentHealth > maxHealth.GetValue())
                currentHealth = maxHealth.GetValue();

            if (startHealth <= 0 && bodyPartType == BodyPartType.Leg)
                HealthSystem.AdjustMobilityPercent(1f / HealthSystem.LegCount());
        }

        void OnDisabled(Unit attacker)
        {
            switch (bodyPartType)
            {
                case BodyPartType.Head:
                    HealthSystem.Die(attacker);
                    break;
                case BodyPartType.Torso:
                    HealthSystem.Die(attacker);
                    break;
                case BodyPartType.Arm:
                    if (bodyPartSide == BodyPartSide.Left)
                    {
                        if (HealthSystem.Unit.UnitEquipment != null && HealthSystem.Unit.UnitMeshManager.leftHeldItem != null)
                            DropItemManager.DropItem(HealthSystem.Unit.UnitEquipment, HealthSystem.Unit.UnitEquipment.LeftHeldItemEquipSlot);
                    }
                    else // Right Arm
                    {
                        if (HealthSystem.Unit.UnitEquipment != null && HealthSystem.Unit.UnitMeshManager.rightHeldItem != null)
                            DropItemManager.DropItem(HealthSystem.Unit.UnitEquipment, HealthSystem.Unit.UnitEquipment.RightHeldItemEquipSlot);
                    }
                    break;
                case BodyPartType.Leg:
                    HealthSystem.AdjustMobilityPercent(-1f / HealthSystem.LegCount());
                    break;
                default:
                    break;
            }
        }

        public bool IsDisabled => currentHealth <= 0;

        public void SetBaseMaxHealth()
        {
            float normalizedHealth = CurrentHealthNormalized;
            maxHealth.SetBaseValue(Mathf.RoundToInt(HealthSystem.Unit.Stats.Vitality.GetValue() * healthModifier));
            currentHealth = Mathf.RoundToInt(maxHealth.GetValue() * normalizedHealth);
        }

        public void AssignHealthSystem(HealthSystem health) => HealthSystem = health;

        public BodyPartIndex BodyPartIndex => bodyPartIndex;
        public BodyPartSide BodyPartSide => bodyPartSide;
        public BodyPartType BodyPartType => bodyPartType;

        public int CurrentHealth => currentHealth;
        public float CurrentHealthNormalized => (float)currentHealth / maxHealth.GetValue();

        public int HitChanceWeight => hitChanceWeight;

        public string Name()
        {
            if (bodyPartSide != BodyPartSide.NotApplicable)
            {
                if (bodyPartIndex == BodyPartIndex.Only)
                    return $"{bodyPartSide} {bodyPartType}";
                else
                    return $"{bodyPartIndex} {bodyPartSide} {bodyPartType}";
            }

            return bodyPartType.ToString();
        }
    }
}
