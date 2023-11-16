using UnityEngine;
using InventorySystem;

namespace UnitSystem
{
    [CreateAssetMenu(fileName = "New Stat Modifier", menuName = "Stat Modifier/Stance Stat Modifier")]
    public class StanceStatModifier_ScriptableObject : StatModifier_ScriptableObject
    {
        [Header("Weapon Stance")]
        [SerializeField] HeldItemStance heldItemStance;

        public HeldItemStance HeldItemStance => heldItemStance;
    }
}
