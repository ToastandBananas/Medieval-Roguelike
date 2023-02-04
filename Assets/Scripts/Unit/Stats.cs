using UnityEngine;

public class Stats : MonoBehaviour
{
    [Header("AP")]
    int currentAP;
    readonly int baseAP = 60; 
    int APLossBuildup;

    [Header("Attributes")]
    [SerializeField] IntStat speed;

    [Header("Other")]
    [SerializeField] FloatStat rangedAccuracy;

    Unit unit;

    void Awake()
    {
        currentAP = MaxAP();

        unit = GetComponent<Unit>();
    }

    public int CurrentAP() => currentAP;

    public int MaxAP() => Mathf.RoundToInt(baseAP + (speed.GetValue() * 1.5f));

    public void UseAP(int amount)
    {
        // Debug.Log("AP used: " + amount);
        currentAP -= amount;
        UnitActionSystemUI.Instance.UpdateActionPoints();
        //if (unit.IsPlayer())
            //gm.healthDisplay.UpdateAPText();
    }

    public void ReplenishAP()
    {
        currentAP = MaxAP();
        if (unit.IsPlayer())
            UnitActionSystemUI.Instance.UpdateActionPoints();
    }

    public void AddToCurrentAP(int amountToAdd)
    {
        currentAP += amountToAdd;
        if (currentAP > MaxAP())
            ReplenishAP();
        if (unit.IsPlayer())
            UnitActionSystemUI.Instance.UpdateActionPoints();
    }

    public void AddToAPLossBuildup(int amount) => APLossBuildup += amount; 

    public void ApplyAPLossBuildup()
    {
        if (currentAP > APLossBuildup)
        {
            currentAP -= APLossBuildup;
            APLossBuildup = 0;
        }
        else
        {
            APLossBuildup -= currentAP;
            currentAP = 0;
            TurnManager.Instance.FinishTurn(unit);
        }

        if (unit.IsPlayer())
            UnitActionSystemUI.Instance.UpdateActionPoints();
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

    public int Speed() => speed.GetValue();

    public float RangedAccuracy() => rangedAccuracy.GetValue();
}
