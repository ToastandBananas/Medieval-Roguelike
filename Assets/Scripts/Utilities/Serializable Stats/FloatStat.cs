using System.Collections.Generic;
using UnityEngine;

namespace Utilities
{
    [System.Serializable]
    public class FloatStat
    {
        [SerializeField] float baseValue = 50f;
        [SerializeField] float minValue = 1f;

        [SerializeField] List<float> modifiers = new List<float>();
        [SerializeField] List<float> percentModifiers = new List<float>();

        public float GetValue()
        {
            float finalValue = baseValue;
            for (int i = 0; i < modifiers.Count; i++)
            {
                finalValue += modifiers[i];
            }

            float percentModifierTotal = 0f;
            for (int i = 0; i < percentModifiers.Count; i++)
            {
                percentModifierTotal += percentModifiers[i];
            }

            finalValue += Mathf.Round(baseValue * percentModifierTotal);
            if (finalValue < minValue)
                finalValue = minValue;

            return finalValue;
        }

        public float GetBaseValue()
        {
            return baseValue;
        }

        public void EditBaseValue(float amount)
        {
            baseValue += amount;
        }

        public void SetBaseValue(float value)
        {
            baseValue = value;
        }

        public void AddModifier(float modifier)
        {
            if (modifier != 0)
                modifiers.Add(modifier);
        }

        public void RemoveModifier(float modifier)
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
    }
}
