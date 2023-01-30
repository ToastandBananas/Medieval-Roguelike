using UnityEngine;

public class ItemData : MonoBehaviour
{
    public Item item;
    public int damage { get; private set; }

    void Start()
    {
        // TO DO: Remove this from Start
        InitializeData();
    }

    public void InitializeData()
    {
        if (item != null)
        { 
            if (item.IsWeapon())
            {
                Weapon weapon = item as Weapon;
                damage = Random.Range(weapon.minDamage, weapon.maxDamage + 1);
            }
        }
    }
}
