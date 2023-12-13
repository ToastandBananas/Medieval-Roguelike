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
            IsBlocking = true;
            if (unit.UnitMeshManager.leftHeldItem == this)
                Anim.CrossFadeInFixedTime("RaiseShield_L", defaultBlockTransitionTime);
            else if (unit.UnitMeshManager.rightHeldItem == this)
                Anim.CrossFadeInFixedTime("RaiseShield_R", defaultBlockTransitionTime);
        }

        public void LowerShield()
        {
            // Don't lower the Unit's shield if they should keep blocking (due to Raise Shield Action)
            if (shouldKeepBlocking || IsBlocking == false)
                return;

            IsBlocking = false;
            if (unit.UnitMeshManager.leftHeldItem == this)
                Anim.Play("LowerShield_L");
            else if (unit.UnitMeshManager.rightHeldItem == this)
                Anim.Play("LowerShield_R");
        }

        public void Recoil()
        {
            if (IsBlocking)
            {
                if (unit.UnitMeshManager.leftHeldItem == this)
                    Anim.Play("BlockRecoil_L");
                else if (unit.UnitMeshManager.rightHeldItem == this)
                    Anim.Play("BlockRecoil_R");
            }
        }

        protected override float GetFumbleChance()
        {
            Item_Shield shield = ItemData.Item as Item_Shield;

            float fumbleChance = (0.5f - (unit.Stats.ShieldSkill.GetValue() / 100f)) * 0.4f; // Shield skill modifier
            fumbleChance += shield.Weight / unit.Stats.Strength.GetValue() / 100f * 15f; // Shield weight to strength ratio modifier

            if (fumbleChance < 0f)
                fumbleChance = 0f;

            // Debug.Log(unit.name + " fumble chance: " + fumbleChance);
            return fumbleChance;
        }

        public void SetShouldKeepBlocking(bool shouldKeepBlocking) => this.shouldKeepBlocking = shouldKeepBlocking;

        public MeshCollider MeshCollider => meshCollider;
    }
}
