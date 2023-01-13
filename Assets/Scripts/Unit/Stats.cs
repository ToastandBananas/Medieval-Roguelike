using System.Buffers.Text;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Stats : MonoBehaviour
{
    [Header("AP")]
    int maxAP = 100;
    int currentAP;
    readonly int baseAP = 25;

    [Header("Attributes")]
    int speed = 10;

    void Awake()
    {
        ReplenishAP();
    }

    public int CurrentAP() => currentAP;

    public int MaxAP()
    {
        //if (speed.GetValue() > 0)
        //return Mathf.RoundToInt(baseAP + (speed.GetValue() * 1.5f));
        //else
        return baseAP;
    }

    public void UseAP(int amount)
    {
        // Debug.Log("AP used: " + amount);
        currentAP -= amount;
        //if (IsNPC() == false)
        //gm.healthDisplay.UpdateAPText();
    }

    public void ReplenishAP()
    {
        currentAP = MaxAP();
    }

    public void AddToCurrentAP(int amountToAdd)
    {
        currentAP += amountToAdd;
    }

    public int UseAPAndGetRemainder(int amount)
    {
        // Debug.Log("Current AP: " + currentAP);
        int remainingAmount = amount;
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
}
