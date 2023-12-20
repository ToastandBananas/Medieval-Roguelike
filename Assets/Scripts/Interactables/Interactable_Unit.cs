using GridSystem;
using InventorySystem;
using UnitSystem;

namespace InteractableObjects
{
    public class Interactable_Unit : Interactable
    {
        Unit myUnit;

        public override void Awake()
        {
            base.Awake();

            myUnit = GetComponent<Unit>();
        }

        public override void Interact(Unit unitInteracting)
        {
            if (myUnit.UnitEquipment.SlotVisualsCreated == false)
            {
                InventoryUI.ClearNPCInventorySlots();

                myUnit.UnitEquipment.CreateSlotVisuals();
                myUnit.UnitInventoryManager.MainInventory.CreateSlotVisuals();

                if (InventoryUI.NpcInventoryActive == false)
                    InventoryUI.ToggleNPCInventory();
            }
        }

        public override void UpdateGridPosition()
        {
            gridPosition.Set(transform.position);
        }

        public override GridPosition GridPosition()
        {
            UpdateGridPosition();
            myUnit.UpdateGridPosition();
            return base.GridPosition();
        }

        public override bool CanInteractAtMyGridPosition() => true;
    }
}