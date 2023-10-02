using UnityEngine;

public class LooseContainerItem : LooseItem
{
    [Header("Container Info")]
    [SerializeField] ContainerInventoryManager containerInventoryManager;

    public override void Interact(Unit unitPickingUpItem)
    {
        if (containerInventoryManager.ContainsAnyItems())
            InventoryUI.Instance.ShowContainerUI(containerInventoryManager, itemData.Item);
        else
        {
            if (InventoryUI.Instance.GetContainerUI(containerInventoryManager) != null)
                InventoryUI.Instance.GetContainerUI(containerInventoryManager).CloseContainerInventory();

            // If the item is Equipment and there's nothing equipped in its EquipSlot, equip it. Else try adding it to the Unit's inventory
            if (TryEquipOnPickup(unitPickingUpItem) || unitPickingUpItem.TryAddItemToInventories(itemData))
                LooseItemPool.ReturnToPool(this);
            else
                FumbleItem();
        }
    }

    public ContainerInventoryManager ContainerInventoryManager => containerInventoryManager;
}
