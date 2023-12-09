using UnityEngine;
using UnitSystem;
using UnitSystem.ActionSystem.Actions;

namespace InventorySystem
{
    public class WeaponSetToggle : MonoBehaviour
    {
        public void SwapWeaponSet()
        {
            if (UnitManager.player.IsMyTurn && UnitManager.player.UnitActionHandler.IsPerformingAction == false && UnitManager.player.UnitActionHandler.MoveAction.IsMoving == false)
                UnitManager.player.UnitActionHandler.GetAction<Action_SwapWeaponSet>().QueueAction();
        }
    }
}
