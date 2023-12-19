using UnitSystem.ActionSystem;
using Controls;
using GridSystem;
using InteractableObjects;
using InventorySystem;
using Pathfinding.Util;
using System.Collections.Generic;
using UnitSystem;
using UnitSystem.ActionSystem.UI;
using UnityEngine;
using UnitSystem.ActionSystem.Actions;

namespace GeneralUI 
{
    public class TooltipManager : MonoBehaviour
    {
        public static TooltipManager Instance;

        [SerializeField] Tooltip tooltipPrefab;
        [SerializeField] int looseItemTooltipsToPool = 5;
        [SerializeField] int unitTooltipsToPool = 3;
        [SerializeField] Tooltip[] inventoryTooltips;

        static List<Tooltip> looseItemTooltips = new();
        static List<Tooltip> unitTooltips = new();

        public static Slot CurrentSlot { get; private set; }
        public static ActionBarSlot CurrentActionBarSlot { get; private set; }
        public static int ActiveInventoryTooltips { get; private set; }

        public static Canvas Canvas { get; private set; }

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

            for (int i = 0; i < looseItemTooltipsToPool; i++)
            {
                CreateNewLooseItemTooltip();
            }

            for (int i = 0; i < unitTooltipsToPool; i++)
            {
                CreateNewUnitTooltip();
            }

            for (int i = 0; i < inventoryTooltips.Length; i++)
            {
                inventoryTooltips[i].Button.interactable = false;
                inventoryTooltips[i].Image.raycastTarget = false;
            }

            Canvas = GetComponentInParent<Canvas>();
        }

        void Update()
        {
            if (GameControls.gamePlayActions.showLooseItemTooltips.WasPressed)
                ShowAllLooseItemTooltips();
            else if (GameControls.gamePlayActions.showLooseItemTooltips.WasReleased)
                ClearLooseItemTooltips();

            if (GameControls.gamePlayActions.showLooseItemTooltips.IsPressed)
            {
                // If the Player moves or rotates, we need to update the tooltips since the visible Loose Items might change
                if (cooldown >= cooldownTime)
                {
                    if (playersLastPosition != UnitManager.player.GridPosition || playersLastDirection != UnitManager.player.UnitActionHandler.TurnAction.currentDirection)
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
            else
                ClearLooseItemTooltips();
        }

        public static void ClearAllTooltips()
        {
            ClearInventoryTooltips();
            ClearLooseItemTooltips();
            ClearUnitTooltips();
        }

        public static void ClearLooseItemTooltips()
        {
            cooldown = 0f;
            for (int i = 0; i < looseItemTooltips.Count; i++)
            {
                looseItemTooltips[i].ClearTooltip();
            }
        }

        public static void ClearUnitTooltips()
        {
            cooldown = 0f;
            for (int i = 0; i < unitTooltips.Count; i++)
            {
                unitTooltips[i].ClearTooltip();
            }
        }

        public static void ClearInventoryTooltips()
        {
            for (int i = 0; i < Instance.inventoryTooltips.Length; i++)
            {
                Instance.inventoryTooltips[i].ClearTooltip();
            }

            CurrentSlot = null;
            CurrentActionBarSlot = null;
            ActiveInventoryTooltips = 0;
        }

        public static void ShowInventoryTooltips(Slot slot)
        {
            GetInventoryTooltip().ShowInventoryTooltip(slot);

            if (slot.GetItemData().Item is Item_Equipment == false)
                return;

            if (slot is EquipmentSlot == false || slot.InventoryItem.MyUnitEquipment != UnitManager.player.UnitEquipment)
            {
                EquipSlot equipSlot = slot.GetItemData().Item.Equipment.EquipSlot;
                if (UnitEquipment.IsHeldItemEquipSlot(equipSlot))
                {
                    if (UnitManager.player.UnitEquipment.CurrentWeaponSet == WeaponSet.One)
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

        public static void ShowLooseItemTooltip(Interactable_LooseItem looseItem, ItemData looseItemData)
        {
            if (looseItem == null || looseItemData == null || looseItemData.Item == null)
                return;

            if (GameControls.gamePlayActions.showLooseItemTooltips.IsPressed)
                return;

            ClearLooseItemTooltips();
            GetLooseItemTooltip().ShowLooseItemTooltip(looseItem, looseItemData, false);
        }

        public static void ShowAllLooseItemTooltips()
        {
            ClearLooseItemTooltips();

            foreach (KeyValuePair<Interactable_LooseItem, int> looseItem in UnitManager.player.Vision.knownLooseItems)
            {
                if (looseItem.Key == null || looseItem.Key.transform == null || looseItem.Key.ItemData == null)
                    continue;

                if (UnitManager.player.Vision.IsVisible(looseItem.Key))
                    GetLooseItemTooltip().ShowLooseItemTooltip(looseItem.Key, looseItem.Key.ItemData, true);
            }
            
            playersLastPosition = UnitManager.player.transform.position;
            playersLastDirection = UnitManager.player.UnitActionHandler.TurnAction.currentDirection;
        }

        public static void ShowUnitHitChanceTooltips(GridPosition targetGridPosition, Action_Base selectedAction)
        {
            ClearUnitTooltips();

            if (selectedAction == null)
                return;

            List<GridPosition> actionAreaGridPositions = ListPool<GridPosition>.Claim();
            actionAreaGridPositions.AddRange(selectedAction.GetActionAreaGridPositions(targetGridPosition));
            for (int i = 0; i < actionAreaGridPositions.Count; i++)
            {
                if (LevelGrid.HasUnitAtGridPosition(actionAreaGridPositions[i], out Unit targetUnit))
                    GetUnitTooltip().ShowUnitHitChanceTooltip(targetUnit, selectedAction);
            }

            ListPool<GridPosition>.Release(actionAreaGridPositions);
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

        static Tooltip GetLooseItemTooltip()
        {
            for (int i = 0; i < looseItemTooltips.Count; i++)
            {
                if (looseItemTooltips[i].gameObject.activeSelf == false)
                    return looseItemTooltips[i];
            }
            return CreateNewLooseItemTooltip();
        }

        static Tooltip GetUnitTooltip()
        {
            for (int i = 0; i < unitTooltips.Count; i++)
            {
                if (unitTooltips[i].gameObject.activeSelf == false)
                    return unitTooltips[i];
            }
            return CreateNewUnitTooltip();
        }

        static Tooltip CreateNewLooseItemTooltip()
        {
            Tooltip tooltip = Instantiate(Instance.tooltipPrefab, Instance.transform);
            looseItemTooltips.Add(tooltip);
            tooltip.gameObject.SetActive(false);
            return tooltip;
        }

        static Tooltip CreateNewUnitTooltip()
        {
            Tooltip tooltip = Instantiate(Instance.tooltipPrefab, Instance.transform);
            unitTooltips.Add(tooltip);
            tooltip.Button.interactable = false;
            tooltip.Image.enabled = false;
            tooltip.gameObject.SetActive(false);
            return tooltip;
        }

        public static void SetCurrentSlot(Slot slot) => CurrentSlot = slot;

        public static void SetCurrentActionBarSlot(ActionBarSlot actionBarSlot) => CurrentActionBarSlot = actionBarSlot;

        public static List<Tooltip> WorldTooltips => looseItemTooltips;

        public static void AddToActiveInventoryTooltips() => ActiveInventoryTooltips++;
    }
}
