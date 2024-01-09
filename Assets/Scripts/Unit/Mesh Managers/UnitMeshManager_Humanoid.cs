using InventorySystem;
using UnityEngine;

namespace UnitSystem
{
    public class UnitMeshManager_Humanoid : UnitMeshManager
    {
        [Header("Other Mesh Renderers")]
        [SerializeField] MeshRenderer hairMeshRenderer;
        [SerializeField] MeshRenderer helmMeshRenderer, shirtMeshRenderer, bodyArmorMeshRenderer;

        [Header("Other Mesh Filters")]
        [SerializeField] MeshFilter hairMeshFilter;
        [SerializeField] MeshFilter helmMeshFilter, shirtMeshFilter, bodyArmorMeshFilter;

        [Header("Parent Transforms")]
        [SerializeField] Transform leftHeldItemParent;
        [SerializeField] Transform rightHeldItemParent;

        HeldItem leftHeldItem, rightHeldItem;

        protected override void Awake()
        {
            base.Awake();

            if (hairMeshRenderer != null)
                meshRenderers.Add(hairMeshRenderer);
            if (helmMeshRenderer != null)
                meshRenderers.Add(helmMeshRenderer);
            if (shirtMeshRenderer != null)
                meshRenderers.Add(shirtMeshRenderer);
            if (bodyArmorMeshRenderer != null)
                meshRenderers.Add(bodyArmorMeshRenderer);
        }

        public override void ShowMeshRenderers()
        {
            base.ShowMeshRenderers();

            if (leftHeldItem != null)
                leftHeldItem.ShowMeshes();

            if (rightHeldItem != null)
                rightHeldItem.ShowMeshes();
        }

        public override void HideMeshRenderers()
        {
            base.HideMeshRenderers();

            if (leftHeldItem != null)
                leftHeldItem.HideMeshes();

            if (rightHeldItem != null)
                rightHeldItem.HideMeshes();
        }

        public override HeldItem LeftHeldItem => leftHeldItem;
        public override HeldItem RightHeldItem => rightHeldItem;

        public void SetLeftHeldItem(HeldItem heldItem) => leftHeldItem = heldItem;
        public void SetRightHeldItem(HeldItem heldItem) => rightHeldItem = heldItem;

        public override HeldMeleeWeapon GetPrimaryHeldMeleeWeapon()
        {
            if (rightHeldItem != null && rightHeldItem.ItemData.Item is Item_MeleeWeapon)
                return rightHeldItem as HeldMeleeWeapon;
            else if (leftHeldItem != null && leftHeldItem.ItemData.Item is Item_MeleeWeapon)
                return leftHeldItem as HeldMeleeWeapon;
            return null;
        }

        public override HeldItem GetHeldItemFromItemData(ItemData itemData)
        {
            if (rightHeldItem != null && rightHeldItem.ItemData == itemData)
                return rightHeldItem;

            if (leftHeldItem != null && leftHeldItem.ItemData == itemData)
                return leftHeldItem;
            return null;
        }

        public override HeldRangedWeapon GetHeldRangedWeapon()
        {
            if (rightHeldItem != null && rightHeldItem.ItemData.Item is Item_RangedWeapon)
                return rightHeldItem as HeldRangedWeapon;
            return null;
        }

        public override HeldMeleeWeapon GetLeftHeldMeleeWeapon()
        {
            if (leftHeldItem != null && leftHeldItem.ItemData.Item is Item_MeleeWeapon)
                return leftHeldItem as HeldMeleeWeapon;
            return null;
        }

        public override HeldMeleeWeapon GetRightHeldMeleeWeapon()
        {
            if (rightHeldItem != null && rightHeldItem.ItemData.Item is Item_MeleeWeapon)
                return rightHeldItem as HeldMeleeWeapon;
            return null;
        }

        public override HeldShield GetHeldShield()
        {
            if (leftHeldItem != null && leftHeldItem.ItemData.Item is Item_Shield)
                return leftHeldItem as HeldShield;
            else if (rightHeldItem != null && rightHeldItem.ItemData.Item is Item_Shield)
                return rightHeldItem as HeldShield;
            return null;
        }

        public override void SetupWearableMesh(EquipSlot equipSlot, Item_VisibleArmor wearable)
        {
            if (wearable == null)
                return;

            switch (equipSlot)
            {
                case EquipSlot.Helm:
                    AssignMeshAndMaterials(helmMeshFilter, helmMeshRenderer, wearable);
                    break;
                case EquipSlot.BodyArmor:
                    AssignMeshAndMaterials(bodyArmorMeshFilter, bodyArmorMeshRenderer, wearable);
                    break;
                case EquipSlot.Shirt:
                    AssignMeshAndMaterials(shirtMeshFilter, shirtMeshRenderer, wearable);
                    break;
                default:
                    break;
            }

            if (myUnit.IsNPC && !IsVisibleOnScreen)
                HideMesh(equipSlot);
        }

        public override void HideMesh(EquipSlot equipSlot)
        {
            switch (equipSlot)
            {
                case EquipSlot.Helm:
                    helmMeshRenderer.enabled = false;
                    break;
                case EquipSlot.BodyArmor:
                    bodyArmorMeshRenderer.enabled = false;
                    break;
                case EquipSlot.Shirt:
                    shirtMeshRenderer.enabled = false;
                    break;
            }
        }

        public override void RemoveMesh(EquipSlot equipSlot)
        {
            switch (equipSlot)
            {
                case EquipSlot.Helm:
                    helmMeshRenderer.material = null;
                    helmMeshFilter.mesh = null;
                    break;
                case EquipSlot.BodyArmor:
                    bodyArmorMeshRenderer.material = null;
                    bodyArmorMeshFilter.mesh = null;
                    break;
                case EquipSlot.Shirt:
                    shirtMeshRenderer.material = null;
                    shirtMeshFilter.mesh = null;
                    break;
            }
        }

        public void ReturnHeldItemToPool(EquipSlot equipSlot)
        {
            if (equipSlot != EquipSlot.LeftHeldItem1 && equipSlot != EquipSlot.RightHeldItem1 && equipSlot != EquipSlot.LeftHeldItem2 && equipSlot != EquipSlot.RightHeldItem2)
                return;

            if (!myUnit.UnitEquipment.EquipSlotHasItem(equipSlot))
                return;

            if (equipSlot == EquipSlot.LeftHeldItem1 || equipSlot == EquipSlot.LeftHeldItem2)
            {
                if (leftHeldItem != null && leftHeldItem.ItemData == myUnit.UnitEquipment.EquippedItemData(equipSlot))
                {
                    leftHeldItem.ResetHeldItem();
                    leftHeldItem = null;
                }
                else if (rightHeldItem != null && rightHeldItem.ItemData == myUnit.UnitEquipment.EquippedItemData(equipSlot))
                {
                    rightHeldItem.ResetHeldItem();
                    rightHeldItem = null;
                }
            }
            else if (RightHeldItem != null)
            {
                rightHeldItem.ResetHeldItem();
                rightHeldItem = null;
            }
        }

        public Transform LeftHeldItemParent => leftHeldItemParent;
        public Transform RightHeldItemParent => rightHeldItemParent;

        public MeshRenderer HelmMeshRenderer => helmMeshRenderer;
    }
}
