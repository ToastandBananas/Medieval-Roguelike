using InventorySystem;
using UnitSystem;
using UnityEngine;

public class OpportunityAttackTrigger : MonoBehaviour
{
    public delegate void EnemyEnterTriggerHandler(Unit enemyUnit);
    public event EnemyEnterTriggerHandler OnEnemyEnterTrigger;

    [SerializeField] SphereCollider sphereCollider;
    [SerializeField] Unit myUnit;

    public void UpdateColliderRadius()
    {
        float maxAttackRange = myUnit.stats.UnarmedAttackRange;
        if (myUnit.UnitEquipment.MeleeWeaponEquipped)
        {
            if (myUnit.unitMeshManager.leftHeldItem != null && myUnit.unitMeshManager.leftHeldItem is HeldMeleeWeapon)
                maxAttackRange = myUnit.unitMeshManager.leftHeldItem.itemData.Item.Weapon.MaxRange;

            if (myUnit.unitMeshManager.rightHeldItem != null && myUnit.unitMeshManager.rightHeldItem is HeldMeleeWeapon && myUnit.unitMeshManager.rightHeldItem.itemData.Item.Weapon.MaxRange > maxAttackRange)
                maxAttackRange = myUnit.unitMeshManager.rightHeldItem.itemData.Item.Weapon.MaxRange;
        }
        else if (myUnit.UnitEquipment.RangedWeaponEquipped)
            maxAttackRange = 0.1f;

        sphereCollider.radius = maxAttackRange;
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Unit") || other.CompareTag("Player"))
        {
            if (other.transform == transform.parent)
                return;

            Unit unit = other.gameObject.GetComponent<Unit>();
            unit.unitsWhoCouldOpportunityAttackMe.Add(myUnit);
            OnEnemyEnterTrigger?.Invoke(unit);
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Unit") || other.CompareTag("Player"))
            other.gameObject.GetComponent<Unit>().unitsWhoCouldOpportunityAttackMe.Remove(myUnit);
    }
}
