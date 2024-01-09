using InventorySystem;
using UnityEngine;

namespace UnitSystem.UI
{
    public class StatBar_Armor : StatBar
    {
        [SerializeField] EquipSlot equipSlot;

        public override void Initialize(Unit unit)
        {
            base.Initialize(unit);
            if (unit.UnitEquipment.EquipSlotHasItem(equipSlot))
                UpdateValue(unit.UnitEquipment.EquippedItemData(equipSlot).CurrentDurabilityNormalized);
            else
                UpdateValue(0);
        }

        public override void UpdateValue(float startNormalizedDurability)
        {
            base.UpdateValue(startNormalizedDurability);
            if (unit.UnitEquipment.EquipSlotHasItem(equipSlot))
            {
                ItemData equipmentItemData = unit.UnitEquipment.EquippedItemData(equipSlot);
                slider.value = equipmentItemData.CurrentDurabilityNormalized;
                if (textMesh != null)
                    textMesh.text = $"{Mathf.CeilToInt(equipmentItemData.CurrentDurability)}/{equipmentItemData.MaxDurability}";
            }
            else
            {
                slider.value = 0;
                if (textMesh != null)
                    textMesh.text = "-";
            }
        }

        public EquipSlot EquipSlot => equipSlot;
    }
}
