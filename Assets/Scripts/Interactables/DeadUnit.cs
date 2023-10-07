using UnityEngine;

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

            if (InventoryUI.Instance.npcInventoryActive == false)
                InventoryUI.Instance.ToggleNPCInventory();
        }
    }

    public override bool CanInteractAtMyGridPosition() => true;
}
