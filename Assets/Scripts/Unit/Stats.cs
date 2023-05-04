using System;
using UnityEngine;

public class Stats : MonoBehaviour
{
    public int currentAP { get; private set; }
    public int pooledAP { get; private set; }
    public int APUntilTimeTick { get; private set; }
    public int lastUsedAP { get; private set; }
    readonly int baseAP = 60;

    [Header("Attributes")]
    [SerializeField] IntStat speed;
    [SerializeField] IntStat strength;

    [Header("Skills")]
    [SerializeField] IntStat axeSkill;
    [SerializeField] IntStat bowSkill;
    [SerializeField] IntStat crossbowSkill;
    [SerializeField] IntStat daggerSkill;
    [SerializeField] IntStat maceSkill;
    [SerializeField] IntStat polearmSkill;
    [SerializeField] IntStat shieldSkill;
    [SerializeField] IntStat spearSkill;
    [SerializeField] IntStat swordSkill;
    [SerializeField] IntStat throwingSkill;
    [SerializeField] IntStat warHammerSkill;

    Unit unit;

    void Awake()
    {
        unit = GetComponent<Unit>();
        APUntilTimeTick = MaxAP();
        // currentAP = MaxAP();
    }

    public void SetLastUsedAP(int amountUsed)
    {
        lastUsedAP = amountUsed;
        UnitActionSystemUI.Instance.UpdateActionPoints();
    }

    public int MaxAP() => Mathf.RoundToInt(baseAP + (Speed().GetValue() * 5f));

    public void UseAP(int amount)
    {
        if (amount <= 0)
            return;
        
        if (unit.IsPlayer())
        {
            UpdateAPUntilTimeTick(amount);
            SetLastUsedAP(amount);

            for (int i = 0; i < UnitManager.Instance.livingNPCs.Count; i++)
            {
                UnitManager.Instance.livingNPCs[i].stats.AddToAPPool(Mathf.RoundToInt(UnitManager.Instance.livingNPCs[i].stats.APUsedMultiplier(amount) * UnitManager.Instance.livingNPCs[i].stats.MaxAP()));
                UnitManager.Instance.livingNPCs[i].unitActionHandler.GetAction<MoveAction>().SetMoveSpeed(amount);
            }
        }
        else
        {
            // Debug.Log("AP used: " + amount);
            currentAP -= amount;
            UpdateAPUntilTimeTick(amount);
        }
    }

    public void UpdateAPUntilTimeTick(int amountAPUsed)
    {
        if (amountAPUsed < APUntilTimeTick)
            APUntilTimeTick -= amountAPUsed;
        else
        {
            amountAPUsed -= APUntilTimeTick;
            APUntilTimeTick = MaxAP();
            UpdateUnit();
            UpdateAPUntilTimeTick(amountAPUsed);
        }
    }

    public void ReplenishAP() => currentAP = MaxAP(); 

    public void AddToCurrentAP(int amountToAdd) => currentAP += amountToAdd;

    public void AddToAPPool(int amountToAdd) => pooledAP += amountToAdd;

    public void GetAPFromPool()
    {
        int APDifference = MaxAP() - currentAP;
        if (pooledAP > APDifference)
        {
            pooledAP -= APDifference;
            currentAP += APDifference;
        }
        else
        {
            currentAP += pooledAP;
            pooledAP = 0;
        }
    }

    public int UseAPAndGetRemainder(int amount)
    {
        // Debug.Log("Current AP: " + currentAP);
        int remainingAmount;
        if (currentAP >= amount)
        {
            UseAP(amount);
            remainingAmount = 0;
        }
        else
        {
            remainingAmount = amount - currentAP;
            UseAP(currentAP);
        }

        //Debug.Log("Remaining amount: " + remainingAmount);
        return remainingAmount;
    }

    void UpdateUnit()
    {
        if (unit.IsPlayer())
            TimeSystem.IncreaseTime();

        unit.vision.UpdateVisibleUnits();

        // We already are running this after the Move and Turn actions are complete, so no need to run it again
        if (unit.unitActionHandler.lastQueuedAction is MoveAction == false && unit.unitActionHandler.lastQueuedAction is TurnAction == false)
            unit.vision.FindVisibleUnits();

        //unit.SetHasStartedTurn(true);

        //unit.stats.ReplenishAP();

        /*
        unit.status.UpdateBuffs();
        unit.status.UpdateInjuries();
        unit.status.RegenerateStamina();
        unit.nutrition.DrainStaminaBonus();

        if (unit.unitActionHandler.canPerformActions || unit.IsPlayer())
        {
            unit.nutrition.DrainNourishment();
            unit.nutrition.DrainWater();
            unit.nutrition.DrainNausea();
        }
        */
    }

    public float APUsedMultiplier(int amountAPUsed) => ((float)amountAPUsed) / MaxAP();

    public IntStat Speed() => speed;

    public IntStat Strength() => strength;

    public IntStat AxeSkill() => axeSkill;

    public IntStat BowSkill() => bowSkill;

    public IntStat CrossbowSkill() => crossbowSkill;

    public IntStat DaggerSkill() => daggerSkill;

    public IntStat MaceSkill() => maceSkill;

    public IntStat PolearmSkill() => polearmSkill;

    public IntStat ShieldSkill() => shieldSkill;

    public IntStat SpearSkill() => spearSkill;

    public IntStat SwordSkill() => swordSkill;

    public IntStat ThrowingSkill() => throwingSkill;

    public IntStat WarHammerSkill() => warHammerSkill;

    public int NaturalBlockPower() => Mathf.RoundToInt(strength.GetValue() * 2.5f); 

    public float ShieldBlockChance(HeldShield heldShield, bool attackerBesideUnit)
    {
        float blockChance = shieldSkill.GetValue() * 2f;
        blockChance = Mathf.RoundToInt((blockChance + heldShield.itemData.item.Shield().blockChanceAddOn) * 100f) / 100f;

        if (attackerBesideUnit)
            blockChance *= 0.5f;

        if (blockChance < 0f) blockChance = 0f;
        return blockChance;
    }

    public float WeaponBlockChance(HeldMeleeWeapon heldWeapon, bool attackerBesideUnit, bool shieldEquipped)
    {
        Weapon weapon = heldWeapon.itemData.item.Weapon();
        float blockChance = swordSkill.GetValue() * 2f * WeaponBlockModifier(weapon);
        blockChance = Mathf.RoundToInt((blockChance + weapon.blockChanceAddOn) * 100f) / 100f;

        if (shieldEquipped)
            blockChance *= 0.65f;

        if (attackerBesideUnit)
            blockChance *= 0.5f;
        
        if (blockChance < 0f) blockChance = 0f;
        return blockChance;
    }

    public int ShieldBlockPower(HeldShield heldShield) => NaturalBlockPower() + (ShieldSkill().GetValue() * 2) + heldShield.itemData.blockPower;

    public int WeaponBlockPower(HeldMeleeWeapon heldWeapon)
    {
        Weapon weapon = heldWeapon.itemData.item.Weapon();
        return Mathf.RoundToInt((NaturalBlockPower() + (WeaponSkill(weapon.weaponType) * 2)) * WeaponBlockModifier(weapon));
    }

    public float RangedAccuracy(ItemData rangedWeaponItemData)
    {
        float accuracy = 0f;
        if (rangedWeaponItemData.item.Weapon().weaponType == WeaponType.Bow)
        {
            accuracy = bowSkill.GetValue() * 4f;
            accuracy = Mathf.RoundToInt((accuracy + rangedWeaponItemData.accuracyModifier) * 100f) / 100f;
        }

        if (accuracy < 0f) accuracy = 0f;
        return accuracy;
    }

    public int WeaponSkill(WeaponType weaponType)
    {
        switch (weaponType)
        {
            case WeaponType.Bow:
                return bowSkill.GetValue();
            case WeaponType.Crossbow:
                return crossbowSkill.GetValue();
            case WeaponType.Throwing:
                return throwingSkill.GetValue();
            case WeaponType.Dagger:
                return daggerSkill.GetValue();
            case WeaponType.Sword:
                return swordSkill.GetValue();
            case WeaponType.Axe:
                return axeSkill.GetValue();
            case WeaponType.Mace:
                return maceSkill.GetValue();
            case WeaponType.WarHammer:
                return warHammerSkill.GetValue();
            case WeaponType.Spear:
                return spearSkill.GetValue();
            case WeaponType.Polearm:
                return polearmSkill.GetValue();
            default:
                Debug.LogError(weaponType.ToString() + " has not been implemented in this method. Fix me!");
                return 0;
        }
    }

    public float WeaponBlockModifier(Weapon weapon)
    {
        switch (weapon.weaponType)
        {
            case WeaponType.Throwing:
                return 0.2f;
            case WeaponType.Dagger:
                return 0.4f;
            case WeaponType.Sword:
                if (weapon.isTwoHanded)
                    return 1.45f;
                return 1.3f;
            case WeaponType.Axe:
                if (weapon.isTwoHanded)
                    return 0.9f;
                return 0.8f;
            case WeaponType.Mace:
                if (weapon.isTwoHanded)
                    return 1.2f;
                return 1.1f;
            case WeaponType.WarHammer:
                if (weapon.isTwoHanded)
                    return 0.8f;
                return 0.65f;
            case WeaponType.Spear:
                return 1f;
            case WeaponType.Polearm:
                return 1.1f;
            default:
                return 0f;
        }
    }
}
