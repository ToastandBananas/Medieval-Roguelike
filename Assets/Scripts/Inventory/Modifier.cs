using UnityEngine;
using UnitSystem;

namespace InventorySystem
{
    [System.Serializable]
    public class Modifier
    {
        [Header("Stats")]
        [SerializeField] int speed;
        [SerializeField] float percentSpeed;

        [SerializeField] int strength;
        [SerializeField] float percentStrength;

        [Header("Skills")]
        [SerializeField] int axeSkill;
        [SerializeField] float percentAxeSkill;

        [SerializeField] int bowSkill;
        [SerializeField] float percentBowSkill;

        [SerializeField] int crossBowSkill;
        [SerializeField] float percentCrossBowSkill;

        [SerializeField] int daggerSkill;
        [SerializeField] float percentDaggerSkill;

        [SerializeField] int maceSkill;
        [SerializeField] float percentMaceSkill;

        [SerializeField] int polearmSkill;
        [SerializeField] float percentPolearmSkill;

        [SerializeField] int shieldSkill;
        [SerializeField] float percentShieldSkill;

        [SerializeField] int spearSkill;
        [SerializeField] float percentSpearSkill;

        [SerializeField] int swordSkill;
        [SerializeField] float percentSwordSkill;

        [SerializeField] int throwingSkill;
        [SerializeField] float percentThrowingSkill;

        [SerializeField] int warHammerSkill;
        [SerializeField] float percentWarHammerSkill;

        public void ApplyModifiers(Stats stats)
        {
            stats.Speed.AddModifier(speed);
            stats.Speed.AddPercentModifier(percentSpeed);

            stats.Strength.AddModifier(strength);
            stats.Strength.AddPercentModifier(percentStrength);

            stats.AxeSkill.AddModifier(axeSkill);
            stats.AxeSkill.AddPercentModifier(percentAxeSkill);

            stats.BowSkill.AddModifier(bowSkill);
            stats.BowSkill.AddPercentModifier(percentBowSkill);

            stats.CrossbowSkill.AddModifier(crossBowSkill);
            stats.CrossbowSkill.AddPercentModifier(percentCrossBowSkill);

            stats.DaggerSkill.AddModifier(daggerSkill);
            stats.DaggerSkill.AddPercentModifier(percentDaggerSkill);

            stats.MaceSkill.AddModifier(maceSkill);
            stats.MaceSkill.AddPercentModifier(percentMaceSkill);

            stats.PolearmSkill.AddModifier(polearmSkill);
            stats.PolearmSkill.AddPercentModifier(percentPolearmSkill);

            stats.ShieldSkill.AddModifier(shieldSkill);
            stats.ShieldSkill.AddPercentModifier(percentShieldSkill);

            stats.SpearSkill.AddModifier(spearSkill);
            stats.SpearSkill.AddPercentModifier(percentSpearSkill);

            stats.SwordSkill.AddModifier(swordSkill);
            stats.SwordSkill.AddPercentModifier(percentSwordSkill);

            stats.ThrowingSkill.AddModifier(throwingSkill);
            stats.ThrowingSkill.AddPercentModifier(percentThrowingSkill);

            stats.WarHammerSkill.AddModifier(warHammerSkill);
            stats.WarHammerSkill.AddPercentModifier(percentWarHammerSkill);
        }

        public void RemoveModifiers(Stats stats)
        {
            stats.Speed.RemoveModifier(speed);
            stats.Speed.RemovePercentModifier(percentSpeed);

            stats.Strength.RemoveModifier(strength);
            stats.Strength.RemovePercentModifier(percentStrength);

            stats.AxeSkill.RemoveModifier(axeSkill);
            stats.AxeSkill.RemovePercentModifier(percentAxeSkill);

            stats.BowSkill.RemoveModifier(bowSkill);
            stats.BowSkill.RemovePercentModifier(percentBowSkill);

            stats.CrossbowSkill.RemoveModifier(crossBowSkill);
            stats.CrossbowSkill.RemovePercentModifier(percentCrossBowSkill);

            stats.DaggerSkill.RemoveModifier(daggerSkill);
            stats.DaggerSkill.RemovePercentModifier(percentDaggerSkill);

            stats.MaceSkill.RemoveModifier(maceSkill);
            stats.MaceSkill.RemovePercentModifier(percentMaceSkill);

            stats.PolearmSkill.RemoveModifier(polearmSkill);
            stats.PolearmSkill.RemovePercentModifier(percentPolearmSkill);

            stats.ShieldSkill.RemoveModifier(shieldSkill);
            stats.ShieldSkill.RemovePercentModifier(percentShieldSkill);

            stats.SpearSkill.RemoveModifier(spearSkill);
            stats.SpearSkill.RemovePercentModifier(percentSpearSkill);

            stats.SwordSkill.RemoveModifier(swordSkill);
            stats.SwordSkill.RemovePercentModifier(percentSwordSkill);

            stats.ThrowingSkill.RemoveModifier(throwingSkill);
            stats.ThrowingSkill.RemovePercentModifier(percentThrowingSkill);

            stats.WarHammerSkill.RemoveModifier(warHammerSkill);
            stats.WarHammerSkill.RemovePercentModifier(percentWarHammerSkill);
        }
    }
}
