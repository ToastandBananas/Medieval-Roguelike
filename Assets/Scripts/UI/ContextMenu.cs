using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections;
using System.Collections.Generic;

public class ContextMenu : MonoBehaviour
{
    public static ContextMenu Instance { get; private set; }

    public GameObject contextMenuButtonPrefab;

    List<ContextMenuButton> contextButtons = new List<ContextMenuButton>();

    Canvas canvas;
    VerticalLayoutGroup verticalLayoutGroup;
    Slot targetSlot;

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
        verticalLayoutGroup = GetComponent<VerticalLayoutGroup>();

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

        if (isActive == false && GameControls.gamePlayActions.menuContext.WasPressed && InventoryUI.Instance.activeSlot != null)
            BuildContextMenu();
    }

    public void BuildContextMenu()
    {
        if (onCooldown == false)
        {
            isActive = true;
            targetSlot = InventoryUI.Instance.activeSlot;

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
            
            // Create the necessary buttons
            CreateOpenContainerButton();
            CreateDropItemButton();
        }
    }

    void CreateOpenContainerButton()
    {
        if (targetSlot != null)
        {
            if (targetSlot.GetParentSlot().IsFull() == false)
                return;

            if (targetSlot.GetParentSlot().GetItemData().Item.IsBackpack() == false)
                return;
        }
        else
            return;

        GetContextMenuButton().SetupOpenContainerButton();
    }

    void CreateDropItemButton()
    {
        if (targetSlot != null)
        {
            if (targetSlot.GetParentSlot().IsFull() == false)
                return;
        }
        else
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

        for (int i = 0; i < contextButtons.Count; i++)
        {
            contextButtons[i].Disable();
        }
    }

    public IEnumerator DelayDisableContextMenu()
    {
        yield return buildContextMenuCooldown;
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

    public Slot TargetSlot => targetSlot;

    public bool IsActive => isActive;
}
