using InventorySystem;
using UnitSystem;

namespace InteractableObjects
{
    public class DeadUnit : Interactable
    {
        Unit myUnit;

        public override void Awake()
        {
            base.Awake();

            myUnit = GetComponent<Unit>();
        }

        public override void Interact(Unit unitInteracting)
        {
            if (myUnit.CharacterEquipment.slotVisualsCreated == false)
            {
                myUnit.CharacterEquipment.CreateSlotVisuals();
                myUnit.MainInventoryManager.MainInventory.CreateSlotVisuals();

                if (InventoryUI.npcInventoryActive == false)
                    InventoryUI.ToggleNPCInventory();

                if (InventoryUI.playerInventoryActive == false)
                    InventoryUI.TogglePlayerInventory();
            }
        }

        public override bool CanInteractAtMyGridPosition() => true;
    }
}