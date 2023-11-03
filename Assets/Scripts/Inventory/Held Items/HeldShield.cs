using UnityEngine;
using UnitSystem;
using GridSystem;

namespace InventorySystem
{
    public class HeldShield : HeldItem
    {
        [SerializeField] MeshCollider meshCollider;

        public override void DoDefaultAttack(GridPosition targetGridPosition)
        {
            Debug.LogWarning("Default attack for Shields is not created yet.");
        }

        public override void BlockAttack(Unit attackingUnit)
        {
            base.BlockAttack(attackingUnit);
            RaiseShield();
        }

        public override void StopBlocking() => LowerShield();

        public void RaiseShield()
        {
            if (isBlocking)
                return;

            isBlocking = true;
            if (unit.unitMeshManager.leftHeldItem == this)
                anim.Play("RaiseShield_L");
            else if (unit.unitMeshManager.rightHeldItem == this)
                anim.Play("RaiseShield_R");
        }

        public void LowerShield()
        {
            if (isBlocking == false)
                return;

            isBlocking = false;
            if (unit.unitMeshManager.leftHeldItem == this)
                anim.Play("LowerShield_L");
            else if (unit.unitMeshManager.rightHeldItem == this)
                anim.Play("LowerShield_R");
        }

        public override void SetupHeldItem(ItemData itemData, Unit unit, EquipSlot equipSlot)
        {
            base.SetupHeldItem(itemData, unit, equipSlot);

            meshCollider.sharedMesh = itemData.Item.HeldEquipment.Meshes[0];
        }

        public MeshCollider MeshCollider => meshCollider;
    }
}
