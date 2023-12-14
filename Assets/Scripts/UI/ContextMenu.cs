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
using UnitSystem.ActionSystem.Actions;

namespace GeneralUI
{
    public class ContextMenu : MonoBehaviour
    {
        public static ContextMenu Instance { get; private set; }

        [SerializeField] GameObject contextMenuButtonPrefab;

        static List<ContextMenuButton> contextButtons = new();
        static RectTransform rectTransform;

        public static Unit TargetUnit { get; private set; }
        public static Slot TargetSlot { get; private set; }
        public static Interactable TargetInteractable { get; private set; }

        static readonly WaitForSeconds buildContextMenuCooldown = new(0.2f);
        static readonly WaitForSeconds disableContextMenuDelay = new(0.1f);
        public static bool IsActive { get; private set; }
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
            if (IsActive && GameControls.gamePlayActions.menuQuickUse.WasPressed || ((PlayerInput.Instance.HighlightedInteractable == null || PlayerInput.Instance.HighlightedInteractable != TargetInteractable) && (InventoryUI.activeSlot == null || (InventoryUI.activeSlot.ParentSlot() != null && !InventoryUI.activeSlot.ParentSlot().IsFull()))
                && (GameControls.gamePlayActions.menuSelect.WasPressed || GameControls.gamePlayActions.menuContext.WasPressed)))
            {
                StartCoroutine(DelayDisableContextMenu());
            }

            if (contextMenuHoldTimer < maxContextMenuHoldTime && GameControls.gamePlayActions.menuContext.IsPressed)
                contextMenuHoldTimer += Time.deltaTime;

            // Don't allow context menu actions while an action is already queued or when dragging items
            if (UnitManager.player.UnitActionHandler.QueuedActions.Count > 0 || InventoryUI.isDraggingItem || ActionSystemUI.IsDraggingAction)
                return;
            
            if (GameControls.gamePlayActions.menuContext.WasReleased)
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

            TargetSlot = null;
            TargetInteractable = PlayerInput.Instance.HighlightedInteractable;
            TargetUnit = PlayerInput.Instance.HighlightedUnit;
            if (TargetUnit != null && TargetUnit.HealthSystem.IsDead)
                TargetUnit.UnitInteractable.UpdateGridPosition();

            if (InventoryUI.activeSlot != null)
            {
                TargetSlot = InventoryUI.activeSlot.ParentSlot();
                if (!TargetSlot.IsFull())
                    TargetSlot = null;
                TargetUnit = null;
                TargetInteractable = null;

                if (TargetSlot is EquipmentSlot)
                {
                    EquipmentSlot equipmentSlot = TargetSlot as EquipmentSlot;
                    if (UnitEquipment.IsHeldItemEquipSlot(equipmentSlot.EquipSlot) && equipmentSlot.IsFull() && equipmentSlot.UnitEquipment.EquipSlotHasItem(equipmentSlot.EquipSlot) == false)
                        TargetSlot = equipmentSlot.UnitEquipment.GetEquipmentSlot(equipmentSlot.UnitEquipment.GetOppositeWeaponEquipSlot(equipmentSlot.EquipSlot));
                }
            }

            if (TargetInteractable != null && Vector3.Distance(TargetInteractable.GridPosition().WorldPosition, UnitManager.player.WorldPosition) > LevelGrid.diaganolDistance)
            {
                TargetUnit = null;
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
                CreateThrowItemButton();
                CreateSplitStackButton();
                CreateAddItemToHotbarButton();
                CreateRemoveFromHotbarButton();
                CreateDropItemButton();

                if (EventSystem.current.IsPointerOverGameObject() == false && ((TargetInteractable == null && TargetUnit == null && TargetSlot == null && activeCount != 1) 
                    || (TargetInteractable != null && Vector3.Distance(TargetInteractable.GridPosition().WorldPosition, UnitManager.player.WorldPosition) > LevelGrid.diaganolDistance)
                    || (TargetUnit != null && Vector3.Distance(TargetUnit.WorldPosition, UnitManager.player.WorldPosition) > LevelGrid.diaganolDistance)))
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
                IsActive = true;
                StartContextMenuCooldown();

                SetupMenuSize();
                SetupMenuPosition();
            }
        }

        public static void BuildReloadActionContextMenu()
        {
            if (onCooldown)
                return;

            SplitStack.Instance.Close();
            DisableContextMenu(true); 
            
            TargetSlot = null;
            TargetInteractable = null;
            TargetUnit = null;

            CreateReloadButtons(out int buttonCount);

            if (buttonCount > 0)
            {
                IsActive = true;
                StartContextMenuCooldown();

                SetupMenuSize();
                SetupMenuPosition_ActionButton();
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

        public static void BuildThrowActionContextMenu()
        {
            if (onCooldown)
                return;

            SplitStack.Instance.Close();
            DisableContextMenu(true);

            TargetSlot = null;
            TargetInteractable = null;
            TargetUnit = null;

            CreateThrowWeaponButtons(out int buttonCount);

            if (buttonCount > 0)
            {
                IsActive = true;
                StartContextMenuCooldown();

                SetupMenuSize();
                SetupMenuPosition_ActionButton();
            }
        }

        static void CreateThrowWeaponButtons(out int buttonCount)
        {
            buttonCount = 0;
            Action_Throw throwAction = UnitManager.player.UnitActionHandler.GetAction<Action_Throw>();
            if (throwAction == null)
                return;

            List<ItemData> uniqueThrowables = ListPool<ItemData>.Claim();
            HeldMeleeWeapon leftHeldMeleeWeapon = UnitManager.player.UnitMeshManager.GetLeftHeldMeleeWeapon();
            HeldMeleeWeapon rightHeldMeleeWeapon = UnitManager.player.UnitMeshManager.GetRightHeldMeleeWeapon();

            // First reduce the list to held weapons and unique belt throwables only
            for (int i = 0; i < throwAction.Throwables.Count; i++)
            {
                if (i == 0)
                    uniqueThrowables.Add(throwAction.Throwables[i]);
                else
                {
                    bool isUnique = false;
                    for (int j = 0; j < uniqueThrowables.Count; j++)
                    {
                        // Held weapons
                        if ((leftHeldMeleeWeapon != null && leftHeldMeleeWeapon.ItemData == throwAction.Throwables[i]) || (rightHeldMeleeWeapon != null && rightHeldMeleeWeapon.ItemData == throwAction.Throwables[i]))
                            isUnique = true;
                        else // Belt throwables
                        {
                            if (!throwAction.Throwables[i].IsEqual(uniqueThrowables[j]))
                                isUnique = true;
                        }
                    }

                    if (isUnique)
                        uniqueThrowables.Add(throwAction.Throwables[i]);
                }
            }

            for (int i = 0; i < uniqueThrowables.Count; i++)
            {
                GetContextMenuButton().SetupThrowWeaponButton(throwAction.Throwables[i]);
            }

            buttonCount = uniqueThrowables.Count;
            ListPool<ItemData>.Release(uniqueThrowables);
        }

        static void CreateThrowItemButton()
        {
            if (TargetSlot == null || !TargetSlot.IsFull() || TargetSlot.InventoryItem.GetMyUnit() != UnitManager.player || (TargetSlot is EquipmentSlot && (!TargetSlot.EquipmentSlot.IsHeldItemSlot() || TargetSlot.GetItemData().Item is Item_MeleeWeapon == false)))
                return;

            GetContextMenuButton().SetupThrowItemButton();
        }

        static void CreateMoveToButton()
        {
            GridPosition targetGridPosition;
            if (TargetInteractable != null)
                targetGridPosition = LevelGrid.GetNearestSurroundingGridPosition(TargetInteractable.GridPosition(), UnitManager.player.GridPosition, LevelGrid.diaganolDistance, TargetInteractable is Interactable_LooseItem);
            else
                targetGridPosition = WorldMouse.CurrentGridPosition();

            if (LevelGrid.IsValidGridPosition(targetGridPosition) == false)
                return;

            GetContextMenuButton().SetupMoveToButton(targetGridPosition);
        }

        static void CreateAttackButton()
        {
            if (TargetUnit == null || TargetUnit.HealthSystem.IsDead || UnitManager.player.Vision.IsVisible(TargetUnit) == false
                || (UnitManager.player.UnitEquipment.MeleeWeaponEquipped == false && (UnitManager.player.UnitEquipment.RangedWeaponEquipped == false || UnitManager.player.UnitEquipment.HasValidAmmunitionEquipped() == false) && UnitManager.player.Stats.CanFightUnarmed == false))
                return;

            Action_Base selectedAction = UnitManager.player.SelectedAction;
            if ((selectedAction is Action_Move == false && selectedAction.IsDefaultAttackAction == false) || (TargetUnit.IsCompletelySurrounded(UnitManager.player.GetAttackRange()) && UnitManager.player.GetAttackRange() < 2f))
                return;

            GetContextMenuButton().SetupAttackButton();
        }

        static void CreateAddToBagButtons()
        {
            if ((TargetSlot == null || !TargetSlot.IsFull()) && (TargetInteractable == null || TargetInteractable is Interactable_LooseItem == false))
                return;

            ItemData itemData = null;
            if (TargetSlot != null)
            {
                if (TargetSlot is EquipmentSlot)
                    return;

                itemData = TargetSlot.GetItemData();
            }
            else if (TargetInteractable != null && TargetInteractable is Interactable_LooseItem)
            {
                Interactable_LooseItem targetLooseItem = TargetInteractable as Interactable_LooseItem;
                if (targetLooseItem is Interactable_LooseContainerItem)
                {
                    Interactable_LooseContainerItem looseContainerItem = targetLooseItem as Interactable_LooseContainerItem;
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
            if ((TargetSlot == null || !TargetSlot.IsFull()) && (TargetInteractable == null || TargetInteractable is Interactable_LooseItem == false))
                return;

            ItemData itemData = null;
            if (TargetSlot != null)
            {
                if (TargetSlot is EquipmentSlot)
                {
                    EquipmentSlot targetEquipmentSlot = TargetSlot as EquipmentSlot;
                    if (targetEquipmentSlot.UnitEquipment == UnitManager.player.UnitEquipment)
                        return;
                }
                else if (TargetSlot is InventorySlot)
                {
                    InventorySlot targetInventorySlot = TargetSlot as InventorySlot;
                    if (targetInventorySlot.myInventory == UnitManager.player.UnitInventoryManager.MainInventory)
                        return;
                }

                itemData = TargetSlot.GetItemData();
            }
            else if (TargetInteractable != null && TargetInteractable is Interactable_LooseItem)
            {
                Interactable_LooseItem targetLooseItem = TargetInteractable as Interactable_LooseItem;
                if (targetLooseItem is Interactable_LooseContainerItem)
                {
                    Interactable_LooseContainerItem looseContainerItem = targetLooseItem as Interactable_LooseContainerItem;
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
            if (TargetSlot != null && TargetSlot is ContainerEquipmentSlot && TargetSlot.IsFull() && TargetSlot.GetItemData().Item is Item_WearableContainer)
            {
                ContainerEquipmentSlot containerEquipmentSlot = TargetSlot as ContainerEquipmentSlot;
                if (containerEquipmentSlot.containerInventoryManager == null)
                {
                    Debug.LogWarning($"{containerEquipmentSlot.name} does not have an assigned ContainerInventoryManager...");
                    return;
                }

                if (containerEquipmentSlot.GetItemData().Item.WearableContainer.HasAnInventory() == false)
                    return;

                if (containerEquipmentSlot.containerInventoryManager.ParentInventory.SlotVisualsCreated)
                {
                    CreateCloseContainerButton();
                    return;
                }
            }
            else if (TargetInteractable != null && TargetInteractable is Interactable_LooseContainerItem)
            {
                Interactable_LooseContainerItem looseContainerItem = TargetInteractable as Interactable_LooseContainerItem;
                if (looseContainerItem.ItemData.Item is Item_WearableContainer && looseContainerItem.ItemData.Item.WearableContainer.HasAnInventory() == false) // Some belts, for example, won't have an inventory, so don't create this button
                    return;

                if (Vector3.Distance(TargetInteractable.GridPosition().WorldPosition, UnitManager.player.WorldPosition) > LevelGrid.diaganolDistance)
                    return;

                if (looseContainerItem.ContainerInventoryManager.ParentInventory.SlotVisualsCreated)
                {
                    CreateCloseContainerButton();
                    return;
                }
            }
            else if (TargetUnit != null && TargetUnit.HealthSystem.IsDead)
            {
                if (Vector3.Distance(LevelGrid.GetGridPosition(TargetUnit.transform.position).WorldPosition, UnitManager.player.WorldPosition) > LevelGrid.diaganolDistance)
                    return;

                if (TargetUnit.UnitEquipment.SlotVisualsCreated)
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
            if ((TargetSlot == null || !TargetSlot.IsFull()) && (TargetInteractable == null || TargetInteractable is Interactable_LooseItem == false))
                return;

            ItemData itemData = null;
            if (TargetSlot != null)
                itemData = TargetSlot.GetItemData();
            else if (TargetInteractable != null)
            {
                Interactable_LooseItem looseItem = TargetInteractable as Interactable_LooseItem;
                itemData = looseItem.ItemData;

                if (itemData.Item is Item_Equipment == false)
                    return;
            }

            if (itemData == null || itemData.Item == null || !itemData.Item.IsUsable)
                return;

            if (itemData.Item is Item_Ammunition && UnitManager.player.QuiverInventoryManager.Contains(itemData))
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
            else if (itemData.Item.MaxStackSize > 1 && itemData.CurrentStackSize > 1 && itemData.Item is Item_Ammunition == false)
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
            if (TargetSlot == null || TargetSlot.IsFull() == false)
                return;

            if (TargetSlot.GetItemData().Item.MaxStackSize <= 1 || TargetSlot.GetItemData().CurrentStackSize <= 1)
                return;

            GetContextMenuButton().SetupSplitStackButton(TargetSlot.GetItemData());
        }

        static void CreateDropItemButton()
        {
            if (TargetSlot == null || !TargetSlot.IsFull())
                return;

            if (TargetSlot is ContainerEquipmentSlot)
            {
                ContainerEquipmentSlot containerSlot = TargetSlot as ContainerEquipmentSlot;
                if (containerSlot.InventoryItem.myUnitEquipment.MyUnit == UnitManager.player && containerSlot.containerInventoryManager.ContainsAnyItems())
                    return;
            }

            GetContextMenuButton().SetupDropItemButton();
        }

        static void CreateAddItemToHotbarButton()
        {
            if (TargetSlot == null || TargetSlot is EquipmentSlot || !TargetSlot.IsFull() || TargetSlot.InventoryItem.GetMyUnit() != UnitManager.player)
                return;
            
            ItemActionBarSlot itemActionSlot = ActionSystemUI.GetNextAvailableItemActionBarSlot();
            if (itemActionSlot == null || ActionSystemUI.ItemActionBarAlreadyHasItem(TargetSlot.GetItemData()))
                return;
            
            GetContextMenuButton().SetupAddItemToHotbarButton(itemActionSlot);
        }

        static void CreateRemoveFromHotbarButton()
        {
            // This only works for ItemActionBar slots with items in them
            if (ActionSystemUI.HighlightedActionSlot == null || ActionSystemUI.HighlightedActionSlot is ItemActionBarSlot == false || ActionSystemUI.HighlightedActionSlot.ItemActionBarSlot.ItemData == null || ActionSystemUI.HighlightedActionSlot.ItemActionBarSlot.ItemData.Item == null)
                return;

            GetContextMenuButton().SetupRemoveFromHotbarButton(ActionSystemUI.HighlightedActionSlot as ItemActionBarSlot);
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
            if (!IsActive)
                return;

            if (!forceDisable && onCooldown)
                return;

            IsActive = false;

            for (int i = 0; i < contextButtons.Count; i++)
                contextButtons[i].Disable();
        }

        static IEnumerator DelayDisableContextMenu()
        {
            yield return disableContextMenuDelay;
            DisableContextMenu();
        }

        static ContextMenuButton GetContextMenuButton()
        {
            for (int i = 0; i < contextButtons.Count; i++)
            {
                if (!contextButtons[i].gameObject.activeSelf)
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
                if (!contextButtons[i].gameObject.activeSelf)
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
            SetupButtonTransform(out float xPosAddon, out float yPosAddon);
            Instance.transform.position = Input.mousePosition + new Vector3(xPosAddon, yPosAddon);
        }

        static void SetupMenuPosition_ActionButton()
        {
            SetupButtonTransform(out float xPosAddon, out float yPosAddon);

            float slotWidthAddOn = 0f;
            float slotHeightAddOn = 0f;
            Vector3 slotPosition = Input.mousePosition;
            if (ActionSystemUI.SelectedActionSlot != null)
            {
                slotWidthAddOn = ActionSystemUI.SelectedActionSlot.RectTransform.rect.width * TooltipManager.Canvas.scaleFactor;
                slotHeightAddOn = ActionSystemUI.SelectedActionSlot.RectTransform.rect.height * TooltipManager.Canvas.scaleFactor / 2f;
                slotPosition = ActionSystemUI.SelectedActionSlot.transform.position;
            }
            else if (ActionSystemUI.HighlightedActionSlot != null)
            {
                slotWidthAddOn = ActionSystemUI.HighlightedActionSlot.RectTransform.rect.width * TooltipManager.Canvas.scaleFactor;
                slotHeightAddOn = ActionSystemUI.HighlightedActionSlot.RectTransform.rect.height * TooltipManager.Canvas.scaleFactor / 2f;
                slotPosition = ActionSystemUI.HighlightedActionSlot.transform.position;
            }

            Instance.transform.position = slotPosition + new Vector3((xPosAddon / 2f) - slotWidthAddOn, yPosAddon + slotHeightAddOn);
        }

        static void SetupButtonTransform(out float xPosAddon, out float yPosAddon)
        {
            int activeButtonCount = 0;
            for (int i = 0; i < contextButtons.Count; i++)
            {
                if (contextButtons[i].gameObject.activeSelf)
                    activeButtonCount++;
            }

            xPosAddon = rectTransform.rect.width * TooltipManager.Canvas.scaleFactor / 2f;
            yPosAddon = activeButtonCount * contextButtons[0].RectTransform.rect.height * TooltipManager.Canvas.scaleFactor / 2f;

            // Get the desired position:
            // If the mouse position is too close to the top of the screen
            if (Input.mousePosition.y >= (Screen.height - (activeButtonCount * contextButtons[0].RectTransform.rect.height * TooltipManager.Canvas.scaleFactor * 1.2f)))
                yPosAddon = -activeButtonCount * contextButtons[0].RectTransform.rect.height * TooltipManager.Canvas.scaleFactor / 2f;

            // If the mouse position is too far to the right of the screen
            if (Input.mousePosition.x >= (Screen.width - (rectTransform.rect.width * TooltipManager.Canvas.scaleFactor * 1.2f)))
                xPosAddon = -rectTransform.rect.width * TooltipManager.Canvas.scaleFactor / 2f;
        }
    }
}
