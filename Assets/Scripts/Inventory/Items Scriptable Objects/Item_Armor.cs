using UnityEngine;

namespace InventorySystem
{
    public abstract class Item_Armor : Item_Wearable
    {
        [Header("Armor Stats")]
        [SerializeField] Vector2Int defenseRange = Vector2Int.one;
        [SerializeField] Vector2Int durabilityRange = Vector2Int.one;

        public float GetMoveNoiseModifier()
        {
            if (this is Item_Boots)
                return Boots.MoveNoiseModifier;
            else if (this is Item_BodyArmor)
                return BodyArmor.MoveNoiseModifier;
            else if (this is Item_LegArmor)
                return LegArmor.MoveNoiseModifier;
            else if (this is Item_Shirt)
                return Shirt.MoveNoiseModifier;
            return 0f;
        }

        public int MinDurability => durabilityRange.x;
        public int MaxDurability => durabilityRange.y;

        public int MinDefense => defenseRange.x;
        public int MaxDefense => defenseRange.y;
    }
}
