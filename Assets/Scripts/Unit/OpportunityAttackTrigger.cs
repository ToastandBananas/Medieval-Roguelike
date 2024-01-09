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
        float maxAttackRange = myUnit.Stats.UnarmedAttackRange;
        if (myUnit.UnitEquipment is UnitEquipment_Humanoid)
        {
            if (myUnit.UnitEquipment.MeleeWeaponEquipped)
            {
                if (myUnit.UnitMeshManager.LeftHeldItem != null && myUnit.UnitMeshManager.LeftHeldItem is HeldMeleeWeapon)
                    maxAttackRange = myUnit.UnitMeshManager.LeftHeldItem.ItemData.Item.Weapon.MaxRange;

                if (myUnit.UnitMeshManager.RightHeldItem != null && myUnit.UnitMeshManager.RightHeldItem is HeldMeleeWeapon && myUnit.UnitMeshManager.RightHeldItem.ItemData.Item.Weapon.MaxRange > maxAttackRange)
                    maxAttackRange = myUnit.UnitMeshManager.RightHeldItem.ItemData.Item.Weapon.MaxRange;
            }
            else if (myUnit.UnitEquipment.RangedWeaponEquipped)
                maxAttackRange = 0.1f;
        }

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
            unit.UnitsWhoCouldOpportunityAttackMe.Add(myUnit);
            OnEnemyEnterTrigger?.Invoke(unit, unit.GridPosition);
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Unit") || other.CompareTag("Player"))
        {
            if (other.transform == transform.parent)
                return;

            other.gameObject.GetComponent<Unit>().UnitsWhoCouldOpportunityAttackMe.Remove(myUnit);
        }
    }
}
