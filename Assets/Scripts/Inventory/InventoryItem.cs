using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class InventoryItem : MonoBehaviour
{
    public Inventory myInventory { get; private set; }
    public ItemData itemData { get; private set; }

    [Header("Components")]
    [SerializeField] Image iconImage;
    [SerializeField] RectTransform rectTransform;
    [SerializeField] Slot mySlot;
    [SerializeField] TextMeshProUGUI stackSizeText;

    readonly int slotSize = 60;

    public void DropItem()
    {
        if (mySlot != null && mySlot.IsFull() == false)
            return;

        if (mySlot == null && InventoryUI.Instance.DraggedItem() == this && itemData.Item() == null)
            return;

        Unit myUnit = myInventory.MyUnit();
        LooseItem looseItem = LooseItemPool.Instance.GetLooseItemFromPool();
        LooseItem looseProjectile = null;

        Vector3 dropDirection = GetDropDirection(myUnit);

        SetupItemDrop(looseItem, itemData.Item(), dropDirection); 
        
        if (myUnit.GetRangedWeapon().ItemData() == itemData && myUnit.RangedWeaponEquipped())
        {
            HeldRangedWeapon heldRangedWeapon = myUnit.GetRangedWeapon();
            if (heldRangedWeapon.isLoaded)
            {
                looseProjectile = LooseItemPool.Instance.GetLooseItemFromPool();
                Item projectileItem = heldRangedWeapon.loadedProjectile.ItemData().Item();
                SetupItemDrop(looseProjectile, projectileItem, dropDirection);
                heldRangedWeapon.loadedProjectile.Disable();
            }
        }

        float randomForceMagnitude = Random.Range(50f, 300f);

        // Get the Rigidbody component(s) and apply force
        looseItem.RigidBody().AddForce(dropDirection * randomForceMagnitude, ForceMode.Impulse);
        if (looseProjectile != null)
            looseProjectile.RigidBody().AddForce(dropDirection * randomForceMagnitude, ForceMode.Impulse);

        if (myUnit != UnitManager.Instance.player && UnitManager.Instance.player.vision.IsVisible(myUnit) == false)
        {
            looseItem.HideMeshRenderer();
            if (looseProjectile != null)
                looseProjectile.HideMeshRenderer();
        }

        myInventory.ItemDatas().Remove(itemData);

        if (mySlot != null)
            mySlot.parentSlot.ClearItem();
        else if (this == InventoryUI.Instance.DraggedItem())
        {
            if (InventoryUI.Instance.parentSlotDraggedFrom != null)
                InventoryUI.Instance.parentSlotDraggedFrom.ClearItem();

            InventoryUI.Instance.DisableDraggedItem();
        }
    }

    void SetupItemDrop(LooseItem looseItem, Item item, Vector3 dropDirection)
    {
        if (item.pickupMesh != null)
            looseItem.SetupMesh(item.pickupMesh, item.pickupMeshRendererMaterial);
        else if (item.meshes[0] != null)
            looseItem.SetupMesh(item.meshes[0], item.meshRendererMaterials[0]);
        else
            Debug.LogWarning("Mesh info has not been set on the ScriptableObject for: " + item.name);

        // Set the LooseItem's position to be slightly in front of the Unit dropping the item
        looseItem.transform.position = myInventory.MyUnit().transform.position + new Vector3(0, myInventory.MyUnit().ShoulderHeight(), 0) + (dropDirection / 2);

        // Randomize the rotation and set active
        looseItem.transform.rotation = Quaternion.Euler(new Vector3(Random.Range(0f, 360f), Random.Range(0f, 360f), Random.Range(0f, 360f)));
        looseItem.gameObject.SetActive(true);
    }

    Vector3 GetDropDirection(Unit myUnit)
    {
        Vector3 forceDirection =  myUnit.transform.forward; // In front of myUnit
        if (Physics.Raycast(myUnit.transform.position, forceDirection, out RaycastHit hit, 1.2f, myUnit.unitActionHandler.AttackObstacleMask()))
        {
            Debug.Log(hit.collider.name);
            forceDirection = -myUnit.transform.forward; // Behind myUnit
            if (Physics.Raycast(myUnit.transform.position, forceDirection, 1.2f, myUnit.unitActionHandler.AttackObstacleMask()))
            {
                forceDirection = myUnit.transform.right; // Right of myUnit
                if (Physics.Raycast(myUnit.transform.position, forceDirection, 1.2f, myUnit.unitActionHandler.AttackObstacleMask()))
                {
                    forceDirection = -myUnit.transform.right; // Left of myUnit
                    if (Physics.Raycast(myUnit.transform.position, forceDirection, 1.2f, myUnit.unitActionHandler.AttackObstacleMask()))
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
        rectTransform.offsetMin = new Vector2(-slotSize * (itemData.Item().width - 1), 0);
        rectTransform.offsetMax = new Vector2(0, slotSize * (itemData.Item().height - 1));
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
            if (mySlot.parentSlot == null)
                return;

            Slot stackSizeSlot = mySlot.parentSlot;//myInventory.GetSlotFromCoordinate(new Vector2(mySlot.parentSlot.slotCoordinate.x, mySlot.parentSlot.slotCoordinate.y + mySlot.parentSlot.InventoryItem().itemData.Item().height - 1));
            if (mySlot.parentSlot.InventoryItem().itemData.CurrentStackSize() == 1)
                stackSizeSlot.InventoryItem().stackSizeText.text = "";
            else
                stackSizeSlot.InventoryItem().stackSizeText.text = mySlot.parentSlot.InventoryItem().itemData.CurrentStackSize().ToString();
        }
    }

    public void ClearStackSizeText()
    {
        if (mySlot == null)
            stackSizeText.text = "";
        else
        {
            if (mySlot.parentSlot == null)
                return;

            mySlot.parentSlot.InventoryItem().stackSizeText.text = "";//myInventory.GetSlotFromCoordinate(new Vector2(mySlot.parentSlot.slotCoordinate.x, mySlot.parentSlot.slotCoordinate.y + mySlot.parentSlot.InventoryItem().itemData.Item().height - 1)).InventoryItem().stackSizeText.text = "";
        }
    }

    public void DisableSprite()
    {
        iconImage.sprite = null;
        iconImage.enabled = false;
    }

    public void SetMyInventory(Inventory inv) => myInventory = inv;

    public void SetItemData(ItemData newItemData) => itemData = newItemData;

    public RectTransform RectTransform() => rectTransform;
}
