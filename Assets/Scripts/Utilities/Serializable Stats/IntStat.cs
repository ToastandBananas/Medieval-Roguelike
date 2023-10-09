using System.Collections.Generic;
using UnityEngine;

namespace Utilities
{
    [System.Serializable]
    public class IntStat
    {
        [SerializeField] int baseValue = 5;
        [SerializeField] int minValue = 1;

        [SerializeField] List<int> modifiers = new List<int>();
        [SerializeField] List<float> percentModifiers = new List<float>();

        public int GetValue()
        {
            int finalValue = baseValue;
            for (int i = 0; i < modifiers.Count; i++)
            {
                finalValue += modifiers[i];
            }

            float percentModifierTotal = 0f;
            for (int i = 0; i < percentModifiers.Count; i++)
            {
                percentModifierTotal += percentModifiers[i];
            }

            finalValue += Mathf.FloorToInt(baseValue * percentModifierTotal);
            if (finalValue < minValue)
                finalValue = minValue;

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

        public void AddPercentModifier(float percentModifier)
        {
            if (percentModifier != 0f)
                percentModifiers.Add(percentModifier);
        }

        public void RemovePercentModifier(float percentModifier)
        {
            if (percentModifier != 0f)
                percentModifiers.Remove(percentModifier);
        }

        public void ClearModifiers()
        {
            modifiers.Clear();
        }
    }
}
