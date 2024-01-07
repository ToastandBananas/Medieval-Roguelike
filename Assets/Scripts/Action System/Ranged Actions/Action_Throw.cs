using GridSystem;
using InventorySystem;
using System.Collections;
using System.Collections.Generic;
using UnitSystem.ActionSystem.UI;
using UnityEngine;
using ContextMenu = GeneralUI.ContextMenu;

namespace UnitSystem.ActionSystem.Actions
{
    public class Action_Throw : Action_BaseRangedAttack
    {
        public ItemData ItemDataToThrow { get; private set; }
        public List<ItemData> Throwables { get; private set; }
        HeldItem hiddenHeldItem, thrownHeldItem;

        public static readonly ItemType[] throwingWeaponItemTypes = new ItemType[] { ItemType.ThrowingDagger, ItemType.ThrowingAxe, ItemType.ThrowingClub, ItemType.ThrowingStar };

        static readonly float minThrowDistance = 2f;
        public static readonly float maxThrowDistance = 6f;
        public static readonly float minMaxThrowDistance = 2.85f;

        public static readonly float brokenItemDamageModifier = 0.1f;

        readonly float defaultThrowEffectivenessAgainstArmor = 1f;
        readonly float defaultThrowArmorPierce = 0.33f;

        void Awake()
        {
            Throwables = new();
        }

        public override void OnActionSelected()
        {
            Throwables.Clear();
            ItemDataToThrow = null;

            HeldMeleeWeapon leftMeleeWeapon = Unit.UnitMeshManager.GetLeftHeldMeleeWeapon();
            if (leftMeleeWeapon != null)
                Throwables.Add(leftMeleeWeapon.ItemData);

            HeldMeleeWeapon rightMeleeWeapon = Unit.UnitMeshManager.GetRightHeldMeleeWeapon();
            if (rightMeleeWeapon != null)
                Throwables.Add(rightMeleeWeapon.ItemData);

            if (Unit.UnitEquipment.HumanoidEquipment.BeltBagEquipped)
            {
                ContainerInventoryManager beltInventoryManager = Unit.BeltInventoryManager;
                if (beltInventoryManager.ParentInventory.AllowedItemTypeContains(throwingWeaponItemTypes))
                    Throwables.AddRange(beltInventoryManager.ParentInventory.ItemDatas);

                for (int subInvIndex = 0; subInvIndex < beltInventoryManager.SubInventories.Length; subInvIndex++)
                {
                    if (beltInventoryManager.SubInventories[subInvIndex].AllowedItemTypeContains(throwingWeaponItemTypes))
                        Throwables.AddRange(beltInventoryManager.SubInventories[subInvIndex].ItemDatas);
                }
            }

            if (Throwables.Count > 1)
            {
                Unit.UnitActionHandler.PlayerActionHandler.SetDefaultSelectedAction();
                ContextMenu.BuildThrowActionContextMenu();
            }
            else if (Throwables.Count == 1)
            {
                ItemDataToThrow = Throwables[0];
                GridSystemVisual.UpdateAttackRangeGridVisual();
            }
            else
                Unit.UnitActionHandler.PlayerActionHandler.SetDefaultSelectedAction();
        }

        public override int ActionPointsCost()
        {
            return 200;
        }

        public override void TakeAction()
        {
            if (ItemDataToThrow == null || !IsInAttackRange(null, Unit.GridPosition, TargetGridPosition))
            {
                CompleteAction();
                TurnManager.Instance.StartNextUnitsTurn(Unit);
                return;
            }

            StartAction();
            Unit.StartCoroutine(DoAttack());
        }

        protected override IEnumerator DoAttack()
        {
            if (TargetEnemyUnit != null)
            {
                while (TargetEnemyUnit.UnitActionHandler.MoveAction.IsMoving || TargetEnemyUnit.UnitAnimator.beingKnockedBack)
                    yield return null;

                // If the target Unit moved out of range, queue a movement instead
                if (!IsInAttackRange(TargetEnemyUnit, Unit.GridPosition, TargetEnemyUnit.GridPosition))
                {
                    MoveToTargetInstead();
                    yield break;
                }
            }

            if (Unit.IsPlayer || TargetEnemyUnit.IsPlayer || Unit.UnitMeshManager.IsVisibleOnScreen || TargetEnemyUnit.UnitMeshManager.IsVisibleOnScreen)
            {
                // Rotate towards the target
                if (!Unit.UnitActionHandler.TurnAction.IsFacingTarget(TargetEnemyUnit.GridPosition))
                    Unit.UnitActionHandler.TurnAction.RotateTowards_Unit(TargetEnemyUnit, false);

                // Wait to finish any rotations already in progress
                while (Unit.UnitActionHandler.TurnAction.isRotating)
                    yield return null;

                // If the target Unit moved out of range, queue a movement instead
                if (TargetEnemyUnit != null && !IsInAttackRange(TargetEnemyUnit, Unit.GridPosition, TargetGridPosition))
                {
                    MoveToTargetInstead();
                    yield break;
                }

                // The targetUnit tries to block and if they're successful, the weapon/shield they blocked with is added as a corresponding Value in the attacking Unit's targetUnits dictionary
                if (TargetEnemyUnit != null && TargetEnemyUnit.UnitActionHandler.TryBlockRangedAttack(Unit, null, this, false))
                {
                    // Target Unit rotates towards this Unit & does block animation, moving shield in path of Projectile
                    TargetEnemyUnit.UnitActionHandler.TurnAction.RotateTowards_Unit(Unit, false);
                    Unit.UnitActionHandler.TargetUnits.TryGetValue(TargetEnemyUnit, out HeldItem itemBlockedWith);
                    if (itemBlockedWith != null)
                        itemBlockedWith.BlockAttack(Unit);
                }

                // Rotate towards the target and do the throw animation
                PlayAttackAnimation();
            }
            else
            {
                // Rotate towards the target
                if (TargetEnemyUnit != null && !Unit.UnitActionHandler.TurnAction.IsFacingTarget(TargetEnemyUnit.GridPosition))
                    Unit.UnitActionHandler.TurnAction.RotateTowards_Unit(TargetEnemyUnit, true);

                bool hitTarget = TryHitTarget(TargetGridPosition);

                bool attackBlocked = false;
                if (TargetEnemyUnit != null)
                    attackBlocked = TargetEnemyUnit.UnitActionHandler.TryBlockRangedAttack(Unit, null, this, false);

                if (hitTarget)
                    DamageTargets(null, ItemDataToThrow);

                // If the attack was blocked and the unit isn't facing their attacker, turn to face the attacker
                if (attackBlocked)
                    TargetEnemyUnit.UnitActionHandler.TurnAction.RotateTowards_Unit(Unit, true);

                Unit.UnitActionHandler.SetIsAttacking(false);
            }

            while (Unit.UnitActionHandler.IsAttacking)
                yield return null;

            CompleteAction();
            TurnManager.Instance.StartNextUnitsTurn(Unit); // This must remain outside of CompleteAction in case we need to call CompleteAction early within MoveToTargetInstead
        }

        protected override float GetBaseDamage(ItemData heldItemAttackingWith, ItemData itemHittingWith)
        {
            float damage;
            if (itemHittingWith.Item is Item_MeleeWeapon)
                damage = itemHittingWith.Damage * ((Unit.Stats.Strength.GetValue() * 0.015f) + (Unit.Stats.ThrowingSkill.GetValue() * 0.015f)); // Weight * (1.5% of strength + 1.5% throwing skill) (100 in each skill will cause triple the weapon's damage)
            else
                damage = itemHittingWith.Item.Weight * ((Unit.Stats.Strength.GetValue() * 0.025f) + (Unit.Stats.ThrowingSkill.GetValue() * 0.025f)); // Weight * (2.5% of strength + 2.5% throwing skill) (100 in each skill will cause 5 times the item's weight in damage)
            damage += damage * itemHittingWith.ThrowingDamageMultiplier;

            if (itemHittingWith.IsBroken) damage *= brokenItemDamageModifier;
            return damage;
        }

        protected override float GetEffectivenessAgainstArmor(HeldItem heldItemAttackingWith, ItemData itemHittingWith)
        {
            if (itemHittingWith != null && itemHittingWith.EffectivenessAgainstArmor > 0f)
                return itemHittingWith.EffectivenessAgainstArmor;
            return defaultThrowEffectivenessAgainstArmor * EffectivenessAgainstArmorModifier();
        }

        protected override float GetArmorPierce(HeldItem heldItemAttackingWith, ItemData itemHittingWith)
        {
            float armorPierce;
            if (itemHittingWith != null && itemHittingWith.ArmorPierce > 0f)
                armorPierce = itemHittingWith.ArmorPierce * ArmorPierceModifier();
            else
                armorPierce = defaultThrowArmorPierce * ArmorPierceModifier();
            return Mathf.Clamp01(armorPierce);
        }

        public override bool TryHitTarget(GridPosition targetGridPosition)
        {
            float random = Random.Range(0f, 1f);
            float rangedAccuracy = Unit.Stats.ThrowingAccuracy(ItemDataToThrow, targetGridPosition, this);
            if (random <= rangedAccuracy)
                return true;
            return false;
        }

        public override void PlayAttackAnimation()
        {
            Unit.UnitActionHandler.TurnAction.RotateTowardsAttackPosition(TargetEnemyUnit.WorldPosition);
            if (Unit.UnitMeshManager.rightHeldItem != null && ItemDataToThrow == Unit.UnitMeshManager.rightHeldItem.ItemData)
                Unit.UnitMeshManager.rightHeldItem.StartThrow();
            else if (Unit.UnitMeshManager.leftHeldItem != null && ItemDataToThrow == Unit.UnitMeshManager.leftHeldItem.ItemData)
                Unit.UnitMeshManager.leftHeldItem.StartThrow();
            else if (ItemDataToThrow.MyInventory != null) // If throwing an item from an inventory
            {
                if (Unit.UnitMeshManager.leftHeldItem != null && Unit.UnitMeshManager.rightHeldItem != null)
                {
                    Unit.UnitMeshManager.rightHeldItem.HideMeshes();
                    hiddenHeldItem = Unit.UnitMeshManager.rightHeldItem;

                    SetupAndThrowItem(Unit.UnitMeshManager.RightHeldItemParent);
                }
                else if (Unit.UnitMeshManager.leftHeldItem == null)
                    SetupAndThrowItem(Unit.UnitMeshManager.LeftHeldItemParent);
                else
                    SetupAndThrowItem(Unit.UnitMeshManager.RightHeldItemParent);
            }
            else
                Debug.LogWarning($"The Item {Unit.name} is trying to throw isn't a held item, nor is it inside an inventory...");
        }
        
        void SetupAndThrowItem(Transform heldItemParent)
        {
            HeldMeleeWeapon heldMeleeWeapon = Pool_HeldItemBase.Instance.GetMeleeWeaponBaseFromPool();
            heldMeleeWeapon.SetupItemToThrow(ItemDataToThrow, Unit, heldItemParent);
            thrownHeldItem = heldMeleeWeapon;
            heldMeleeWeapon.StartThrow();
        }

        void ShowHiddenHeldItem()
        {
            if (hiddenHeldItem != null && (Unit.IsPlayer || Unit.UnitMeshManager.IsVisibleOnScreen))
                hiddenHeldItem.ShowMeshes();
            hiddenHeldItem = null;
        }

        public void OnThrowHeldItem()
        {
            if (Unit.UnitEquipment.ItemDataEquipped(ItemDataToThrow))
                Unit.UnitEquipment.RemoveEquipment(ItemDataToThrow);
            else if (ItemDataToThrow.MyInventory != null)
            {
                // If this item is not coming from a belt bag with special throwing item slots, queue an Inventory Action (for removing the item from the inventory before throwing it)
                if (!Unit.UnitEquipment.HumanoidEquipment.BeltBagEquipped || !Unit.BeltInventoryManager.ContainsItemData(ItemDataToThrow) || !Unit.BeltInventoryManager.ParentInventory.AllowedItemTypeContains(throwingWeaponItemTypes))
                    Unit.UnitActionHandler.GetAction<Action_Inventory>().QueueAction(ItemDataToThrow, 1, ItemDataToThrow.MyInventory is ContainerInventory ? ItemDataToThrow.MyInventory.ContainerInventory.containerInventoryManager : null);

                if (ItemDataToThrow.CurrentStackSize > 1)
                    ItemDataToThrow.AdjustCurrentStackSize(-1);
                else
                    ItemDataToThrow.MyInventory.RemoveItem(ItemDataToThrow);

                Pool_HeldItemBase.ReturnToPool(thrownHeldItem);
            }

            thrownHeldItem = null;
            ItemDataToThrow = null;
        }

        public override void CompleteAction()
        {
            // StartNextUnitsTurn will be called when the thrown item hits something, rather than in this method as is with non-ranged actions
            base.CompleteAction();
            if (Unit.IsPlayer)
            {
                TargetEnemyUnit = null;
                Unit.UnitActionHandler.SetTargetEnemyUnit(null);
            }

            ShowHiddenHeldItem();
            Throwables.Clear();
            Unit.UnitActionHandler.FinishAction();
        }

        public override bool IsValidAction() => true;

        public override bool CanShowAttackRange() => ItemDataToThrow != null;

        public override int EnergyCost()
        {
            return 10;
        }

        public override string ActionName()
        {
            if (Unit.UnitEquipment.MeleeWeaponEquipped && !Unit.UnitEquipment.IsDualWielding && (!Unit.UnitEquipment.HumanoidEquipment.BeltBagEquipped || !Unit.BeltInventoryManager.AllowedItemTypeContains(throwingWeaponItemTypes) || !Unit.BeltInventoryManager.ContainsAnyItems()))
                return $"Throw {Unit.UnitMeshManager.GetPrimaryHeldMeleeWeapon().ItemData.Item.Name}";
            return "Throw Item";
        }

        public override string TooltipDescription()
        {
            return "<b>Throw</b> a <b>held weapon</b>, a <b>throwing weapon</b> from your <b>belt</b>, or an <b>item</b> from your <b>inventory</b>. (Throwing an inventory item comes with an added <b>AP cost</b>. To throw an item from your inventory, choose the option from the item's context menu.)";
        }

        public static bool IsThrowingWeapon(ItemType itemType)
        {
            for (int i = 0; i < throwingWeaponItemTypes.Length; i++)
            {
                if (throwingWeaponItemTypes[i] == itemType)
                    return true;
            }
            return false;
        }

        public void SetItemToThrow(ItemData itemDataToThrow) => ItemDataToThrow = itemDataToThrow;

        public override float AccuracyModifier() => 1f;

        public override bool IsInterruptable() => false;

        public override bool CanQueueMultiple() => false;

        public override bool CanBeClearedFromActionQueue() => true;

        public override bool ActionIsUsedInstantly() => false;

        public override bool IsMeleeAttackAction() => false;

        public override bool IsRangedAttackAction() => true;

        public override ActionBarSection ActionBarSection() => UI.ActionBarSection.Basic;

        public override float MinAttackRange() => minThrowDistance;

        public override float MaxAttackRange() => ItemDataToThrow != null ? Unit.Stats.MaxThrowRange(ItemDataToThrow.Item) : minThrowDistance;

        public override bool CanAttackThroughUnits() => false;
    }
}
