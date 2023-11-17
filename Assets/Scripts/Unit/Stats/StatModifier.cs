using UnityEngine;

namespace UnitSystem
{
    [System.Serializable]
    public class StatModifier
    {
        [Header("Attributes")]
        [SerializeField] int agility;
        [SerializeField] float percentAgility;

        [SerializeField] int endurance;
        [SerializeField] float percentEndurance;

        [SerializeField] int speed;
        [SerializeField] float percentSpeed;

        [SerializeField] int strength;
        [SerializeField] float percentStrength;

        [Header("Defensive Skills")]
        [SerializeField] int shieldSkill;
        [SerializeField] float percentShieldSkill;

        [Header("Ranged Weapon Skills")]
        [SerializeField] int bowSkill;
        [SerializeField] float percentBowSkill;

        [SerializeField] int crossBowSkill;
        [SerializeField] float percentCrossbowSkill;

        [SerializeField] int throwingSkill;
        [SerializeField] float percentThrowingSkill;

        [Header("Melee Weapon Skills")]
        [SerializeField] int unarmedSkill;
        [SerializeField] float percentUnarmedSkill;

        [SerializeField] int axeSkill;
        [SerializeField] float percentAxeSkill;

        [SerializeField] int daggerSkill;
        [SerializeField] float percentDaggerSkill;

        [SerializeField] int maceSkill;
        [SerializeField] float percentMaceSkill;

        [SerializeField] int polearmSkill;
        [SerializeField] float percentPolearmSkill;

        [SerializeField] int spearSkill;
        [SerializeField] float percentSpearSkill;

        [SerializeField] int swordSkill;
        [SerializeField] float percentSwordSkill;

        [SerializeField] int warHammerSkill;
        [SerializeField] float percentWarHammerSkill;

        public void ApplyModifiers(Stats stats)
        {
            // Attributes
            stats.Agility.AddModifier(agility);
            stats.Agility.AddPercentModifier(percentAgility);

            stats.Endurance.AddModifier(endurance);
            stats.Endurance.AddPercentModifier(percentEndurance);

            stats.Speed.AddModifier(speed);
            stats.Speed.AddPercentModifier(percentSpeed);

            stats.Strength.AddModifier(strength);
            stats.Strength.AddPercentModifier(percentStrength);

            // Defensive Skills
            stats.ShieldSkill.AddModifier(shieldSkill);
            stats.ShieldSkill.AddPercentModifier(percentShieldSkill);

            // Ranged Weapon Skills
            stats.BowSkill.AddModifier(bowSkill);
            stats.BowSkill.AddPercentModifier(percentBowSkill);

            stats.CrossbowSkill.AddModifier(crossBowSkill);
            stats.CrossbowSkill.AddPercentModifier(percentCrossbowSkill);

            // Melee Weapon Skills
            stats.UnarmedSkill.AddModifier(unarmedSkill);
            stats.UnarmedSkill.AddPercentModifier(percentUnarmedSkill);

            stats.AxeSkill.AddModifier(axeSkill);
            stats.AxeSkill.AddPercentModifier(percentAxeSkill);

            stats.DaggerSkill.AddModifier(daggerSkill);
            stats.DaggerSkill.AddPercentModifier(percentDaggerSkill);

            stats.MaceSkill.AddModifier(maceSkill);
            stats.MaceSkill.AddPercentModifier(percentMaceSkill);

            stats.PolearmSkill.AddModifier(polearmSkill);
            stats.PolearmSkill.AddPercentModifier(percentPolearmSkill);

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
            // Attributes
            stats.Agility.RemoveModifier(agility);
            stats.Agility.RemovePercentModifier(percentAgility);

            stats.Endurance.RemoveModifier(endurance);
            stats.Endurance.RemovePercentModifier(percentEndurance);

            stats.Speed.RemoveModifier(speed);
            stats.Speed.RemovePercentModifier(percentSpeed);

            stats.Strength.RemoveModifier(strength);
            stats.Strength.RemovePercentModifier(percentStrength);

            // Defensive Skills
            stats.ShieldSkill.RemoveModifier(shieldSkill);
            stats.ShieldSkill.RemovePercentModifier(percentShieldSkill);

            // Ranged Weapon Skills
            stats.BowSkill.RemoveModifier(bowSkill);
            stats.BowSkill.RemovePercentModifier(percentBowSkill);

            stats.CrossbowSkill.RemoveModifier(crossBowSkill);
            stats.CrossbowSkill.RemovePercentModifier(percentCrossbowSkill);

            // Melee Weapon Skills
            stats.AxeSkill.RemoveModifier(axeSkill);
            stats.AxeSkill.RemovePercentModifier(percentAxeSkill);

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

            stats.UnarmedSkill.RemoveModifier(unarmedSkill);
            stats.UnarmedSkill.RemovePercentModifier(percentUnarmedSkill);

            stats.WarHammerSkill.RemoveModifier(warHammerSkill);
            stats.WarHammerSkill.RemovePercentModifier(percentWarHammerSkill);
        }

        // Attributes
        public int Agility => agility;
        public float PercentAgility => percentAgility;
        public int Endurance => endurance;
        public float PercentEndurance => percentEndurance;
        public int Speed => speed;
        public float PercentSpeed => percentSpeed;
        public int Strength => strength;
        public float PercentStrength => percentStrength;

        // Defensive Skills
        public int ShieldSkill => shieldSkill;
        public float PercentShieldSkill => percentShieldSkill;

        // Ranged Weapon Skills
        public int BowSkill => bowSkill;
        public float PercentBowSkill => percentBowSkill;

        public int CrossbowSkill => crossBowSkill;
        public float PercentCrossbowSkill => percentCrossbowSkill;

        public int ThrowingSkill => throwingSkill;
        public float PercentThrowingSkill => percentThrowingSkill;

        // Melee Weapon Skills
        public int UnarmedSkill => unarmedSkill;
        public float PercentUnarmedSkill => percentUnarmedSkill;

        public int AxeSkill => axeSkill;
        public float PercentAxeSkill => percentAxeSkill;

        public int DaggerSkill => daggerSkill;
        public float PercentDaggerSkill => percentDaggerSkill;

        public int MaceSkill => maceSkill;
        public float PercentMaceSkill => percentMaceSkill;

        public int PolearmSkill => polearmSkill;
        public float PercentPolearmSkill => percentPolearmSkill;

        public int SpearSkill => spearSkill;
        public float PercentSpearSkill => percentSpearSkill;

        public int SwordSkill => swordSkill;
        public float PercentSwordSkill => percentSwordSkill;

        public int WarHammerSkill => warHammerSkill;
        public float PercentWarHammerSkill => percentWarHammerSkill;
    }
}
