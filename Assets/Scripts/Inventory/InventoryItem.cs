using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class InventoryItem : MonoBehaviour
{
    public Inventory myInventory { get; private set; }
    public CharacterEquipment myCharacterEquipment { get; private set; }
    public ItemData itemData { get; private set; }

    [Header("Components")]
    [SerializeField] protected Image iconImage;
    [SerializeField] protected RectTransform rectTransform;
    [SerializeField] protected Slot mySlot;
    [SerializeField] protected TextMeshProUGUI stackSizeText;

    readonly int slotSize = 60;

    public virtual void DropItem()
    {
        if (mySlot != null && mySlot.IsFull() == false)
            return;

        if (mySlot == null && InventoryUI.Instance.DraggedItem() == this && itemData.Item() == null)
            return;

        LooseItem looseItem = LooseItemPool.Instance.GetLooseItemFromPool();

        Vector3 dropDirection = GetDropDirection(GetMyUnit());

        SetupItemDrop(looseItem, itemData.Item(), dropDirection);

        float randomForceMagnitude = Random.Range(50f, 300f);

        // Apply force to the dropped item
        looseItem.RigidBody().AddForce(dropDirection * randomForceMagnitude, ForceMode.Impulse);

        if (myInventory != null)
            myInventory.ItemDatas().Remove(itemData);
        else 
        {
            EquipmentSlot equipmentSlot = null;
            if (mySlot != null)
                equipmentSlot = mySlot as EquipmentSlot;
            else if (this == InventoryUI.Instance.DraggedItem())
                equipmentSlot = InventoryUI.Instance.parentSlotDraggedFrom as EquipmentSlot;

            if (equipmentSlot != null)
                myCharacterEquipment.EquippedItemDatas()[(int)equipmentSlot.EquipSlot()] = null;
        }

        if (mySlot != null)
            mySlot.parentSlot.ClearItem();
        else if (this == InventoryUI.Instance.DraggedItem())
        {
            if (InventoryUI.Instance.parentSlotDraggedFrom != null)
                InventoryUI.Instance.parentSlotDraggedFrom.ClearItem();

            InventoryUI.Instance.DisableDraggedItem();
        }
    }

    public virtual void SetupItemDrop(LooseItem looseItem, Item item, Vector3 dropDirection)
    {
        if (item.pickupMesh != null)
            looseItem.SetupMesh(item.pickupMesh, item.pickupMeshRendererMaterial);
        else if (item.meshes[0] != null)
            looseItem.SetupMesh(item.meshes[0], item.meshRendererMaterials[0]);
        else
            Debug.LogWarning("Mesh info has not been set on the ScriptableObject for: " + item.name);

        // Set the LooseItem's position to be slightly in front of the Unit dropping the item
        looseItem.transform.position = GetMyUnit().transform.position + new Vector3(0, GetMyUnit().ShoulderHeight(), 0) + (dropDirection / 2);

        // Randomize the rotation and set active
        looseItem.transform.rotation = Quaternion.Euler(new Vector3(Random.Range(0f, 360f), Random.Range(0f, 360f), Random.Range(0f, 360f)));
        looseItem.gameObject.SetActive(true);
    }

    Unit GetMyUnit()
    {
        if (myInventory != null)
            return myInventory.MyUnit();
        else
            return myCharacterEquipment.MyUnit();
    }

    protected Vector3 GetDropDirection(Unit myUnit)
    {
        Vector3 forceDirection =  myUnit.transform.forward; // In front of myUnit
        float raycastDistance = 1.2f;
        if (Physics.Raycast(myUnit.transform.position, forceDirection, out RaycastHit hit, raycastDistance, myUnit.unitActionHandler.AttackObstacleMask()))
        {
            forceDirection = -myUnit.transform.forward; // Behind myUnit
            if (Physics.Raycast(myUnit.transform.position, forceDirection, raycastDistance, myUnit.unitActionHandler.AttackObstacleMask()))
            {
                forceDirection = myUnit.transform.right; // Right of myUnit
                if (Physics.Raycast(myUnit.transform.position, forceDirection, raycastDistance, myUnit.unitActionHandler.AttackObstacleMask()))
                {
                    forceDirection = -myUnit.transform.right; // Left of myUnit
                    if (Physics.Raycast(myUnit.transform.position, forceDirection, raycastDistance, myUnit.unitActionHandler.AttackObstacleMask()))
                        forceDirection = myUnit.transform.up; // Above myUnit
                }
            }
        }

        // Add some randomness to the force direction
        float randomAngleRange = Random.Range(-25f, 25f); // Random angle range in degrees
        Quaternion randomRotation = Quaternion.Euler(0, randomAngleRange, 0);
        forceDirection = randomRotation * forceDirection;

        return forceDirection;
    }

    public Vector2 GetInventoryItemOffset()
    {
        int width = itemData.Item().width;
        int height = itemData.Item().height;
        return new Vector2((-0.5f * (width - 1)) * rectTransform.rect.width, (0.5f * (height - 1))) * rectTransform.rect.height;
    }

    public Vector2 GetDraggedItemOffset()
    {
        int width = itemData.Item().width;
        int height = itemData.Item().height;

        return new Vector2(((-width * slotSize) / 2) + (slotSize / 2), ((height * slotSize) / 2) - (slotSize / 2));
    }

    public void SetupSprite()
    {
        iconImage.sprite = itemData.Item().inventorySprite;
        if (mySlot is InventorySlot)
        {
            rectTransform.offsetMin = new Vector2(-slotSize * (itemData.Item().width - 1), 0);
            rectTransform.offsetMax = new Vector2(0, slotSize * (itemData.Item().height - 1));
        }
        else
            rectTransform.sizeDelta = new Vector2(slotSize * itemData.Item().width, slotSize * itemData.Item().height);

        iconImage.enabled = true;
    }

    public void SetupDraggedSprite()
    {
        iconImage.sprite = itemData.Item().inventorySprite;
        rectTransform.sizeDelta = new Vector2(slotSize * itemData.Item().width, slotSize * itemData.Item().height);
        iconImage.enabled = true;
    }

    public void UpdateStackSizeText()
    {
        if (mySlot == null)
        {
            if (itemData.CurrentStackSize() == 1)
                stackSizeText.text = "";
            else
                stackSizeText.text = itemData.CurrentStackSize().ToString();
        }
        else
        {
            if (mySlot is InventorySlot)
            {
                InventorySlot myInventorySlot = mySlot as InventorySlot;
                if (myInventorySlot.parentSlot == null)
                    return;

                if (myInventorySlot.parentSlot.InventoryItem().itemData.CurrentStackSize() == 1)
                    myInventorySlot.parentSlot.InventoryItem().stackSizeText.text = "";
                else
                    myInventorySlot.parentSlot.InventoryItem().stackSizeText.text = myInventorySlot.parentSlot.InventoryItem().itemData.CurrentStackSize().ToString();
            }
            else
            {
                EquipmentSlot myEquipmentSlot = mySlot as EquipmentSlot;
                if (myEquipmentSlot.InventoryItem().itemData.CurrentStackSize() == 1)
                    myEquipmentSlot.InventoryItem().stackSizeText.text = "";
                else
                    myEquipmentSlot.InventoryItem().stackSizeText.text = myEquipmentSlot.InventoryItem().itemData.CurrentStackSize().ToString();
            }
        }
    }

    public void ClearStackSizeText()
    {
        if (mySlot == null)
            stackSizeText.text = "";
        else
        {
            if (mySlot is InventorySlot)
            {
                InventorySlot myInventorySlot = mySlot as InventorySlot;
                if (myInventorySlot.parentSlot == null)
                    return;

                myInventorySlot.parentSlot.InventoryItem().stackSizeText.text = "";
            }
            else
            {
                EquipmentSlot myEquipmentSlot = mySlot as EquipmentSlot;
                myEquipmentSlot.InventoryItem().stackSizeText.text = "";
            }
        }
    }

    public void DisableSprite()
    {
        iconImage.sprite = null;
        iconImage.enabled = false;
    }

    public void SetMyInventory(Inventory inv) => myInventory = inv;

    public void SetMyCharacterEquipment(CharacterEquipment charEquipment) => myCharacterEquipment = charEquipment;

    public void SetItemData(ItemData newItemData) => itemData = newItemData;

    public RectTransform RectTransform() => rectTransform;
}
