using UnityEngine;
using UnitSystem;

namespace InventorySystem
{
    public class WeaponSetToggle : MonoBehaviour
    {
        CharacterEquipment characterEquipment;

        void Start()
        {
            if (transform.parent.name == "Player Equipment")
                characterEquipment = UnitManager.player.CharacterEquipment;
        }

        public void SwapWeaponSet() => characterEquipment.SwapWeaponSet();
    }
}
