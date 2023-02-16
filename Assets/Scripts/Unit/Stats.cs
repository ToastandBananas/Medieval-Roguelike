using System;
using UnityEngine;

public class Stats : MonoBehaviour
{
    public int currentAP { get; private set; }
    public int pooledAP { get; private set; }
    public int lastUsedAP { get; private set; }
    readonly int baseAP = 60;
    int APUntilTimeTick;

    [Header("Attributes")]
    [SerializeField] IntStat speed;

    [Header("Skills")]
    [SerializeField] IntStat bowSkill;
    [SerializeField] IntStat shieldSkill;
    [SerializeField] IntStat swordSkill;

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

    public int MaxAP() => Mathf.RoundToInt(baseAP + (speed.GetValue() * 1.5f));

    public void UseAP(int amount)
    {
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
        int maxAP = MaxAP();
        int APDifference = maxAP - currentAP;
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

    public int Speed() => speed.GetValue();

    public float ShieldBlockChance(ItemData shieldItemData)
    {
        float blockChance = shieldSkill.GetValue() * 2f;
        blockChance = Mathf.RoundToInt((blockChance + shieldItemData.item.Shield().blockChanceAddOn) * 100f) / 100f;

        if (blockChance < 0f) blockChance = 0f;
        return blockChance;
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
}
