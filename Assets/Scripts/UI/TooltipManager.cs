using ActionSystem;
using Controls;
using GridSystem;
using InteractableObjects;
using InventorySystem;
using System.Collections.Generic;
using UnitSystem;
using UnityEngine;

namespace GeneralUI 
{
    public class TooltipManager : MonoBehaviour
    {
        public static TooltipManager Instance;

        [SerializeField] Tooltip tooltipPrefab;
        [SerializeField] int amountToPool = 3;
        [SerializeField] Tooltip[] inventoryTooltips;

        static List<Tooltip> worldTooltips = new List<Tooltip>();

        public static Slot currentSlot { get; private set; }
        public static ActionBarSlot currentActionBarSlot { get; private set; }
        public static int activeInventoryTooltips { get; private set; }

        static Vector3 playersLastPosition;
        static Direction playersLastDirection;

        static float cooldown;
        readonly float cooldownTime = 0.5f;

        void Awake()
        {
            if (Instance != null)
            {
                Debug.LogError("There's more than one TooltipManager! " + transform + " - " + Instance);
                Destroy(gameObject);
                return;
            }
            Instance = this;

            for (int i = 0; i < amountToPool; i++)
            {
                CreateNewWorldTooltip();
            }

            for (int i = 0; i < inventoryTooltips.Length; i++)
            {
                inventoryTooltips[i].Button.interactable = false;
                inventoryTooltips[i].Image.raycastTarget = false;
            }
        }

        void Update()
        {
            if (GameControls.gamePlayActions.showLooseItemTooltips.WasPressed)
                ShowAllLooseItemTooltips();
            else if (GameControls.gamePlayActions.showLooseItemTooltips.WasReleased)
                ClearWorldTooltips();

            if (GameControls.gamePlayActions.showLooseItemTooltips.IsPressed)
            {
                // If the Player moves or rotates, we need to update the tooltips since the visible Loose Items might change
                if (cooldown >= cooldownTime)
                {
                    if (playersLastPosition != UnitManager.player.GridPosition || playersLastDirection != UnitManager.player.unitActionHandler.turnAction.currentDirection)
                        ShowAllLooseItemTooltips();
                }
                else
                    cooldown += Time.deltaTime; // Cooldown is reset whenever world tooltips are cleared
            }
        }

        public static void UpdateLooseItemTooltips()
        {
            if (GameControls.gamePlayActions.showLooseItemTooltips.IsPressed)
                ShowAllLooseItemTooltips();
        }

        public static void ClearTooltips()
        {
            ClearWorldTooltips();
            ClearInventoryTooltips();
        }

        public static void ClearWorldTooltips()
        {
            cooldown = 0f;
            for (int i = 0; i < worldTooltips.Count; i++)
            {
                worldTooltips[i].ClearTooltip();
            }
        }
        
        public static void ClearInventoryTooltips()
        {
            for (int i = 0; i < Instance.inventoryTooltips.Length; i++)
            {
                Instance.inventoryTooltips[i].ClearTooltip();
            }

            currentSlot = null;
            currentActionBarSlot = null;
            activeInventoryTooltips = 0;
        }

        public static void ShowInventoryTooltips(Slot slot)
        {
            GetInventoryTooltip().ShowInventoryTooltip(slot);

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
                            GetInventoryTooltip().ShowInventoryTooltip(UnitManager.player.UnitEquipment.GetEquipmentSlot(EquipSlot.LeftHeldItem1));

                        if (UnitManager.player.UnitEquipment.EquipSlotHasItem(EquipSlot.RightHeldItem1))
                            GetInventoryTooltip().ShowInventoryTooltip(UnitManager.player.UnitEquipment.GetEquipmentSlot(EquipSlot.RightHeldItem1));
                    }
                    else
                    {
                        if (UnitManager.player.UnitEquipment.EquipSlotHasItem(EquipSlot.LeftHeldItem2))
                            GetInventoryTooltip().ShowInventoryTooltip(UnitManager.player.UnitEquipment.GetEquipmentSlot(EquipSlot.LeftHeldItem2));

                        if (UnitManager.player.UnitEquipment.EquipSlotHasItem(EquipSlot.RightHeldItem2))
                            GetInventoryTooltip().ShowInventoryTooltip(UnitManager.player.UnitEquipment.GetEquipmentSlot(EquipSlot.RightHeldItem2));
                    }
                }
                else
                {
                    if (UnitManager.player.UnitEquipment.EquipSlotHasItem(equipSlot))
                        GetInventoryTooltip().ShowInventoryTooltip(UnitManager.player.UnitEquipment.GetEquipmentSlot(equipSlot));
                }
            }
        }

        public static void ShowActionBarTooltip(ActionBarSlot actionBarSlot)
        {
            if (actionBarSlot == null)
                return;

            GetInventoryTooltip().ShowActionTooltip(actionBarSlot);
        }

        public static void ShowLooseItemTooltip(LooseItem looseItem, ItemData looseItemData)
        {
            if (looseItem == null || looseItemData == null || looseItemData.Item == null)
                return;

            if (GameControls.gamePlayActions.showLooseItemTooltips.IsPressed)
                return;

            ClearWorldTooltips();
            GetTooltip().ShowLooseItemTooltip(looseItem, looseItemData, false);
        }

        public static void ShowAllLooseItemTooltips()
        {
            ClearWorldTooltips();

            foreach (KeyValuePair<LooseItem, int> looseItem in UnitManager.player.vision.knownLooseItems)
            {
                if (looseItem.Key == null || looseItem.Key.transform == null || looseItem.Key.ItemData == null)
                    continue;

                if (UnitManager.player.vision.IsVisible(looseItem.Key))
                    GetTooltip().ShowLooseItemTooltip(looseItem.Key, looseItem.Key.ItemData, true);
            }
            
            playersLastPosition = UnitManager.player.transform.position;
            playersLastDirection = UnitManager.player.unitActionHandler.turnAction.currentDirection;
        }

        static Tooltip GetInventoryTooltip()
        {
            for (int i = 0; i < Instance.inventoryTooltips.Length; i++)
            {
                if (Instance.inventoryTooltips[i].gameObject.activeSelf == false)
                    return Instance.inventoryTooltips[i];
            }
            return Instance.inventoryTooltips[Instance.inventoryTooltips.Length - 1];
        }

        static Tooltip GetTooltip()
        {
            for (int i = 0; i < worldTooltips.Count; i++)
            {
                if (worldTooltips[i].gameObject.activeSelf == false)
                    return worldTooltips[i];
            }
            return CreateNewWorldTooltip();
        }

        static Tooltip CreateNewWorldTooltip()
        {
            Tooltip tooltip = Instantiate(Instance.tooltipPrefab, Instance.transform);
            worldTooltips.Add(tooltip);
            tooltip.gameObject.SetActive(false);
            return tooltip;
        }

        public static void SetCurrentSlot(Slot slot) => currentSlot = slot;

        public static void SetCurrentActionBarSlot(ActionBarSlot actionBarSlot) => currentActionBarSlot = actionBarSlot;

        public static List<Tooltip> WorldTooltips => worldTooltips;

        public static void AddToActiveInventoryTooltips() => activeInventoryTooltips++;
    }
}
