using UnityEngine;

namespace InventorySystem
{
    public abstract class Item_Armor : Item_Wearable
    {
        [Header("Armor Stats")]
        [SerializeField] Vector2Int defenseRange = Vector2Int.one;
        [SerializeField] Vector2Int durabilityRange = Vector2Int.one;
        [SerializeField, Range(-1f, 1f)] float minProtection;
        [SerializeField, Range(-1f, 1f)] float maxProtection;

        public float GetMoveCostModifier()
        {
            if (this is Item_Boots)
                return Boots.MoveCostModifier;
            else if (this is Item_BodyArmor)
                return BodyArmor.MoveCostModifier;
            else if (this is Item_LegArmor)
                return LegArmor.MoveCostModifier;
            return 0f;
        }

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

        public float MinProtection => minProtection;
        public float MaxProtection => maxProtection;
    }
}
