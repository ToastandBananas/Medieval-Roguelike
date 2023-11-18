using UnityEngine;
using UnitSystem;
using GridSystem;

namespace InventorySystem
{
    public class HeldShield : HeldItem
    {
        [SerializeField] MeshCollider meshCollider;

        bool shouldKeepBlocking;

        readonly float defaultBlockTransitionTime = 0.2f;

        public override void SetupHeldItem(ItemData itemData, Unit unit, EquipSlot equipSlot)
        {
            base.SetupHeldItem(itemData, unit, equipSlot);

            meshCollider.sharedMesh = itemData.Item.HeldEquipment.Meshes[0];
        }

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
            isBlocking = true;
            if (unit.unitMeshManager.leftHeldItem == this)
                anim.CrossFadeInFixedTime("RaiseShield_L", defaultBlockTransitionTime);
            else if (unit.unitMeshManager.rightHeldItem == this)
                anim.CrossFadeInFixedTime("RaiseShield_R", defaultBlockTransitionTime);
        }

        public void LowerShield()
        {
            // Don't lower the Unit's shield if they should keep blocking (due to Raise Shield Action)
            if (shouldKeepBlocking || isBlocking == false)
                return;

            isBlocking = false;
            if (unit.unitMeshManager.leftHeldItem == this)
                anim.Play("LowerShield_L");
            else if (unit.unitMeshManager.rightHeldItem == this)
                anim.Play("LowerShield_R");
        }

        public void Recoil()
        {
            if (isBlocking)
            {
                if (unit.unitMeshManager.leftHeldItem == this)
                    anim.Play("BlockRecoil_L");
                else if (unit.unitMeshManager.rightHeldItem == this)
                    anim.Play("BlockRecoil_R");
            }
        }

        protected override float GetFumbleChance()
        {
            Shield shield = itemData.Item as Shield;

            float fumbleChance = (0.5f - (unit.stats.ShieldSkill.GetValue() / 100f)) * 0.4f; // Shield skill modifier
            fumbleChance += shield.Weight / unit.stats.Strength.GetValue() / 100f * 15f; // Shield weight to strength ratio modifier

            if (fumbleChance < 0f)
                fumbleChance = 0f;

            // Debug.Log(unit.name + " fumble chance: " + fumbleChance);
            return fumbleChance;
        }

        public void SetShouldKeepBlocking(bool shouldKeepBlocking) => this.shouldKeepBlocking = shouldKeepBlocking;

        public MeshCollider MeshCollider => meshCollider;
    }
}
