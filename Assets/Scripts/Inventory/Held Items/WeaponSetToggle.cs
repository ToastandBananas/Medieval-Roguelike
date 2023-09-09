using UnityEngine;

public class WeaponSetToggle : MonoBehaviour
{
    CharacterEquipment characterEquipment;

    void Start()
    {
        if (transform.parent.name == "Player Equipment")
            characterEquipment = UnitManager.Instance.player.CharacterEquipment();
    }

    public void SwapWeaponSet() => characterEquipment.SwapWeaponSet();
}
