using UnityEngine;

namespace InventorySystem
{
    [CreateAssetMenu(fileName = "New Helm", menuName = "Inventory/Helm")]
    public class Helm : Armor
    {
        [Header("Helm Info")]
        [SerializeField, Range(0f, 100f)] float fallOffOnDeathChance;

        public float FallOffOnDeathChance => fallOffOnDeathChance;
    }
}
