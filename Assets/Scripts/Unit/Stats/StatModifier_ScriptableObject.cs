using UnityEngine;

namespace UnitSystem
{
    [CreateAssetMenu(fileName = "New Stat Modifier", menuName = "Stat Modifier/Basic Stat Modifier")]
    public class StatModifier_ScriptableObject : ScriptableObject
    {
        [Header("Modifier")]
        [SerializeField] StatModifier statModifier;

        public StatModifier StatModifier => statModifier;
    }
}
