using UnityEngine;
using UnitSystem;
using ActionSystem;

namespace InventorySystem
{
    public class WeaponSetToggle : MonoBehaviour
    {
        public void SwapWeaponSet()
        {
            if (UnitManager.player.isMyTurn && UnitManager.player.unitActionHandler.isPerformingAction == false && UnitManager.player.unitActionHandler.isMoving == false)
                UnitManager.player.unitActionHandler.GetAction<SwapWeaponSetAction>().QueueAction();
        }
    }
}
