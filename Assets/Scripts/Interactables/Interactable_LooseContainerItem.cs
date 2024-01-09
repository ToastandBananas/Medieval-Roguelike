using UnityEngine;
using InventorySystem;
using UnitSystem;

namespace InteractableObjects
{
    public class Interactable_LooseContainerItem : Interactable_LooseItem
    {
        [Header("Container Info")]
        [SerializeField] InventoryManager_Container containerInventoryManager;

        public override void Awake()
        {
            base.Awake();

            containerInventoryManager.SetLooseItem(this);
        }

        public override void Interact(Unit unitPickingUpItem)
        {
            if (unitPickingUpItem.UnitActionHandler.TurnAction.IsFacingTarget(gridPosition) == false)
                unitPickingUpItem.UnitActionHandler.TurnAction.RotateTowardsPosition(gridPosition.WorldPosition, false, unitPickingUpItem.UnitActionHandler.TurnAction.DefaultRotateSpeed * 2f);

            if (containerInventoryManager.ContainsAnyItems())
                InventoryUI.ShowContainerUI(containerInventoryManager, itemData.Item);
            else
            {
                if (InventoryUI.GetContainerUI(containerInventoryManager) != null)
                    InventoryUI.GetContainerUI(containerInventoryManager).CloseContainerInventory();

                // If the item is Equipment and there's nothing equipped in its EquipSlot, equip it. Else try adding it to the Unit's inventory
                if (TryEquipOnPickup(unitPickingUpItem) || unitPickingUpItem.UnitInventoryManager.TryAddItemToInventories(itemData))
                    Pool_LooseItems.ReturnToPool(this);
                else
                    JiggleItem();
            }
        }

        public InventoryManager_Container ContainerInventoryManager => containerInventoryManager;
    }
}