using UnityEngine;

public class EquipmentItem : InventoryItem
{
    public override void DropItem()
    {
        if (mySlot == null)
            Debug.LogError($"mySlot for {name} is not assigned...");

        if (mySlot != null && mySlot.IsFull() == false)
            return;

        Unit myUnit = myCharacterEquipment.MyUnit();
        LooseItem looseItem = LooseItemPool.Instance.GetLooseItemFromPool();
        LooseItem looseProjectile = null;

        Vector3 dropDirection = GetDropDirection(myUnit);

        SetupItemDrop(looseItem, itemData.Item(), dropDirection);

        if (myUnit.unitMeshManager.GetRangedWeapon().ItemData() == itemData && myUnit.CharacterEquipment().RangedWeaponEquipped())
        {
            HeldRangedWeapon heldRangedWeapon = myUnit.unitMeshManager.GetRangedWeapon();
            if (heldRangedWeapon.isLoaded)
            {
                looseProjectile = LooseItemPool.Instance.GetLooseItemFromPool();
                Item projectileItem = heldRangedWeapon.loadedProjectile.ItemData().Item();
                SetupItemDrop(looseProjectile, projectileItem, dropDirection);
                heldRangedWeapon.loadedProjectile.Disable();
            }
        }

        float randomForceMagnitude = Random.Range(50f, 300f);

        // Get the Rigidbody component(s) and apply force
        looseItem.RigidBody().AddForce(dropDirection * randomForceMagnitude, ForceMode.Impulse);
        if (looseProjectile != null)
            looseProjectile.RigidBody().AddForce(dropDirection * randomForceMagnitude, ForceMode.Impulse);

        if (myUnit != UnitManager.Instance.player && UnitManager.Instance.player.vision.IsVisible(myUnit) == false)
        {
            looseItem.HideMeshRenderer();
            if (looseProjectile != null)
                looseProjectile.HideMeshRenderer();
        }

        mySlot.ClearItem();
    }

    public override void SetupItemDrop(LooseItem looseItem, Item item, Vector3 dropDirection)
    {
        if (item.pickupMesh != null)
            looseItem.SetupMesh(item.pickupMesh, item.pickupMeshRendererMaterial);
        else if (item.meshes[0] != null)
            looseItem.SetupMesh(item.meshes[0], item.meshRendererMaterials[0]);
        else
            Debug.LogWarning("Mesh info has not been set on the ScriptableObject for: " + item.name);

        // Set the LooseItem's position to be slightly in front of the Unit dropping the item
        looseItem.transform.position = myInventory.MyUnit().transform.position + new Vector3(0, myInventory.MyUnit().ShoulderHeight(), 0) + (dropDirection / 2);

        // Randomize the rotation and set active
        looseItem.transform.rotation = Quaternion.Euler(new Vector3(Random.Range(0f, 360f), Random.Range(0f, 360f), Random.Range(0f, 360f)));
        looseItem.gameObject.SetActive(true);
    }
}
