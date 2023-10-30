using ActionSystem;
using InventorySystem;
using UnitSystem;
using UnityEngine;

namespace GeneralUI 
{
    public class TooltipManager : MonoBehaviour
    {
        public static TooltipManager Instance;

        [SerializeField] Tooltip[] tooltips;

        public static Slot currentSlot { get; private set; }
        public static ActionBarSlot currentActionBarSlot { get; private set; }

        void Awake()
        {
            if (Instance != null)
            {
                Debug.LogError("There's more than one TooltipManager! " + transform + " - " + Instance);
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        public static void ClearTooltips()
        {
            for (int i = 0; i < Instance.tooltips.Length; i++)
            {
                Instance.tooltips[i].ClearTooltip();
            }

            currentSlot = null;
            currentActionBarSlot = null;
        }

        public static void ShowInventoryTooltips(Slot slot)
        {
            GetTooltip().ShowInventoryTooltip(slot);

            if (slot.GetItemData().Item is Equipment == false)
                return;

            if (slot is EquipmentSlot == false || slot.InventoryItem.myUnitEquipment != UnitManager.player.UnitEquipment)
            {
                EquipSlot equipSlot = slot.GetItemData().Item.Equipment.EquipSlot;
                if (UnitEquipment.IsHeldItemEquipSlot(equipSlot))
                {
                    if (UnitManager.player.UnitEquipment.currentWeaponSet == WeaponSet.One)
                    {
                        if (UnitManager.player.UnitEquipment.EquipSlotHasItem(EquipSlot.LeftHeldItem1))
                            GetTooltip().ShowInventoryTooltip(UnitManager.player.UnitEquipment.GetEquipmentSlot(EquipSlot.LeftHeldItem1));

                        if (UnitManager.player.UnitEquipment.EquipSlotHasItem(EquipSlot.RightHeldItem1))
                            GetTooltip().ShowInventoryTooltip(UnitManager.player.UnitEquipment.GetEquipmentSlot(EquipSlot.RightHeldItem1));
                    }
                    else
                    {
                        if (UnitManager.player.UnitEquipment.EquipSlotHasItem(EquipSlot.LeftHeldItem2))
                            GetTooltip().ShowInventoryTooltip(UnitManager.player.UnitEquipment.GetEquipmentSlot(EquipSlot.LeftHeldItem2));

                        if (UnitManager.player.UnitEquipment.EquipSlotHasItem(EquipSlot.RightHeldItem2))
                            GetTooltip().ShowInventoryTooltip(UnitManager.player.UnitEquipment.GetEquipmentSlot(EquipSlot.RightHeldItem2));
                    }
                }
                else
                {
                    if (UnitManager.player.UnitEquipment.EquipSlotHasItem(equipSlot))
                        GetTooltip().ShowInventoryTooltip(UnitManager.player.UnitEquipment.GetEquipmentSlot(equipSlot));
                }
            }
        }

        public static void ShowActionBarTooltip(ActionBarSlot actionBarSlot)
        {
            if (actionBarSlot == null)
                return;

            GetTooltip().ShowActionTooltip(actionBarSlot);
        }

        public static void ShowLooseItemTooltip(Transform looseItemTransform, ItemData looseItemData)
        {
            if (looseItemTransform == null || looseItemData == null || looseItemData.Item == null)
                return;

            ClearTooltips();
            GetTooltip().ShowLooseItemTooltip(looseItemTransform, looseItemData);
        }

        static Tooltip GetTooltip()
        {
            for (int i = 0; i < Instance.tooltips.Length; i++)
            {
                if (Instance.tooltips[i].gameObject.activeSelf == false)
                    return Instance.tooltips[i];
            }

            Debug.LogWarning("Not enough tooltips...");
            return null;
        }

        public static void SetCurrentSlot(Slot slot) => currentSlot = slot;

        public static void SetCurrentActionBarSlot(ActionBarSlot actionBarSlot) => currentActionBarSlot = actionBarSlot;

        public static Tooltip[] Tooltips => Instance.tooltips;
    }
}
