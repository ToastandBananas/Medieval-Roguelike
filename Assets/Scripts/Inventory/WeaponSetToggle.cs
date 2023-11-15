using UnityEngine;
using UnitSystem;
using UnitSystem.ActionSystem;

namespace InventorySystem
{
    public class WeaponSetToggle : MonoBehaviour
    {
        public void SwapWeaponSet()
        {
            if (UnitManager.player.IsMyTurn && UnitManager.player.unitActionHandler.isPerformingAction == false && UnitManager.player.unitActionHandler.moveAction.isMoving == false)
                UnitManager.player.unitActionHandler.GetAction<SwapWeaponSetAction>().QueueAction();
        }
    }
}
