using UnityEngine;

namespace InventorySystem
{
    public abstract class Item_Armor : Item_Wearable
    {
        [Header("Armor Stats")]
        [SerializeField] Vector2Int defenseRange = Vector2Int.one;
        [SerializeField] Vector2Int durabilityRange = Vector2Int.one;

        public int MinDurability => durabilityRange.x;
        public int MaxDurability => durabilityRange.y;

        public int MinDefense => defenseRange.x;
        public int MaxDefense => defenseRange.y;
    }
}
