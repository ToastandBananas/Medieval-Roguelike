using UnityEngine;
using TMPro;
using System.Text;
using UnityEngine.EventSystems;
using InteractableObjects;
using Controls;

namespace InventorySystem
{
    public class SplitStack : MonoBehaviour
    {
        public static SplitStack Instance;

        [SerializeField] RectTransform rectTransform;
        [SerializeField] TextMeshProUGUI titleText;
        [SerializeField] TMP_InputField inputField;

        ItemData targetItemData;
        Slot targetParentSlot;

        StringBuilder stringBuilder = new StringBuilder();

        void Awake()
        {
            if (Instance != null)
            {
                Debug.LogError("There's more than one SplitStack! " + transform + " - " + Instance);
                Destroy(gameObject);
                return;
            }
            Instance = this;

            Close();
        }

        void Update()
        {
            if (transform.GetChild(0).gameObject.activeSelf)
            {
                if (GameControls.gamePlayActions.splitStackEnter.WasPressed && inputField.isFocused == false)
                    Enter();
                else if (GameControls.gamePlayActions.splitStackDelete.WasPressed)
                    Backspace();
                else if ((EventSystem.current.IsPointerOverGameObject() == false && (GameControls.gamePlayActions.menuSelect.WasPressed || GameControls.gamePlayActions.menuContext.WasPressed)) || GameControls.gamePlayActions.menuQuickUse.WasPressed)
                    Close();
            }
        }

        public void CheckInput() => ParseInputField();

        public void PressNumber(int number)
        {
            stringBuilder.Append(number.ToString());
            inputField.text = stringBuilder.ToString();
            inputField.Select();
        }

        void ParseInputField()
        {
            if (int.TryParse(inputField.text, out int intValue))
            {
                intValue = Mathf.Clamp(intValue, 1, targetItemData.CurrentStackSize - 1);
                stringBuilder.Clear();
                stringBuilder.Append(intValue.ToString());
                inputField.text = stringBuilder.ToString();
            }
        }

        public void Backspace()
        {
            if (stringBuilder.Length > 0)
            {
                stringBuilder.Remove(stringBuilder.Length - 1, 1);
                inputField.text = stringBuilder.ToString();
            }

            inputField.Select();
        }

        public void Clear()
        {
            stringBuilder.Clear();
            inputField.text = stringBuilder.ToString();
            inputField.Select();
        }

        public void Enter()
        {
            int splitAmount = int.Parse(inputField.text);
            ItemData newItemData = new ItemData(targetItemData);
            newItemData.SetCurrentStackSize(splitAmount);
            targetItemData.AdjustCurrentStackSize(-splitAmount);

            targetParentSlot.InventoryItem.UpdateStackSizeVisuals();

            if (targetItemData.MyInventory != null)
            {
                Inventory myInventory = targetItemData.MyInventory;
                if (myInventory.TryAddItem(newItemData, null, false) == false)
                    InventoryUI.SetupDraggedItem(newItemData, null, (Inventory)null);

                if (myInventory is ContainerInventory && myInventory.ContainerInventory.LooseItem != null && myInventory.ContainerInventory.LooseItem is LooseQuiverItem)
                    myInventory.ContainerInventory.LooseItem.LooseQuiverItem.UpdateArrowMeshes();
                else if (myInventory.MyUnit.UnitEquipment.slotVisualsCreated && myInventory is ContainerInventory && myInventory.ContainerInventory.containerInventoryManager == myInventory.MyUnit.QuiverInventoryManager)
                    myInventory.MyUnit.UnitEquipment.GetEquipmentSlot(EquipSlot.Quiver).InventoryItem.QuiverInventoryItem.UpdateQuiverSprites();
            }
            else
                InventoryUI.SetupDraggedItem(newItemData, null, (Inventory)null);

            Close();
        }

        public void Close()
        {
            if (transform.GetChild(0).gameObject.activeSelf == false)
                return;

            transform.GetChild(0).gameObject.SetActive(false);
            targetItemData = null;
            targetParentSlot = null;
            Clear();
        }

        public void Open(ItemData targetItemData, Slot targetSlot)
        {
            if (transform.GetChild(0).gameObject.activeSelf)
                return;

            this.targetItemData = targetItemData;
            targetParentSlot = targetSlot.ParentSlot();
            titleText.text = targetItemData.Name();

            SetupMenuPosition();
            transform.GetChild(0).gameObject.SetActive(true);
            inputField.Select();
        }

        void SetupMenuPosition()
        {
            float xPosAddon = rectTransform.sizeDelta.x / 2f;
            float yPosAddon = rectTransform.sizeDelta.y / 2f;

            // Get the desired position:
            // If the mouse position is too close to the top or bottom of the screen
            if (Input.mousePosition.y >= (Screen.height - (rectTransform.sizeDelta.y * 2f)))
                yPosAddon = -rectTransform.sizeDelta.y / 2f;
            else if (Input.mousePosition.y <= (0 + (rectTransform.sizeDelta.y * 2f)))
                yPosAddon = rectTransform.sizeDelta.y / 2f;

            // If the mouse position is too far to the right of the screen
            if (Input.mousePosition.x >= (Screen.width - (rectTransform.sizeDelta.x * 2)))
                xPosAddon = -rectTransform.sizeDelta.x / 2f;

            transform.position = Input.mousePosition + new Vector3(xPosAddon, yPosAddon);
        }
    }
}
