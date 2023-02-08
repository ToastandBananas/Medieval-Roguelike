using UnityEngine;

public class ItemData : MonoBehaviour
{
    public Item item;

    public int damage { get; private set; }
    public float accuracyModifier { get; private set; }

    public bool hasBeenInitialized { get; private set; }

    void Start()
    {
        // TO DO: Remove this from Start
        InitializeData();
    }

    public void InitializeData()
    {
        if (item != null && hasBeenInitialized == false)
        {
            hasBeenInitialized = true;

            if (item.IsWeapon())
            {
                Weapon weapon = item as Weapon;
                damage = Random.Range(weapon.minDamage, weapon.maxDamage + 1);
                accuracyModifier = Random.Range(weapon.minAccuracyModifier, weapon.maxAccuracyModifier);
            }
        }
    }

    public void ClearItemData()
    {
        hasBeenInitialized = false;
        item = null;
    }
}
