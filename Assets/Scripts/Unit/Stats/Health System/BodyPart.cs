using InventorySystem;
using UnitSystem.ActionSystem.UI;
using UnitSystem.UI;
using UnityEngine;
using Utilities;

namespace UnitSystem
{
    public enum BodyPartType { Head, Torso, Arm, Leg, Hand, Foot }
    public enum BodyPartSide { NotApplicable, Left, Right }
    public enum BodyPartIndex { Only, First, Second, Third, Fourth }

    [System.Serializable]
    public class BodyPart
    {
        [SerializeField] BodyPartIndex bodyPartIndex;
        [SerializeField] BodyPartSide bodyPartSide;
        [SerializeField] BodyPartType bodyPartType;

        [Tooltip("This value will be multiplied by the Unit's Vitality to determine the body part's max health.")]
        [SerializeField] float healthModifier = 2f;

        [Tooltip("This value will determine the percent chance of it being hit by an attack. (hitChanceWeight / TotalHitChanceWeight)")]
        [SerializeField] float hitChanceWeight = 1f;

        public HealthSystem HealthSystem { get; private set; }

        int currentHealth;
        readonly IntStat maxHealth = new();

        public void InitializeHealth(HealthSystem healthSystem)
        {
            HealthSystem = healthSystem;
            SetBaseMaxHealth();
            currentHealth = maxHealth.GetValue();
        }

        public void TakeDamage(int damageAmount, Unit attacker)
        {
            if (damageAmount <= 0)
            {
                if (attacker != null)
                    HealthSystem.Unit.UnitAnimator.DoSlightKnockback(attacker.transform);
                return;
            }

            HealthSystem.Unit.ShowFloatingStatBars();

            int startHealth = currentHealth;
            float startNormalizedHealth = CurrentHealthNormalized;
            currentHealth -= damageAmount;

            if (currentHealth < 0)
                currentHealth = 0;

            if (HealthSystem.Unit.IsPlayer)
                StatBarManager_Player.UpdateHealthBar(bodyPartType, bodyPartSide, startNormalizedHealth);
            else if (HealthSystem.Unit.StatBarManager != null)
                HealthSystem.Unit.StatBarManager.UpdateHealthBar(bodyPartType, startNormalizedHealth);

            // SpawnBlood(attackerTransform);

            if (currentHealth == 0)
            {
                if (startHealth > 0)
                {
                    OnDisabled(attacker);
                    if (HealthSystem.Unit.StatBarManager != null)
                        HealthSystem.Unit.StatBarManager.Hide();
                }
            }
            else if (attacker != null)
                HealthSystem.Unit.UnitAnimator.DoSlightKnockback(attacker.transform);
        }

        public void Heal(int healAmount)
        {
            if (healAmount == 0)
                return;

            HealthSystem.Unit.ShowFloatingStatBars();

            int startHealth = currentHealth;
            float startNormalizedHealth = CurrentHealthNormalized;
            currentHealth += healAmount;
            if (currentHealth > maxHealth.GetValue())
                currentHealth = maxHealth.GetValue();

            if (HealthSystem.Unit.IsPlayer)
                StatBarManager_Player.UpdateHealthBar(bodyPartType, bodyPartSide, startNormalizedHealth);
            else if (HealthSystem.Unit.StatBarManager != null)
                HealthSystem.Unit.StatBarManager.UpdateHealthBar(bodyPartType, startNormalizedHealth);

            if (startHealth <= 0 && currentHealth > 0)
                OnEnabled();
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
                    DropHeldItem();
                    break;
                case BodyPartType.Hand:
                    DropHeldItem();
                    break;
                case BodyPartType.Leg:
                    HealthSystem.AdjustMobilityPercent(-1f / (HealthSystem.LegCount() + HealthSystem.FootCount()));
                    break;
                case BodyPartType.Foot:
                    HealthSystem.AdjustMobilityPercent(-1f / (HealthSystem.LegCount() + HealthSystem.FootCount()));
                    break;
                default:
                    break;
            }
        }

        void OnEnabled()
        {
            switch (bodyPartType)
            {
                case BodyPartType.Head:
                    HealthSystem.Resurrect();
                    break;
                case BodyPartType.Torso:
                    HealthSystem.Resurrect();
                    break;
                case BodyPartType.Arm:
                    break;
                case BodyPartType.Hand:
                    break;
                case BodyPartType.Leg:
                    HealthSystem.AdjustMobilityPercent(1f / (HealthSystem.LegCount() + HealthSystem.FootCount()));
                    break;
                case BodyPartType.Foot:
                    HealthSystem.AdjustMobilityPercent(1f / (HealthSystem.LegCount() + HealthSystem.FootCount()));
                    break;
                default:
                    break;
            }
        }

        void DropHeldItem()
        {
            if (bodyPartSide == BodyPartSide.Left) // Left Arm or Hand
            {
                if (HealthSystem.Unit.UnitEquipment != null && HealthSystem.Unit.UnitMeshManager.leftHeldItem != null)
                    DropItemManager.DropItem(HealthSystem.Unit.UnitEquipment, HealthSystem.Unit.UnitEquipment.HumanoidEquipment.LeftHeldItemEquipSlot);
            }
            else // Right Arm or Hand
            {
                if (HealthSystem.Unit.UnitEquipment != null && HealthSystem.Unit.UnitMeshManager.rightHeldItem != null)
                {
                    if (HealthSystem.Unit.UnitMeshManager.rightHeldItem.ItemData.Item is Item_Weapon && HealthSystem.Unit.UnitMeshManager.rightHeldItem.ItemData.Item.Weapon.IsTwoHanded)
                        DropItemManager.DropItem(HealthSystem.Unit.UnitEquipment, HealthSystem.Unit.UnitEquipment.HumanoidEquipment.LeftHeldItemEquipSlot);
                    else
                        DropItemManager.DropItem(HealthSystem.Unit.UnitEquipment, HealthSystem.Unit.UnitEquipment.HumanoidEquipment.RightHeldItemEquipSlot);
                }
            }
        }

        public bool IsDisabled => currentHealth <= 0;

        public void SetBaseMaxHealth()
        {
            float normalizedHealth = CurrentHealthNormalized;
            maxHealth.SetBaseValue(Mathf.RoundToInt(HealthSystem.Unit.Stats.Vitality.GetValue() * healthModifier));
            currentHealth = Mathf.RoundToInt(maxHealth.GetValue() * normalizedHealth);
        }

        public BodyPartIndex BodyPartIndex => bodyPartIndex;
        public BodyPartSide BodyPartSide => bodyPartSide;
        public BodyPartType BodyPartType => bodyPartType;

        public int CurrentHealth => currentHealth;
        public float CurrentHealthNormalized => (float)currentHealth / maxHealth.GetValue();

        public float HitChanceWeight => hitChanceWeight;

        public IntStat MaxHealth => maxHealth;

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
