using GridSystem;
using InventorySystem;
using UnitSystem;
using UnityEngine;

public class OpportunityAttackTrigger : MonoBehaviour
{
    public delegate void EnemyEnterTriggerHandler(Unit enemyUnit, GridPosition enemyGridPosition);
    public event EnemyEnterTriggerHandler OnEnemyEnterTrigger;

    public delegate void EnemyMovedHandler(Unit enemyUnit, GridPosition enemyGridPosition);
    public event EnemyMovedHandler OnEnemyMoved;

    [SerializeField] SphereCollider sphereCollider;
    [SerializeField] Unit myUnit;

    public void UpdateColliderRadius()
    {
        float maxAttackRange = myUnit.stats.UnarmedAttackRange;
        if (myUnit.UnitEquipment.MeleeWeaponEquipped)
        {
            if (myUnit.unitMeshManager.leftHeldItem != null && myUnit.unitMeshManager.leftHeldItem is HeldMeleeWeapon)
                maxAttackRange = myUnit.unitMeshManager.leftHeldItem.ItemData.Item.Weapon.MaxRange;

            if (myUnit.unitMeshManager.rightHeldItem != null && myUnit.unitMeshManager.rightHeldItem is HeldMeleeWeapon && myUnit.unitMeshManager.rightHeldItem.ItemData.Item.Weapon.MaxRange > maxAttackRange)
                maxAttackRange = myUnit.unitMeshManager.rightHeldItem.ItemData.Item.Weapon.MaxRange;
        }
        else if (myUnit.UnitEquipment.RangedWeaponEquipped)
            maxAttackRange = 0.1f;

        sphereCollider.radius = maxAttackRange;
    }

    ///<summary>Used when the enemy moves, but an opportunity attack isn't triggered because they are still within range.</summary>
    public void OnEnemyUnitMoved(Unit enemyUnit, GridPosition enemyGridPosition) => OnEnemyMoved?.Invoke(enemyUnit, enemyGridPosition);

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Unit") || other.CompareTag("Player"))
        {
            if (other.transform == transform.parent)
                return;

            Unit unit = other.gameObject.GetComponent<Unit>();
            unit.unitsWhoCouldOpportunityAttackMe.Add(myUnit);
            OnEnemyEnterTrigger?.Invoke(unit, unit.GridPosition);
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Unit") || other.CompareTag("Player"))
        {
            if (other.transform == transform.parent)
                return;

            other.gameObject.GetComponent<Unit>().unitsWhoCouldOpportunityAttackMe.Remove(myUnit);
        }
    }
}
