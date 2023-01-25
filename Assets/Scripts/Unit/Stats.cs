using UnityEngine;

public class Stats : MonoBehaviour
{
    [Header("AP")]
    int currentAP;
    readonly int baseAP = 60; 
    int APLossBuildup;

    [Header("Attributes")]
    [SerializeField] IntStat speed;

    Unit unit;

    void Awake()
    {
        ReplenishAP();

        unit = GetComponent<Unit>();
    }

    public int CurrentAP() => currentAP;

    public int MaxAP() => Mathf.RoundToInt(baseAP + (speed.GetValue() * 1.5f));

    public void UseAP(int amount)
    {
        // Debug.Log("AP used: " + amount);
        currentAP -= amount;
        //if (IsNPC() == false)
        //gm.healthDisplay.UpdateAPText();
    }

    public void ReplenishAP() => currentAP = MaxAP();

    public void AddToCurrentAP(int amountToAdd) => currentAP += amountToAdd; 

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
}
