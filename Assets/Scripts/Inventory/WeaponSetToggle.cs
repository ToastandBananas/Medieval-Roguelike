using UnityEngine;
using UnitSystem;
using UnitSystem.ActionSystem;

namespace InventorySystem
{
    public class WeaponSetToggle : MonoBehaviour
    {
        public void SwapWeaponSet()
        {
            if (UnitManager.player.IsMyTurn && UnitManager.player.unitActionHandler.IsPerformingAction == false && UnitManager.player.unitActionHandler.MoveAction.IsMoving == false)
                UnitManager.player.unitActionHandler.GetAction<SwapWeaponSetAction>().QueueAction();
        }
    }
}
