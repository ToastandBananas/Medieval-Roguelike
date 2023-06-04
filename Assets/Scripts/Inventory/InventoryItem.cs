using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class InventoryItem : MonoBehaviour
{
    public Inventory myInventory { get; private set; }
    public ItemData itemData { get; private set; }

    [Header("Components")]
    [SerializeField] Image image;
    [SerializeField] RectTransform rectTransform;
    [SerializeField] Slot mySlot;
    [SerializeField] TextMeshProUGUI stackSizeText;

    readonly int slotSize = 48;

    public void DropItem()
    {
        if (mySlot != null && mySlot.IsFull() == false)
            return;

        if (mySlot == null && InventoryUI.Instance.DraggedItem() == this && itemData.Item() == null)
            return;

        Unit myUnit = myInventory.MyUnit();
        LooseItem looseItem = LooseItemPool.Instance.GetLooseItemFromPool();
        LooseItem looseProjectile = null;

        SetupItemDrop(looseItem, itemData.Item()); 
        
        if (myUnit.GetRangedWeapon().ItemData() == itemData && myUnit.RangedWeaponEquipped())
        {
            HeldRangedWeapon heldRangedWeapon = myUnit.GetRangedWeapon();
            if (heldRangedWeapon.isLoaded)
            {
                looseProjectile = LooseItemPool.Instance.GetLooseItemFromPool();
                Item projectileItem = heldRangedWeapon.loadedProjectile.ItemData().Item();
                SetupItemDrop(looseProjectile, projectileItem);
                heldRangedWeapon.loadedProjectile.Disable();
            }
        }

        float randomForceMagnitude = Random.Range(100f, 600f);
        float randomAngleRange = Random.Range(-25f, 25f); // Random angle range in degrees

        Vector3 forwardPosition = Vector3.forward;
        Vector3 unitPosition = transform.parent.position;
        Vector3 forceDirection = (unitPosition - forwardPosition).normalized;

        // Add some randomness to the force direction
        Quaternion randomRotation = Quaternion.Euler(0, randomAngleRange, 0);
        forceDirection = randomRotation * forceDirection;

        // Get the Rigidbody component(s) and apply force
        looseItem.RigidBody().AddForce(forceDirection * randomForceMagnitude, ForceMode.Impulse);
        if (looseProjectile != null)
            looseProjectile.RigidBody().AddForce(forceDirection * randomForceMagnitude, ForceMode.Impulse);

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

    void SetupItemDrop(LooseItem looseItem, Item item)
    {
        if (item.pickupMesh != null)
            looseItem.SetupMesh(item.pickupMesh, item.pickupMeshRendererMaterial);
        else if (item.meshes[0] != null)
            looseItem.SetupMesh(item.meshes[0], item.meshRendererMaterials[0]);
        else
            Debug.LogWarning("Mesh info has not been set on the ScriptableObject for: " + item.name);

        // Set the LooseItem's position to match the HeldItem before we add force
        looseItem.transform.position = myInventory.MyUnit().transform.position + new Vector3(Vector3.forward.x / 2f, myInventory.MyUnit().ShoulderHeight(), Vector3.forward.y / 2f);

        // Randomize the rotation
        Vector3 randomRotation = new Vector3(Random.Range(0f, 360f), Random.Range(0f, 360f), Random.Range(0f, 360f));
        transform.rotation = Quaternion.Euler(randomRotation);

        looseItem.gameObject.SetActive(true);
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
        image.sprite = itemData.Item().inventorySprite;
        rectTransform.offsetMin = new Vector2(-slotSize * (itemData.Item().width - 1), 0);
        rectTransform.offsetMax = new Vector2(0, slotSize * (itemData.Item().height - 1));
        image.enabled = true;
    }

    public void SetupDraggedSprite()
    {
        image.sprite = itemData.Item().inventorySprite;
        rectTransform.sizeDelta = new Vector2(slotSize * itemData.Item().width, slotSize * itemData.Item().height);
        image.enabled = true;
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

    public void DisableSprite() => image.enabled = false;

    public void SetMyInventory(Inventory inv) => myInventory = inv;

    public void SetItemData(ItemData newItemData) => itemData = newItemData;

    public RectTransform RectTransform() => rectTransform;
}
