using UnityEngine;
using UnityEngine.EventSystems;
using System.Collections;
using System.Collections.Generic;
using InteractableObjects;
using GridSystem;
using ActionSystem;
using InventorySystem;
using UnitSystem;
using Controls;
using Utilities;

namespace GeneralUI
{
    public class ContextMenu : MonoBehaviour
    {
        public static ContextMenu Instance { get; private set; }

        [SerializeField] GameObject contextMenuButtonPrefab;

        static List<ContextMenuButton> contextButtons = new List<ContextMenuButton>();
        static RectTransform rectTransform;

        public static Unit targetUnit { get; private set; }
        public static Slot targetSlot { get; private set; }
        public static Interactable targetInteractable { get; private set; }

        static WaitForSeconds buildContextMenuCooldown = new WaitForSeconds(0.2f);
        static WaitForSeconds disableContextMenuDelay = new WaitForSeconds(0.1f);
        public static bool isActive { get; private set; }
        static bool onCooldown;

        static float contextMenuHoldTimer;
        static readonly float maxContextMenuHoldTime = 0.125f;
        static readonly float minButtonWidth = 100;

        void Awake()
        {
            if (Instance != null)
            {
                Debug.LogError("There's more than one ContextMenu! " + transform + " - " + Instance);
                Destroy(gameObject);
                return;
            }
            Instance = this;

            rectTransform = GetComponent<RectTransform>();
        }

        void Update()
        {
            if (isActive && GameControls.gamePlayActions.menuQuickUse.WasPressed || ((PlayerInput.Instance.highlightedInteractable == null || PlayerInput.Instance.highlightedInteractable != targetInteractable) && (InventoryUI.activeSlot == null || (InventoryUI.activeSlot.ParentSlot() != null && InventoryUI.activeSlot.ParentSlot().IsFull() == false))
                && (GameControls.gamePlayActions.menuSelect.WasPressed || GameControls.gamePlayActions.menuContext.WasPressed)))
            {
                StartCoroutine(DelayDisableContextMenu());
            }

            if (contextMenuHoldTimer < maxContextMenuHoldTime && GameControls.gamePlayActions.menuContext.IsPressed)
                contextMenuHoldTimer += Time.deltaTime;

            if (GameControls.gamePlayActions.menuContext.WasReleased)
            {
                // Don't allow context menu actions while an action is already queued
                if (UnitManager.player.unitActionHandler.queuedActions.Count > 0)
                    return;

                if (contextMenuHoldTimer < maxContextMenuHoldTime)
                    BuildContextMenu();

                contextMenuHoldTimer = 0;
            }
        }

        public static void BuildContextMenu()
        {
            if (onCooldown)
                return;

            SplitStack.Instance.Close();

            int activeCount = 0;
            for (int i = 0; i < contextButtons.Count; i++)
            {
                if (contextButtons[i].gameObject.activeSelf)
                    activeCount++;
            }

            DisableContextMenu(true);

            targetUnit = PlayerInput.Instance.highlightedUnit;
            targetInteractable = PlayerInput.Instance.highlightedInteractable;
            targetSlot = null;
            if (InventoryUI.activeSlot != null)
            {
                targetSlot = InventoryUI.activeSlot.ParentSlot();
                if (targetSlot is EquipmentSlot)
                {
                    EquipmentSlot equipmentSlot = targetSlot as EquipmentSlot;
                    if (UnitEquipment.IsHeldItemEquipSlot(equipmentSlot.EquipSlot) && equipmentSlot.IsFull() && equipmentSlot.UnitEquipment.EquipSlotHasItem(equipmentSlot.EquipSlot) == false)
                        targetSlot = equipmentSlot.UnitEquipment.GetEquipmentSlot(equipmentSlot.UnitEquipment.GetOppositeWeaponEquipSlot(equipmentSlot.EquipSlot));
                }
            }

            if (targetInteractable != null && TacticsPathfindingUtilities.CalculateWorldSpaceDistance_XYZ(targetInteractable.GridPosition(), UnitManager.player.GridPosition) > LevelGrid.diaganolDistance)
            {
                CreateMoveToButton();
            }
            else
            {
                // Create the necessary buttons
                CreateAttackButton();
                CreateTakeItemButton();
                CreateAddToBagButtons();
                CreateOpenContainerButton();
                CreateUseItemButtons();
                CreateSplitStackButton();
                CreateDropItemButton();

                if (EventSystem.current.IsPointerOverGameObject() == false && ((targetInteractable == null && targetSlot == null && activeCount != 1) || (targetInteractable != null && TacticsPathfindingUtilities.CalculateWorldSpaceDistance_XYZ(targetInteractable.GridPosition(), UnitManager.player.GridPosition) > LevelGrid.diaganolDistance)))
                    CreateMoveToButton();
            }

            int buttonCount = 0;
            for (int i = 0; i < contextButtons.Count; i++)
            {
                if (contextButtons[i].gameObject.activeSelf)
                {
                    buttonCount++;
                    break;
                }
            }

            if (buttonCount > 0)
            {
                isActive = true;
                Instance.StartCoroutine(BuildContextMenuCooldown());

                SetupMenuSize();
                SetupMenuPosition();
            }
        }

        static void CreateMoveToButton()
        {
            GridPosition targetGridPosition;
            if (targetInteractable != null)
                targetGridPosition = LevelGrid.Instance.GetNearestSurroundingGridPosition(targetInteractable.GridPosition(), UnitManager.player.GridPosition, LevelGrid.diaganolDistance, targetInteractable is LooseItem);
            else
                targetGridPosition = WorldMouse.GetCurrentGridPosition();

            if (LevelGrid.IsValidGridPosition(targetGridPosition) == false)
                return;

            GetContextMenuButton().SetupMoveToButton(targetGridPosition);
        }

        static void CreateAttackButton()
        {
            if (targetUnit == null || targetUnit.health.IsDead() || UnitManager.player.vision.IsVisible(targetUnit) == false
                || (UnitManager.player.UnitEquipment.MeleeWeaponEquipped() == false && (UnitManager.player.UnitEquipment.RangedWeaponEquipped() == false || UnitManager.player.UnitEquipment.HasValidAmmunitionEquipped() == false) && UnitManager.player.stats.CanFightUnarmed == false))
                return;

            BaseAction selectedAction = UnitManager.player.SelectedAction;
            if ((selectedAction is MoveAction == false && selectedAction.IsDefaultAttackAction() == false) || (targetUnit.IsCompletelySurrounded(UnitManager.player.GetAttackRange(targetUnit, false)) && UnitManager.player.GetAttackRange(targetUnit, false) < 2f))
                return;

            GetContextMenuButton().SetupAttackButton();
        }

        static void CreateAddToBagButtons()
        {
            if ((targetSlot == null || targetSlot.IsFull() == false) && (targetInteractable == null || targetInteractable is LooseItem == false))
                return;

            ItemData itemData = null;
            if (targetSlot != null)
            {
                if (targetSlot is EquipmentSlot)
                    return;

                itemData = targetSlot.GetItemData();
            }
            else if (targetInteractable != null && targetInteractable is LooseItem)
            {
                LooseItem targetLooseItem = targetInteractable as LooseItem;
                if (targetLooseItem is LooseContainerItem)
                {
                    LooseContainerItem looseContainerItem = targetLooseItem as LooseContainerItem;
                    if (looseContainerItem.ContainerInventoryManager.ContainsAnyItems())
                        return;
                }

                itemData = targetLooseItem.ItemData;
            }

            if (itemData == null || itemData.Item == null)
                return;

            if (itemData.MyInventory() != null && itemData.MyInventory() is ContainerInventory)
            {
                ContainerInventory containerInventory = itemData.MyInventory() as ContainerInventory;
                if (containerInventory.containerInventoryManager != UnitManager.player.BackpackInventoryManager && UnitManager.player.BackpackInventoryManager != null
                    && UnitManager.player.UnitEquipment.EquipSlotHasItem(EquipSlot.Back) && UnitManager.player.UnitEquipment.EquippedItemDatas[(int)EquipSlot.Back].Item is Backpack)
                {
                    GetContextMenuButton().SetupAddToBackpackButton(itemData);
                }
            }
            else
            {
                if (UnitManager.player.BackpackInventoryManager != null && UnitManager.player.UnitEquipment.EquipSlotHasItem(EquipSlot.Back) && UnitManager.player.UnitEquipment.EquippedItemDatas[(int)EquipSlot.Back].Item is Backpack)
                    GetContextMenuButton().SetupAddToBackpackButton(itemData);
            }
        }

        static void CreateTakeItemButton()
        {
            if ((targetSlot == null || targetSlot.IsFull() == false) && (targetInteractable == null || targetInteractable is LooseItem == false))
                return;

            ItemData itemData = null;
            if (targetSlot != null)
            {
                if (targetSlot is EquipmentSlot)
                {
                    EquipmentSlot targetEquipmentSlot = targetSlot as EquipmentSlot;
                    if (targetEquipmentSlot.UnitEquipment == UnitManager.player.UnitEquipment)
                        return;
                }
                else if (targetSlot is InventorySlot)
                {
                    InventorySlot targetInventorySlot = targetSlot as InventorySlot;
                    if (targetInventorySlot.myInventory == UnitManager.player.UnitInventoryManager.MainInventory)
                        return;
                }

                itemData = targetSlot.GetItemData();
            }
            else if (targetInteractable != null && targetInteractable is LooseItem)
            {
                LooseItem targetLooseItem = targetInteractable as LooseItem;
                if (targetLooseItem is LooseContainerItem)
                {
                    LooseContainerItem looseContainerItem = targetLooseItem as LooseContainerItem;
                    if (looseContainerItem.ContainerInventoryManager.ContainsAnyItems())
                        return;
                }

                itemData = targetLooseItem.ItemData;
            }

            if (itemData == null || itemData.Item == null)
                return;

            GetContextMenuButton().SetupTakeItemButton(itemData);
        }

        static void CreateOpenContainerButton()
        {
            if (targetSlot != null && targetSlot is ContainerEquipmentSlot && targetSlot.IsFull() && (targetSlot.GetItemData().Item is Backpack || targetSlot.GetItemData().Item is Quiver))
            {
                ContainerEquipmentSlot containerEquipmentSlot = targetSlot as ContainerEquipmentSlot;
                if (containerEquipmentSlot.containerInventoryManager == null)
                {
                    Debug.LogWarning($"{containerEquipmentSlot.name} does not have an assigned ContainerInventoryManager...");
                    return;
                }

                if (containerEquipmentSlot.containerInventoryManager.ParentInventory.slotVisualsCreated)
                {
                    CreateCloseContainerButton();
                    return;
                }
            }
            else if (targetInteractable != null && targetInteractable is LooseContainerItem)
            {
                LooseContainerItem looseContainerItem = targetInteractable as LooseContainerItem;
                if (looseContainerItem.ContainerInventoryManager.ParentInventory.slotVisualsCreated)
                {
                    CreateCloseContainerButton();
                    return;
                }
            }
            else if (targetUnit != null && targetUnit.health.IsDead())
            {
                if (targetUnit.UnitEquipment.slotVisualsCreated)
                {
                    CreateCloseContainerButton();
                    return;
                }
            }
            else
                return;

            GetContextMenuButton().SetupOpenContainerButton();
        }

        static void CreateCloseContainerButton() => GetContextMenuButton().SetupCloseContainerButton();

        static void CreateUseItemButtons()
        {
            if ((targetSlot == null || targetSlot.IsFull() == false) && (targetInteractable == null || targetInteractable is LooseItem == false))
                return;

            ItemData itemData = null;
            if (targetSlot != null)
                itemData = targetSlot.GetItemData();
            else if (targetInteractable != null)
            {
                LooseItem looseItem = targetInteractable as LooseItem;
                itemData = looseItem.ItemData;

                if (itemData.Item is Equipment == false)
                    return;
            }

            if (itemData == null || itemData.Item == null || itemData.Item.IsUsable == false)
                return;

            if (itemData.Item is Ammunition && UnitManager.player.QuiverInventoryManager.Contains(itemData))
                return;

            if (itemData.Item.MaxUses > 1 && itemData.RemainingUses > 1)
            {
                GetContextMenuButton().SetupUseItemButton(itemData, itemData.RemainingUses); // Use all

                if (itemData.RemainingUses >= 4 && Mathf.CeilToInt(itemData.RemainingUses * 0.75f) > 1)
                    GetContextMenuButton().SetupUseItemButton(itemData, Mathf.CeilToInt(itemData.RemainingUses * 0.75f)); // Use 3/4

                if (itemData.RemainingUses >= 4 && Mathf.CeilToInt(itemData.RemainingUses * 0.5f) > 1)
                    GetContextMenuButton().SetupUseItemButton(itemData, Mathf.CeilToInt(itemData.RemainingUses * 0.5f)); // Use 1/2

                if (itemData.RemainingUses >= 4 && Mathf.CeilToInt(itemData.RemainingUses * 0.25f) > 1)
                    GetContextMenuButton().SetupUseItemButton(itemData, Mathf.CeilToInt(itemData.RemainingUses * 0.25f)); // Use 1/4

                if (itemData.RemainingUses >= 10 && Mathf.CeilToInt(itemData.RemainingUses * 0.1f) > 1)
                    GetContextMenuButton().SetupUseItemButton(itemData, Mathf.CeilToInt(itemData.RemainingUses * 0.1f)); // Use 1/10

                GetContextMenuButton().SetupUseItemButton(itemData, 1); // Use 1
            }
            else if (itemData.Item.MaxStackSize > 1 && itemData.CurrentStackSize > 1 && itemData.Item is Ammunition == false)
            {
                GetContextMenuButton().SetupUseItemButton(itemData, itemData.CurrentStackSize); // Use all

                if (itemData.CurrentStackSize >= 4 && Mathf.CeilToInt(itemData.CurrentStackSize * 0.75f) > 1)
                    GetContextMenuButton().SetupUseItemButton(itemData, Mathf.CeilToInt(itemData.CurrentStackSize * 0.75f)); // Use 3/4

                if (itemData.CurrentStackSize >= 4 && Mathf.CeilToInt(itemData.CurrentStackSize * 0.5f) > 1)
                    GetContextMenuButton().SetupUseItemButton(itemData, Mathf.CeilToInt(itemData.CurrentStackSize * 0.5f)); // Use 1/2

                if (itemData.CurrentStackSize >= 4 && Mathf.CeilToInt(itemData.CurrentStackSize * 0.25f) > 1)
                    GetContextMenuButton().SetupUseItemButton(itemData, Mathf.CeilToInt(itemData.CurrentStackSize * 0.25f)); // Use 1/4

                if (itemData.CurrentStackSize >= 10 && Mathf.CeilToInt(itemData.CurrentStackSize * 0.1f) > 1)
                    GetContextMenuButton().SetupUseItemButton(itemData, Mathf.CeilToInt(itemData.CurrentStackSize * 0.1f)); // Use 1/10

                GetContextMenuButton().SetupUseItemButton(itemData, 1); // Use 1
            }
            else
                GetContextMenuButton().SetupUseItemButton(itemData, 1); // There's only 1 left or can only have 1 use/stack size
        }

        static void CreateSplitStackButton()
        {
            if (targetSlot == null || targetSlot.IsFull() == false)
                return;

            if (targetSlot.GetItemData().Item.MaxStackSize <= 1 || targetSlot.GetItemData().CurrentStackSize <= 1)
                return;

            GetContextMenuButton().SetupSplitStackButton(targetSlot.GetItemData());
        }

        static void CreateDropItemButton()
        {
            if (targetSlot == null || targetSlot.IsFull() == false)
                return;

            if (targetSlot != null && targetSlot is ContainerEquipmentSlot)
            {
                ContainerEquipmentSlot containerSlot = targetSlot as ContainerEquipmentSlot;
                if (containerSlot.containerInventoryManager.ContainsAnyItems())
                    return;
            }

            GetContextMenuButton().SetupDropItemButton();
        }

        static IEnumerator BuildContextMenuCooldown()
        {
            if (onCooldown == false)
            {
                onCooldown = true;
                yield return buildContextMenuCooldown;
                onCooldown = false;
            }
        }

        public static void DisableContextMenu(bool forceDisable = false)
        {
            if (isActive == false)
                return;

            if (forceDisable == false && onCooldown)
                return;

            isActive = false;

            for (int i = 0; i < contextButtons.Count; i++)
            {
                contextButtons[i].Disable();
            }
        }

        static IEnumerator DelayDisableContextMenu(bool forceDisable = false)
        {
            yield return disableContextMenuDelay;
            DisableContextMenu();
        }

        static ContextMenuButton GetContextMenuButton()
        {
            for (int i = 0; i < contextButtons.Count; i++)
            {
                if (contextButtons[i].gameObject.activeSelf == false)
                    return contextButtons[i];
            }

            return CreateNewContextMenuButton();
        }

        static ContextMenuButton CreateNewContextMenuButton()
        {
            ContextMenuButton contextButton = Instantiate(Instance.contextMenuButtonPrefab, Instance.transform).GetComponent<ContextMenuButton>();
            contextButtons.Add(contextButton);
            contextButton.gameObject.SetActive(false);
            return contextButton;
        }

        static void SetupMenuSize()
        {
            float width = minButtonWidth;
            float heigth = 0f;

            for (int i = 0; i < contextButtons.Count; i++)
            {
                if (contextButtons[i].gameObject.activeSelf == false)
                    continue;

                // Get the width of the button with the largest amount of text
                contextButtons[i].ButtonText.ForceMeshUpdate();
                if (width < contextButtons[i].ButtonText.textBounds.size.x)
                    width = contextButtons[i].ButtonText.textBounds.size.x;

                heigth += contextButtons[i].RectTransform.sizeDelta.y;
            }

            if (width > minButtonWidth)
                width += 40;

            rectTransform.sizeDelta = new Vector2(width, heigth);
        }

        static void SetupMenuPosition()
        {
            int activeButtonCount = 0;
            for (int i = 0; i < contextButtons.Count; i++)
            {
                if (contextButtons[i].gameObject.activeSelf)
                    activeButtonCount++;
            }

            float xPosAddon = rectTransform.sizeDelta.x / 2f;
            float yPosAddon = (activeButtonCount * contextButtons[0].RectTransform.sizeDelta.y) / 2f;

            // Get the desired position:
            // If the mouse position is too close to the top of the screen
            if (Input.mousePosition.y >= (Screen.height - (activeButtonCount * contextButtons[0].RectTransform.sizeDelta.y * 1.2f)))
                yPosAddon = (-activeButtonCount * contextButtons[0].RectTransform.sizeDelta.y) / 2f;

            // If the mouse position is too far to the right of the screen
            if (Input.mousePosition.x >= (Screen.width - (rectTransform.sizeDelta.x * 1.2f)))
                xPosAddon = -rectTransform.sizeDelta.x / 2f;

            Instance.transform.position = Input.mousePosition + new Vector3(xPosAddon, yPosAddon);
        }
    }
}
