using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class IntStat
{
    [SerializeField] int baseValue = 5;

    [SerializeField] List<int> modifiers = new List<int>();

    public int GetValue()
    {
        int finalValue = baseValue;
        for (int i = 0; i < modifiers.Count; i++)
        {
            finalValue += modifiers[i];
        }
        return finalValue;
    }

    public int GetBaseValue()
    {
        return baseValue;
    }

    public void EditBaseValue(int amount)
    {
        baseValue += amount;
    }

    public void SetBaseValue(int value)
    {
        baseValue = value;
    }

    public void AddModifier(int modifier)
    {
        if (modifier != 0)
            modifiers.Add(modifier);
    }

    public void RemoveModifier(int modifier)
    {
        if (modifier != 0)
            modifiers.Remove(modifier);
    }

    public void ClearModifiers()
    {
        modifiers.Clear();
    }
}
