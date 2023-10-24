using UnityEngine;

namespace InventorySystem
{
    public class Armor : Wearable
    {
        [Header("Armor Stats")]
        [SerializeField] Vector2Int defenseRange;

        public int MinDefense => defenseRange.x;
        public int MaxDefense => defenseRange.y;
    }
}
