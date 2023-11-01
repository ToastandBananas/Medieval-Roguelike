using UnityEngine;
using InventorySystem;
using UnitSystem;

namespace InteractableObjects
{
    public class LooseContainerItem : LooseItem
    {
        [Header("Container Info")]
        [SerializeField] ContainerInventoryManager containerInventoryManager;

        public override void Awake()
        {
            base.Awake();

            containerInventoryManager.SetLooseItem(this);
        }

        public override void Interact(Unit unitPickingUpItem)
        {
            if (UnitManager.player.unitActionHandler.turnAction.IsFacingTarget(gridPosition) == false)
                UnitManager.player.unitActionHandler.turnAction.RotateTowardsPosition(gridPosition.WorldPosition, false, UnitManager.player.unitActionHandler.turnAction.DefaultRotateSpeed * 2f);

            if (containerInventoryManager.ContainsAnyItems())
                InventoryUI.ShowContainerUI(containerInventoryManager, itemData.Item);
            else
            {
                if (InventoryUI.GetContainerUI(containerInventoryManager) != null)
                    InventoryUI.GetContainerUI(containerInventoryManager).CloseContainerInventory();

                // If the item is Equipment and there's nothing equipped in its EquipSlot, equip it. Else try adding it to the Unit's inventory
                if (TryEquipOnPickup(unitPickingUpItem) || unitPickingUpItem.UnitInventoryManager.TryAddItemToInventories(itemData))
                    LooseItemPool.ReturnToPool(this);
                else
                    FumbleItem();
            }
        }

        public ContainerInventoryManager ContainerInventoryManager => containerInventoryManager;
    }
}