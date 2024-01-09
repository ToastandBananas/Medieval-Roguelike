using UnityEngine;
using UnitSystem;
using GridSystem;
using UnitSystem.ActionSystem.Actions;

namespace InventorySystem
{
    public class HeldShield : HeldItem
    {
        [SerializeField] MeshCollider meshCollider;

        bool shouldKeepBlocking;

        readonly float blockTransitionTime = 0.2f;

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
            if (unit.UnitMeshManager.LeftHeldItem == this)
                Anim.CrossFadeInFixedTime("RaiseShield_L", blockTransitionTime);
            else if (unit.UnitMeshManager.RightHeldItem == this)
                Anim.CrossFadeInFixedTime("RaiseShield_R", blockTransitionTime);
        }

        public void LowerShield()
        {
            // Don't lower the Unit's shield if they should keep blocking (due to Raise Shield Action)
            if (shouldKeepBlocking || !IsBlocking)
                return;

            IsBlocking = false;
            if (unit.UnitMeshManager.LeftHeldItem == this)
                Anim.CrossFadeInFixedTime("LowerShield_L", blockTransitionTime);
            else if (unit.UnitMeshManager.RightHeldItem == this)
                Anim.CrossFadeInFixedTime("LowerShield_R", blockTransitionTime);
        }

        public override void Recoil()
        {
            if (IsBlocking)
            {
                if (unit.UnitMeshManager.LeftHeldItem == this)
                    Anim.Play("BlockRecoil_L");
                else if (unit.UnitMeshManager.RightHeldItem == this)
                    Anim.Play("BlockRecoil_R");
            }
        }

        protected override float GetFumbleChance()
        {
            Item_Shield shield = ItemData.Item as Item_Shield;

            float fumbleChance = (0.5f - (unit.Stats.ShieldSkill.GetValue() / 100f)) * 0.4f; // Shield skill modifier
            float baseFumbleChange = fumbleChance;
            fumbleChance += shield.Weight / unit.Stats.Strength.GetValue() / 100f * 15f; // Shield weight to strength ratio modifier

            // Shield fumble modifier
            fumbleChance += baseFumbleChange * ItemData.FumbleChanceModifier;

            // Gloves fumble modifier
            if (unit.UnitEquipment.EquipSlotHasItem(EquipSlot.Gloves))
                fumbleChance += baseFumbleChange * unit.UnitEquipment.EquippedItemData(EquipSlot.Gloves).FumbleChanceModifier;

            if (fumbleChance < 0f)
                fumbleChance = 0f;

            // Debug.Log(unit.name + " fumble chance: " + fumbleChance);
            return fumbleChance;
        }

        public void SetShouldKeepBlocking(bool shouldKeepBlocking) => this.shouldKeepBlocking = shouldKeepBlocking;

        public MeshCollider MeshCollider => meshCollider;
    }
}
