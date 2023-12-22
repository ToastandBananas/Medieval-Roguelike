using InventorySystem;
using UnityEngine;
namespace UnitSystem.UI
{
    public class ArmorDurabilityBar : StatBar
    {
        [SerializeField] EquipSlot equipSlot;

        public void Initialize()
        {
            UpdateValue();
        }

        public override void UpdateValue()
        {
            if (UnitManager.player.UnitEquipment.EquipSlotHasItem(equipSlot))
            {
                ItemData equipmentItemData = UnitManager.player.UnitEquipment.EquippedItemDatas[(int)equipSlot];
                slider.value = equipmentItemData.CurrentDurabilityNormalized;
                textMesh.text = $"{Mathf.CeilToInt(equipmentItemData.CurrentDurability)}/{equipmentItemData.MaxDurability}";
            }
            else
            {
                slider.value = 0;
                textMesh.text = "-";
            }
        }

        public EquipSlot EquipSlot => equipSlot;
    }
}
