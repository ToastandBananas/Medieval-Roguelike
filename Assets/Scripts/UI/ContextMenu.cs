using UnityEngine;
using UnityEngine.EventSystems;
using System.Collections;
using System.Collections.Generic;

public class ContextMenu : MonoBehaviour
{
    public static ContextMenu Instance { get; private set; }

    public GameObject contextMenuButtonPrefab;

    List<ContextMenuButton> contextButtons = new List<ContextMenuButton>();

    Canvas canvas;
    Slot targetSlot;
    Interactable targetInteractable;

    WaitForSeconds buildContextMenuCooldown = new WaitForSeconds(0.2f);
    bool onCooldown, isActive;

    void Awake()
    {
        if (Instance != null)
        {
            Debug.LogError("There's more than one ContextMenu! " + transform + " - " + Instance);
            Destroy(gameObject);
            return;
        }
        Instance = this;

        canvas = GetComponentInParent<Canvas>();

        for (int i = 0; i < transform.childCount; i++)
        {
            contextButtons[i] = transform.GetChild(i).GetComponent<ContextMenuButton>();
        }
    }

    void Update()
    {
        if (isActive && EventSystem.current.IsPointerOverGameObject() == false
            && (GameControls.gamePlayActions.menuSelect.WasPressed || GameControls.gamePlayActions.menuContext.WasPressed || Input.GetMouseButtonDown(2)))
        {
            DisableContextMenu();
        }

        if (isActive == false && GameControls.gamePlayActions.menuContext.WasPressed && (InventoryUI.Instance.activeSlot != null || PlayerInput.Instance.highlightedInteractable != null))
            BuildContextMenu();
    }

    public void BuildContextMenu()
    {
        if (onCooldown == false)
        {
            targetInteractable = PlayerInput.Instance.highlightedInteractable;
            if (InventoryUI.Instance.activeSlot != null)
                targetSlot = InventoryUI.Instance.activeSlot.ParentSlot();

            StartCoroutine(BuildContextMenuCooldown());

            // Set our context menu's position
            RectTransformUtility.ScreenPointToLocalPointInRectangle(canvas.transform as RectTransform, Input.mousePosition, canvas.worldCamera, out Vector2 pos);
            transform.position = canvas.transform.TransformPoint(pos) + new Vector3(1, -0.5f, 0);

            // If this slot is on the very bottom of the screen
            if (pos.y < -420f)
                transform.position += new Vector3(0, 1.5f, 0);

            // If this slot is on the far right of the inventory menu
            //if (thisInvSlot != null && thisInvSlot.slotCoordinate.x == invUI.maxInventoryWidth)
            //  contextMenu.transform.position += new Vector3(-2, 0, 0);

            if (targetInteractable != null && TacticsPathfindingUtilities.CalculateWorldSpaceDistance_XYZ(targetInteractable.gridPosition, UnitManager.Instance.player.gridPosition) > LevelGrid.diaganolDistance)
            {
                CreateMoveToButton(targetInteractable.gridPosition);
            }
            else
            {
                // Create the necessary buttons
                CreateTakeItemButton();
                CreateOpenContainerButton();
                CreateUseItemButton();
                CreateDropItemButton();
                
                
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
                isActive = true;
        }
    }

    void CreateMoveToButton(GridPosition gridPosition)
    {

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
        if ((targetSlot == null || targetSlot.IsFull() == false) && (targetInteractable == null || targetInteractable is LooseItem))
            return;

        ItemData itemData = null;
        if (targetSlot != null)
            itemData = targetSlot.GetItemData();

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

    public void DisableContextMenu()
    {
        if (onCooldown) return;
        
        isActive = false;
        targetSlot = null;
        targetInteractable = null;

        for (int i = 0; i < contextButtons.Count; i++)
        {
            contextButtons[i].Disable();
        }
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

    public Slot TargetSlot => targetSlot;

    public Interactable TargetInteractable => targetInteractable;

    public bool IsActive => isActive;
}
