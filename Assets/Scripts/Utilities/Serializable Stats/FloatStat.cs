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

        public float GetValue()
        {
            float finalValue = baseValue;
            for (int i = 0; i < modifiers.Count; i++)
            {
                finalValue += modifiers[i];
            }

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

        public void ClearModifiers()
        {
            modifiers.Clear();
        }
    }
}
