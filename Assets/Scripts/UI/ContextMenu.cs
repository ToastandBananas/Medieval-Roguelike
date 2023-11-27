using UnityEngine;
using UnityEngine.EventSystems;
using System.Collections;
using System.Collections.Generic;
using InteractableObjects;
using GridSystem;
using UnitSystem.ActionSystem;
using UnitSystem.ActionSystem.UI;
using InventorySystem;
using UnitSystem;
using Controls;
using Pathfinding.Util;

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
            if (isActive && GameControls.gamePlayActions.menuQuickUse.WasPressed || ((PlayerInput.Instance.HighlightedInteractable == null || PlayerInput.Instance.HighlightedInteractable != targetInteractable) && (InventoryUI.activeSlot == null || (InventoryUI.activeSlot.ParentSlot() != null && InventoryUI.activeSlot.ParentSlot().IsFull() == false))
                && (GameControls.gamePlayActions.menuSelect.WasPressed || GameControls.gamePlayActions.menuContext.WasPressed)))
            {
                StartCoroutine(DelayDisableContextMenu());
            }

            if (contextMenuHoldTimer < maxContextMenuHoldTime && GameControls.gamePlayActions.menuContext.IsPressed)
                contextMenuHoldTimer += Time.deltaTime;

            // Don't allow context menu actions while an action is already queued or when dragging items
            if (UnitManager.player.unitActionHandler.queuedActions.Count > 0 || InventoryUI.isDraggingItem || ActionSystemUI.isDraggingAction)
                return;

            if (GameControls.gamePlayActions.menuContext.WasReleased && UnitManager.player.unitActionHandler.PlayerActionHandler.DefaultActionIsSelected)
            {
                if (contextMenuHoldTimer < maxContextMenuHoldTime)
                    BuildContextMenu();

                contextMenuHoldTimer = 0;
            }
        }

        public static void BuildContextMenu()
        {
            if (onCooldown || InventoryUI.isDraggingItem)
                return;

            SplitStack.Instance.Close();

            int activeCount = 0;
            for (int i = 0; i < contextButtons.Count; i++)
            {
                if (contextButtons[i].gameObject.activeSelf)
                    activeCount++;
            }

            DisableContextMenu(true);

            targetSlot = null;
            targetInteractable = PlayerInput.Instance.HighlightedInteractable;
            targetUnit = PlayerInput.Instance.HighlightedUnit;
            if (targetUnit != null && targetUnit.health.IsDead)
                targetUnit.unitInteractable.UpdateGridPosition();

            if (InventoryUI.activeSlot != null)
            {
                targetSlot = InventoryUI.activeSlot.ParentSlot();
                targetUnit = null;
                targetInteractable = null;

                if (targetSlot is EquipmentSlot)
                {
                    EquipmentSlot equipmentSlot = targetSlot as EquipmentSlot;
                    if (UnitEquipment.IsHeldItemEquipSlot(equipmentSlot.EquipSlot) && equipmentSlot.IsFull() && equipmentSlot.UnitEquipment.EquipSlotHasItem(equipmentSlot.EquipSlot) == false)
                        targetSlot = equipmentSlot.UnitEquipment.GetEquipmentSlot(equipmentSlot.UnitEquipment.GetOppositeWeaponEquipSlot(equipmentSlot.EquipSlot));
                }
            }

            if (targetInteractable != null && Vector3.Distance(targetInteractable.GridPosition().WorldPosition, UnitManager.player.WorldPosition) > LevelGrid.diaganolDistance)
            {
                targetUnit = null;
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
                CreateAddItemToHotbarButton();
                CreateRemoveFromHotbarButton();
                CreateDropItemButton();

                if (EventSystem.current.IsPointerOverGameObject() == false && ((targetInteractable == null && targetUnit == null && targetSlot == null && activeCount != 1) 
                    || (targetInteractable != null && Vector3.Distance(targetInteractable.GridPosition().WorldPosition, UnitManager.player.WorldPosition) > LevelGrid.diaganolDistance)
                    || (targetUnit != null && Vector3.Distance(targetUnit.WorldPosition, UnitManager.player.WorldPosition) > LevelGrid.diaganolDistance)))
                {
                    CreateMoveToButton();
                }
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
                StartContextMenuCooldown();

                SetupMenuSize();
                SetupMenuPosition();
            }
        }

        public static void BuildReloadContextMenu()
        {
            if (onCooldown)
                return;

            SplitStack.Instance.Close();
            DisableContextMenu(true); 
            
            targetSlot = null;
            targetInteractable = null;
            targetUnit = null;

            CreateReloadButtons(out int buttonCount);

            if (buttonCount > 0)
            {
                isActive = true;
                StartContextMenuCooldown();

                SetupMenuSize();
                SetupMenuPosition();
            }
        }

        static void CreateReloadButtons(out int buttonCount)
        {
            buttonCount = 0;
            if (UnitManager.player.UnitEquipment.HasValidAmmunitionEquipped() == false || UnitManager.player.UnitEquipment.QuiverEquipped() == false)
                return;

            List<ItemData> uniqueProjectileTypes = ListPool<ItemData>.Claim();
            for (int i = 0; i < UnitManager.player.QuiverInventoryManager.ParentInventory.ItemDatas.Count; i++)
            {
                if (i == 0)
                    uniqueProjectileTypes.Add(UnitManager.player.QuiverInventoryManager.ParentInventory.ItemDatas[i]);
                else
                {
                    bool isEqual = false;
                    for (int j = 0; j < uniqueProjectileTypes.Count; j++)
                    {
                        if (uniqueProjectileTypes[j].IsEqual(UnitManager.player.QuiverInventoryManager.ParentInventory.ItemDatas[i]))
                        {
                            isEqual = true;
                            break;
                        }
                    }

                    if (isEqual == false)
                        uniqueProjectileTypes.Add(UnitManager.player.QuiverInventoryManager.ParentInventory.ItemDatas[i]);
                }
            }

            for (int i = 0; i < uniqueProjectileTypes.Count; i++)
            {
                GetContextMenuButton().SetupReloadButton(uniqueProjectileTypes[i]);
                buttonCount++;
            }

            ListPool<ItemData>.Release(uniqueProjectileTypes);
        }

        public static void BuildThrowWeaponContextMenu()
        {
            if (onCooldown)
                return;

            SplitStack.Instance.Close();
            DisableContextMenu(true);

            targetSlot = null;
            targetInteractable = null;
            targetUnit = null;

            CreateThrowWeaponButtons(out int buttonCount);

            if (buttonCount > 0)
            {
                isActive = true;
                StartContextMenuCooldown();

                SetupMenuSize();
                SetupMenuPosition();
            }
        }

        static void CreateThrowWeaponButtons(out int buttonCount)
        {
            buttonCount = 0;

            HeldMeleeWeapon leftMeleeWeapon = UnitManager.player.unitMeshManager.GetLeftHeldMeleeWeapon();
            if (leftMeleeWeapon != null)
            {
                GetContextMenuButton().SetupThrowWeaponButton(leftMeleeWeapon.ItemData);
                buttonCount++;
            }

            HeldMeleeWeapon rightMeleeWeapon = UnitManager.player.unitMeshManager.GetRightHeldMeleeWeapon();
            if (rightMeleeWeapon != null)
            {
                GetContextMenuButton().SetupThrowWeaponButton(rightMeleeWeapon.ItemData);
                buttonCount++;
            }
        }

        static void CreateMoveToButton()
        {
            GridPosition targetGridPosition;
            if (targetInteractable != null)
                targetGridPosition = LevelGrid.GetNearestSurroundingGridPosition(targetInteractable.GridPosition(), UnitManager.player.GridPosition, LevelGrid.diaganolDistance, targetInteractable is LooseItem);
            else
                targetGridPosition = WorldMouse.CurrentGridPosition();

            if (LevelGrid.IsValidGridPosition(targetGridPosition) == false)
                return;

            GetContextMenuButton().SetupMoveToButton(targetGridPosition);
        }

        static void CreateAttackButton()
        {
            if (targetUnit == null || targetUnit.health.IsDead || UnitManager.player.vision.IsVisible(targetUnit) == false
                || (UnitManager.player.UnitEquipment.MeleeWeaponEquipped == false && (UnitManager.player.UnitEquipment.RangedWeaponEquipped == false || UnitManager.player.UnitEquipment.HasValidAmmunitionEquipped() == false) && UnitManager.player.stats.CanFightUnarmed == false))
                return;

            BaseAction selectedAction = UnitManager.player.SelectedAction;
            if ((selectedAction is MoveAction == false && selectedAction.IsDefaultAttackAction == false) || (targetUnit.IsCompletelySurrounded(UnitManager.player.GetAttackRange()) && UnitManager.player.GetAttackRange() < 2f))
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
                    if (looseContainerItem.ContainerInventoryManager.ContainsAnyItems()) // We can't put a container inside an inventory if it has items inside of it
                        return;
                }

                itemData = targetLooseItem.ItemData;
            }

            if (itemData == null || itemData.Item == null)
                return;

            if (itemData.MyInventory != null && itemData.MyInventory is ContainerInventory)
            {
                // Skip if this is the backpack itself
                ContainerInventory containerInventory = itemData.MyInventory as ContainerInventory;
                if (containerInventory.containerInventoryManager != UnitManager.player.BackpackInventoryManager && UnitManager.player.BackpackInventoryManager != null && UnitManager.player.UnitEquipment.BackpackEquipped())
                    GetContextMenuButton().SetupAddToBackpackButton(itemData);

                if (containerInventory.containerInventoryManager != UnitManager.player.BeltInventoryManager && UnitManager.player.BeltInventoryManager != null && UnitManager.player.UnitEquipment.BeltBagEquipped())
                    GetContextMenuButton().SetupAddToBeltBagButton(itemData);
            }
            else
            {
                if (UnitManager.player.BackpackInventoryManager != null && UnitManager.player.UnitEquipment.BackpackEquipped())
                    GetContextMenuButton().SetupAddToBackpackButton(itemData);

                if (UnitManager.player.BeltInventoryManager != null && UnitManager.player.UnitEquipment.BeltBagEquipped())
                    GetContextMenuButton().SetupAddToBeltBagButton(itemData);
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
                    if (looseContainerItem.ContainerInventoryManager.ContainsAnyItems()) // We can't put containers with items inside of them into an inventory
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
            if (targetSlot != null && targetSlot is ContainerEquipmentSlot && targetSlot.IsFull() && targetSlot.GetItemData().Item is WearableContainer)
            {
                ContainerEquipmentSlot containerEquipmentSlot = targetSlot as ContainerEquipmentSlot;
                if (containerEquipmentSlot.containerInventoryManager == null)
                {
                    Debug.LogWarning($"{containerEquipmentSlot.name} does not have an assigned ContainerInventoryManager...");
                    return;
                }

                if (containerEquipmentSlot.GetItemData().Item.WearableContainer.HasAnInventory() == false)
                    return;

                if (containerEquipmentSlot.containerInventoryManager.ParentInventory.slotVisualsCreated)
                {
                    CreateCloseContainerButton();
                    return;
                }
            }
            else if (targetInteractable != null && targetInteractable is LooseContainerItem)
            {
                LooseContainerItem looseContainerItem = targetInteractable as LooseContainerItem;
                if (looseContainerItem.ItemData.Item is WearableContainer && looseContainerItem.ItemData.Item.WearableContainer.HasAnInventory() == false) // Some belts, for example, won't have an inventory, so don't create this button
                    return;

                if (Vector3.Distance(targetInteractable.GridPosition().WorldPosition, UnitManager.player.WorldPosition) > LevelGrid.diaganolDistance)
                    return;

                if (looseContainerItem.ContainerInventoryManager.ParentInventory.slotVisualsCreated)
                {
                    CreateCloseContainerButton();
                    return;
                }
            }
            else if (targetUnit != null && targetUnit.health.IsDead)
            {
                if (Vector3.Distance(LevelGrid.GetGridPosition(targetUnit.transform.position).WorldPosition, UnitManager.player.WorldPosition) > LevelGrid.diaganolDistance)
                    return;

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

            if (targetSlot is ContainerEquipmentSlot)
            {
                ContainerEquipmentSlot containerSlot = targetSlot as ContainerEquipmentSlot;
                if (containerSlot.InventoryItem.myUnitEquipment.MyUnit == UnitManager.player && containerSlot.containerInventoryManager.ContainsAnyItems())
                    return;
            }

            GetContextMenuButton().SetupDropItemButton();
        }

        static void CreateAddItemToHotbarButton()
        {
            if (targetSlot == null || targetSlot is EquipmentSlot || targetSlot.IsFull() == false || targetSlot.InventoryItem.GetMyUnit() != UnitManager.player)
                return;
            
            ItemActionBarSlot itemActionSlot = ActionSystemUI.GetNextAvailableItemActionBarSlot();
            if (itemActionSlot == null || ActionSystemUI.ItemActionBarAlreadyHasItem(targetSlot.GetItemData()))
                return;
            
            GetContextMenuButton().SetupAddItemToHotbarButton(itemActionSlot);
        }

        static void CreateRemoveFromHotbarButton()
        {
            // This only works for ItemActionBar slots with items in them
            if (ActionSystemUI.highlightedActionSlot == null || ActionSystemUI.highlightedActionSlot is ItemActionBarSlot == false || ActionSystemUI.highlightedActionSlot.ItemActionBarSlot.itemData == null || ActionSystemUI.highlightedActionSlot.ItemActionBarSlot.itemData.Item == null)
                return;

            GetContextMenuButton().SetupRemoveFromHotbarButton(ActionSystemUI.highlightedActionSlot as ItemActionBarSlot);
        }

        public static void StartContextMenuCooldown() => Instance.StartCoroutine(StartContextMenuCooldown_Coroutine());

        static IEnumerator StartContextMenuCooldown_Coroutine()
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
