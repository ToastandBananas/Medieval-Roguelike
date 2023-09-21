using UnityEngine;
using UnityEngine.EventSystems;
using System.Collections;
using System.Collections.Generic;

public class ContextMenu : MonoBehaviour
{
    public static ContextMenu Instance { get; private set; }

    [SerializeField] GameObject contextMenuButtonPrefab;

    List<ContextMenuButton> contextButtons = new List<ContextMenuButton>();
    RectTransform rectTransform;

    Slot targetSlot;
    Interactable targetInteractable;

    WaitForSeconds buildContextMenuCooldown = new WaitForSeconds(0.2f);
    WaitForSeconds disableContextMenuDelay = new WaitForSeconds(0.1f);
    bool onCooldown, isActive;

    float contextMenuHoldTimer;
    readonly float maxContextMenuHoldTime = 0.125f;
    readonly float minButtonWidth = 100;

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

        for (int i = 0; i < transform.childCount; i++)
        {
            contextButtons[i] = transform.GetChild(i).GetComponent<ContextMenuButton>();
        }
    }

    void Update()
    {
        if (isActive && GameControls.gamePlayActions.menuQuickUse.WasPressed || ((PlayerInput.Instance.highlightedInteractable == null || PlayerInput.Instance.highlightedInteractable != targetInteractable)
            && (GameControls.gamePlayActions.menuSelect.WasPressed || GameControls.gamePlayActions.menuContext.WasPressed)))
        {
            StartCoroutine(DelayDisableContextMenu());
        }

        if (contextMenuHoldTimer < maxContextMenuHoldTime && GameControls.gamePlayActions.menuContext.IsPressed)
            contextMenuHoldTimer += Time.deltaTime;

        if (GameControls.gamePlayActions.menuContext.WasReleased)
        {
            if (isActive == false && contextMenuHoldTimer < maxContextMenuHoldTime)
                BuildContextMenu();

            contextMenuHoldTimer = 0;
        }
    }

    public void BuildContextMenu()
    {
        if (onCooldown == false)
        {
            targetInteractable = PlayerInput.Instance.highlightedInteractable;
            if (InventoryUI.Instance.activeSlot != null)
                targetSlot = InventoryUI.Instance.activeSlot.ParentSlot();

            StartCoroutine(BuildContextMenuCooldown());

            if (targetInteractable != null && TacticsPathfindingUtilities.CalculateWorldSpaceDistance_XYZ(targetInteractable.gridPosition, UnitManager.Instance.player.gridPosition) > LevelGrid.diaganolDistance)
            {
                CreateMoveToButton();
            }
            else
            {
                // Create the necessary buttons
                CreateTakeItemButton();
                CreateOpenContainerButton();
                CreateUseItemButton();
                CreateDropItemButton();

                if (EventSystem.current.IsPointerOverGameObject() == false && ((targetInteractable == null && targetSlot == null) || (targetInteractable != null && TacticsPathfindingUtilities.CalculateWorldSpaceDistance_XYZ(targetInteractable.gridPosition, UnitManager.Instance.player.gridPosition) > LevelGrid.diaganolDistance)))
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
                SetupMenuSize();
                SetupMenuPosition();
            }
        }
    }

    void CreateMoveToButton()
    {
        GridPosition targetGridPosition;
        if (targetInteractable != null)
            targetGridPosition = LevelGrid.Instance.GetNearestSurroundingGridPosition(targetInteractable.gridPosition, UnitManager.Instance.player.gridPosition, LevelGrid.diaganolDistance, targetInteractable is LooseItem);
        else
            targetGridPosition = WorldMouse.GetCurrentGridPosition();
        
        if (LevelGrid.IsValidGridPosition(targetGridPosition) == false)
            return;
        
        GetContextMenuButton().SetupMoveToButton(targetGridPosition);
    }

    void CreateTakeItemButton()
    {
        if ((targetSlot == null || targetSlot.IsFull() == false) && (targetInteractable == null || targetInteractable is LooseItem == false))
            return; 
        
        ItemData itemData = null;
        if (targetSlot != null)
        {
            if (targetSlot is EquipmentSlot)
            {
                EquipmentSlot targetEquipmentSlot = targetSlot as EquipmentSlot;
                if (targetEquipmentSlot.CharacterEquipment == UnitManager.Instance.player.CharacterEquipment)
                    return;
            }
            else if (targetSlot is InventorySlot)
            {
                InventorySlot targetInventorySlot = targetSlot as InventorySlot;
                if (targetInventorySlot.myInventory.MyUnit == UnitManager.Instance.player)
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

    void CreateOpenContainerButton()
    {
        if (targetSlot != null && targetSlot is ContainerEquipmentSlot && targetSlot.ParentSlot().IsFull())
        {
            ContainerEquipmentSlot containerEquipmentSlot = targetSlot as ContainerEquipmentSlot;
            if (containerEquipmentSlot.containerInventoryManager == null)
            {
                Debug.LogWarning($"{containerEquipmentSlot.name} does not have an assigned ContainerInventoryManager...");
                return;
            }

            if (containerEquipmentSlot.containerInventoryManager.ParentInventory.SlotVisualsCreated)
            {
                CreateCloseContainerButton();
                return;
            }
        }
        else if (targetInteractable != null && targetInteractable is LooseContainerItem)
        {
            LooseContainerItem looseContainerItem = targetInteractable as LooseContainerItem;
            if (looseContainerItem.ContainerInventoryManager.ParentInventory.SlotVisualsCreated)
            {
                CreateCloseContainerButton();
                return;
            }
        }
        else 
            return;

        GetContextMenuButton().SetupOpenContainerButton();
    }

    void CreateCloseContainerButton() => GetContextMenuButton().SetupCloseContainerButton();

    void CreateUseItemButton()
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

            if (itemData.Item.IsEquipment() == false)
                return;
        }

        if (itemData == null || itemData.Item == null || itemData.Item.isUsable == false)
            return;
        
        if (itemData.Item.maxUses > 1)
        {
            GetContextMenuButton().SetupUseItemButton(itemData, itemData.RemainingUses); // Use all

            if (itemData.RemainingUses >= 4)
                GetContextMenuButton().SetupUseItemButton(itemData, Mathf.CeilToInt(itemData.RemainingUses * 0.75f)); // Use 3/4

            if (itemData.RemainingUses >= 2)
                GetContextMenuButton().SetupUseItemButton(itemData, Mathf.CeilToInt(itemData.RemainingUses * 0.5f)); // Use 1/2

            if (itemData.RemainingUses >= 4)
                GetContextMenuButton().SetupUseItemButton(itemData, Mathf.CeilToInt(itemData.RemainingUses * 0.25f)); // Use 1/4

            if (itemData.RemainingUses >= 10)
                GetContextMenuButton().SetupUseItemButton(itemData, Mathf.CeilToInt(itemData.RemainingUses * 0.1f)); // Use 1/10
        }
        else if (itemData.Item.maxStackSize > 1)
        {
            GetContextMenuButton().SetupUseItemButton(itemData, itemData.CurrentStackSize); // Use all

            if (itemData.CurrentStackSize >= 4)
                GetContextMenuButton().SetupUseItemButton(itemData, Mathf.CeilToInt(itemData.CurrentStackSize * 0.75f)); // Use 3/4

            if (itemData.CurrentStackSize >= 2)
                GetContextMenuButton().SetupUseItemButton(itemData, Mathf.CeilToInt(itemData.CurrentStackSize * 0.5f)); // Use 1/2

            if (itemData.CurrentStackSize >= 4)
                GetContextMenuButton().SetupUseItemButton(itemData, Mathf.CeilToInt(itemData.CurrentStackSize * 0.25f)); // Use 1/4

            if (itemData.CurrentStackSize >= 10)
                GetContextMenuButton().SetupUseItemButton(itemData, Mathf.CeilToInt(itemData.CurrentStackSize * 0.1f)); // Use 1/10

            if (Mathf.CeilToInt(itemData.CurrentStackSize * 0.1f) > 1)
                GetContextMenuButton().SetupUseItemButton(itemData, 1); // Use 1
        }
        else
            GetContextMenuButton().SetupUseItemButton(itemData, 1);
    }

    void CreateDropItemButton()
    {
        if (targetSlot == null || targetSlot.ParentSlot().IsFull() == false)
            return;

        if (targetSlot != null && targetSlot is ContainerEquipmentSlot)
        {
            ContainerEquipmentSlot containerSlot = targetSlot as ContainerEquipmentSlot;
            if (containerSlot.containerInventoryManager.ContainsAnyItems())
                return;
        }

        GetContextMenuButton().SetupDropItemButton();
    }

    IEnumerator BuildContextMenuCooldown()
    {
        if (onCooldown == false)
        {
            onCooldown = true;
            yield return buildContextMenuCooldown;
            onCooldown = false;
        }
    }

    public void DisableContextMenu(bool forceDisable = false)
    {
        if (forceDisable == false && onCooldown) return;
        
        isActive = false;
        targetSlot = null;
        targetInteractable = null;

        for (int i = 0; i < contextButtons.Count; i++)
        {
            contextButtons[i].Disable();
        }
    }

    IEnumerator DelayDisableContextMenu(bool forceDisable = false)
    {
        yield return disableContextMenuDelay;
        DisableContextMenu();
    }

    ContextMenuButton GetContextMenuButton()
    {
        for (int i = 0; i < contextButtons.Count; i++)
        {
            if (contextButtons[i].gameObject.activeSelf == false)
                return contextButtons[i];
        }

        return CreateNewContextMenuButton();
    }

    ContextMenuButton CreateNewContextMenuButton()
    {
        ContextMenuButton contextButton = Instantiate(contextMenuButtonPrefab, transform).GetComponent<ContextMenuButton>();
        contextButtons.Add(contextButton);
        contextButton.gameObject.SetActive(false);
        return contextButton;
    }

    void SetupMenuSize()
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

    void SetupMenuPosition()
    {
        int activeButtonCount = 0;
        for (int i = 0; i < contextButtons.Count; i++)
        {
            if (contextButtons[i].gameObject.activeSelf)
                activeButtonCount++;
        }

        float xPosAddon = rectTransform.sizeDelta.x - (rectTransform.sizeDelta.x / 2f);
        float yPosAddon = (activeButtonCount * contextButtons[0].RectTransform.sizeDelta.y) / 2f;

        // Get the desired position:
        // If the mouse position is too close to the top of the screen
        if (Input.mousePosition.y >= (Screen.height - (activeButtonCount * contextButtons[0].RectTransform.sizeDelta.y * 2)))
            yPosAddon = (-activeButtonCount * contextButtons[0].RectTransform.sizeDelta.y) / 2f;

        // If the mouse position is too far to the right of the screen
        if (Input.mousePosition.x >= (Screen.width - (rectTransform.sizeDelta.x * 2)))
            xPosAddon = -rectTransform.sizeDelta.x / 2f;

        transform.position = Input.mousePosition + new Vector3(xPosAddon, yPosAddon);
    }

    public Slot TargetSlot => targetSlot;

    public Interactable TargetInteractable => targetInteractable;

    public bool IsActive => isActive;
}
