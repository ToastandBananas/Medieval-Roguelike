using UnityEngine;
using UnitSystem;

namespace InventorySystem
{
    public class HeldShield : HeldItem
    {
        [SerializeField] MeshCollider meshCollider;

        public bool shieldRaised { get; private set; }

        public override void DoDefaultAttack()
        {
            Debug.LogWarning("Default attack for Shields is not created yet.");
        }

        public void RaiseShield()
        {
            if (shieldRaised)
                return;

            shieldRaised = true;
            if (unit.unitMeshManager.leftHeldItem == this)
                anim.Play("RaiseShield_L");
            else if (unit.unitMeshManager.rightHeldItem == this)
                anim.Play("RaiseShield_R");
        }

        public void LowerShield()
        {
            if (shieldRaised == false)
                return;

            shieldRaised = false;
            if (unit.unitMeshManager.leftHeldItem == this)
                anim.Play("LowerShield_L");
            else if (unit.unitMeshManager.rightHeldItem == this)
                anim.Play("LowerShield_R");
        }

        public override void SetupHeldItem(ItemData itemData, Unit unit, EquipSlot equipSlot)
        {
            base.SetupHeldItem(itemData, unit, equipSlot);

            meshCollider.sharedMesh = itemData.Item.Meshes[0];
        }

        public MeshCollider MeshCollider => meshCollider;
    }
}
