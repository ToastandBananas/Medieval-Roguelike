using UnityEngine;
using UnityEngine.EventSystems;
using InteractableObjects;
using GridSystem;
using InventorySystem;
using UnitSystem.ActionSystem.Actions;
using UnitSystem.ActionSystem.UI;
using Controls;
using GeneralUI;
using ContextMenu = GeneralUI.ContextMenu;

namespace UnitSystem.ActionSystem
{
    public class PlayerInput : MonoBehaviour
    {
        public static PlayerInput Instance;

        [SerializeField] LayerMask interactableMask;
        [SerializeField] LayerMask unitMask;

        public Interactable HighlightedInteractable { get; private set; }
        public Unit HighlightedUnit { get; private set; }
        public bool AutoAttack { get; private set; }

        GridPosition mouseGridPosition;
        GridPosition lastMouseGridPosition;

        Unit player;

        readonly float skipTurnCooldown = 0.1f;
        float skipTurnCooldownTimer;

        void Awake()
        {
            if (Instance != null)
            {
                if (Instance != this)
                {
                    Debug.LogWarning("More than one Instance of PlayerActionInput. Fix me!");
                    Destroy(gameObject);
                }
            }
            else
                Instance = this;
        }

        void Start()
        {
            player = GetComponent<Unit>();
            GridSystemVisual.UpdateAttackGridVisual();
        }

        void Update()
        {
            if (skipTurnCooldownTimer < skipTurnCooldown)
                skipTurnCooldownTimer += Time.deltaTime;

            if (InventoryUI.isDraggingItem || ActionSystemUI.IsDraggingAction)
                return;

            if (EventSystem.current.IsPointerOverGameObject())
            {
                ActionLineRenderer.Instance.HideLineRenderers();
                WorldMouse.ChangeCursor(CursorState.Default);
                ClearHighlightedInteractable();

                if (GameControls.gamePlayActions.menuContext.WasPressed && !player.UnitActionHandler.PlayerActionHandler.DefaultActionIsSelected)
                    player.UnitActionHandler.PlayerActionHandler.SetDefaultSelectedAction();

                return;
            }
            
            if (!player.Health.IsDead)
            {
                // If the Player was holding the button for turn mode (the Turn Action) and then they release it
                if (GameControls.gamePlayActions.turnMode.WasReleased && player.UnitActionHandler.PlayerActionHandler.SelectedActionType.GetAction(player) is TurnAction)
                {
                    // Reset the line renderer and go back to the Move Action
                    ActionLineRenderer.ResetCurrentPositions();
                    player.UnitActionHandler.PlayerActionHandler.SetDefaultSelectedAction();
                }

                // If the Player is trying to cancel their current action
                if (GameControls.gamePlayActions.cancelAction.WasPressed)
                {
                    player.UnitActionHandler.CancelActions();
                    ActionLineRenderer.ResetCurrentPositions();
                }
                // If the Player wants to revert back to the default selected action
                else if (GameControls.gamePlayActions.menuContext.WasPressed && !player.UnitActionHandler.PlayerActionHandler.DefaultActionIsSelected)
                {
                    player.UnitActionHandler.PlayerActionHandler.SetDefaultSelectedAction();
                    ContextMenu.StartContextMenuCooldown();
                }
                // If it's time for the Player to choose an action
                else if (player.IsMyTurn && !player.UnitActionHandler.IsPerformingAction && !player.UnitActionHandler.MoveAction.IsMoving)
                {
                    // If the player wants to skip their turn
                    if (GameControls.gamePlayActions.skipTurn.IsPressed && skipTurnCooldownTimer >= skipTurnCooldown)
                    {
                        skipTurnCooldownTimer = 0f;
                        ActionLineRenderer.Instance.HideLineRenderers();
                        player.UnitActionHandler.SkipTurn();
                        return;
                    }

                    if (GameControls.gamePlayActions.switchVersatileStance.WasPressed)
                    {
                        VersatileStanceAction versatileStanceAction = player.UnitActionHandler.GetAction<VersatileStanceAction>();
                        if (versatileStanceAction != null && versatileStanceAction.IsValidAction())
                            player.UnitActionHandler.GetAction<VersatileStanceAction>().QueueAction();
                        return;
                    }

                    // Display the appropriate mouse cursor and line renderer, depending on what/who is at mouse grid position and which action is currently selected by the player
                    SetupCursorAndLineRenderer();

                    // If the Player is trying to perform the Turn Action
                    if (GameControls.gamePlayActions.turnMode.IsPressed || player.UnitActionHandler.PlayerActionHandler.SelectedAction is TurnAction)
                        HandleTurnMode();
                    // If the Player is trying to swap their weapon set
                    else if (GameControls.gamePlayActions.swapWeapons.WasPressed && !GameControls.gamePlayActions.turnMode.IsPressed)
                        player.UnitActionHandler.GetAction<SwapWeaponSetAction>().QueueAction();
                    // If the Player selects a grid position to try and perform an action
                    else if (GameControls.gamePlayActions.select.WasPressed)
                        HandleActions();
                }
                else if (player.UnitActionHandler.QueuedActions.Count > 0 || player.UnitActionHandler.MoveAction.IsMoving)
                {
                    ActionLineRenderer.Instance.HideLineRenderers();
                    WorldMouse.ChangeCursor(CursorState.Default);
                }
            }
            else
            {
                ActionLineRenderer.Instance.HideLineRenderers();
                WorldMouse.ChangeCursor(CursorState.Default);
            }
        }

        void HandleTurnMode()
        {
            TurnAction turnAction = player.UnitActionHandler.TurnAction;
            player.UnitActionHandler.PlayerActionHandler.SetSelectedActionType(turnAction.ActionType, true);
            WorldMouse.ChangeCursor(CursorState.Default);

            mouseGridPosition = WorldMouse.CurrentGridPosition();
            if (GameControls.gamePlayActions.select.WasPressed && mouseGridPosition != player.GridPosition)
                turnAction.QueueAction(mouseGridPosition);
        }

        void HandleActions()
        {
            // If the mouse is hovering over an Interactable
            if (HighlightedInteractable != null)
            {
                // Do nothing if the player is in the same grid position as the interactable (such as an open door...loose items are okay though)
                if (player.GridPosition == HighlightedInteractable.GridPosition() && !HighlightedInteractable.CanInteractAtMyGridPosition())
                    return;

                // Interact with or move to the interactable
                player.UnitActionHandler.InteractAction.QueueAction(HighlightedInteractable);
            }
            // If the mouse is hovering over a dead Unit
            else if (HighlightedUnit != null && HighlightedUnit.Health.IsDead)
            {
                // Interact with or move to the dead Unit
                player.UnitActionHandler.InteractAction.QueueAction(HighlightedUnit.UnitInteractable);
            }
            // If the player is trying to perform an action on themselves
            else if (mouseGridPosition == player.GridPosition)
            {
                // TODO: Implement actions that can be performed on oneself (such as healing or a buff)
            }
            // Make sure the mouse grid position is a valid position to perform an action
            else if (LevelGrid.IsValidGridPosition(mouseGridPosition) && AstarPath.active.GetNearest(mouseGridPosition.WorldPosition).node.Walkable)
            {
                Unit unitAtGridPosition = LevelGrid.GetUnitAtGridPosition(mouseGridPosition);
                BaseAction selectedAction = player.UnitActionHandler.PlayerActionHandler.SelectedActionType.GetAction(player);

                // If the mouse is hovering over a living unit that's in the player's Vision
                if (unitAtGridPosition != null && unitAtGridPosition.Health.IsDead == false && player.Vision.IsVisible(unitAtGridPosition))
                {
                    // If the unit is someone the player can attack (an enemy, or a neutral unit, but only if we have an attack action selected)
                    if (player.Stats.HasEnoughEnergy(selectedAction.InitialEnergyCost()) && (player.Alliance.IsEnemy(unitAtGridPosition) || (player.Alliance.IsNeutral(unitAtGridPosition) && selectedAction is BaseAttackAction)))
                    {
                        // Set the Unit as the target enemy
                        player.UnitActionHandler.SetTargetEnemyUnit(unitAtGridPosition);

                        // If the player has an attack action selected
                        if (selectedAction is BaseAttackAction)
                        {
                            // If the target is in attack range
                            if (selectedAction.BaseAttackAction.IsInAttackRange(unitAtGridPosition, player.GridPosition, mouseGridPosition))
                            {
                                // If the target enemy unit is already completely surrounded by other units or other obstructions
                                float attackRange = player.GetAttackRange();
                                if (unitAtGridPosition.IsCompletelySurrounded(attackRange) && attackRange < 2f)
                                {
                                    // Remove the unit as the target enemy
                                    player.UnitActionHandler.SetTargetEnemyUnit(null);
                                    return;
                                }

                                HighlightedUnit = null;
                                TooltipManager.ClearUnitTooltips();

                                // Turn towards and attack the target enemy
                                player.UnitActionHandler.PlayerActionHandler.AttackTarget();
                                return;
                            }
                        }
                        // If the player has a ranged weapon equipped and the target enemy is within attack range
                        else if (player.UnitEquipment.RangedWeaponEquipped && player.UnitEquipment.HasValidAmmunitionEquipped())
                        {
                            // Do nothing if the target unit is dead
                            if (unitAtGridPosition.Health.IsDead)
                                return;

                            // If the target is in shooting range
                            if (player.UnitActionHandler.GetAction<ShootAction>().IsInAttackRange(unitAtGridPosition, player.GridPosition, mouseGridPosition))
                            {
                                HighlightedUnit = null;
                                TooltipManager.ClearUnitTooltips();

                                // Turn towards and attack the target enemy
                                player.UnitActionHandler.PlayerActionHandler.AttackTarget();
                                return;
                            }
                        }
                        // If the player has a melee weapon equipped or is unarmed and the target enemy is within attack range
                        else if (player.UnitEquipment.MeleeWeaponEquipped || player.UnitEquipment.IsUnarmed || player.UnitEquipment.RangedWeaponEquipped)
                        {
                            // Do nothing if the target unit is dead
                            if (unitAtGridPosition.Health.IsDead)
                                return;

                            // If the target is in attack range
                            if (player.UnitActionHandler.GetAction<MeleeAction>().IsInAttackRange(unitAtGridPosition, player.GridPosition, mouseGridPosition))
                            {
                                // If the target enemy unit is already completely surrounded by other units or other obstructions
                                float attackRange = player.GetAttackRange();
                                if (unitAtGridPosition.IsCompletelySurrounded(attackRange) && attackRange < 2f)
                                {
                                    // Remove the unit as the target enemy
                                    player.UnitActionHandler.SetTargetEnemyUnit(null);
                                    return;
                                }

                                HighlightedUnit = null;
                                TooltipManager.ClearUnitTooltips();

                                // Turn towards and attack the target enemy
                                player.UnitActionHandler.PlayerActionHandler.AttackTarget();
                                return;
                            }
                        }

                        //////////////////////////////////////////////////////////////////////////////////////////////////////////////
                        // The target enemy wasn't in attack range, so find and move to the nearest melee or ranged attack position //
                        //////////////////////////////////////////////////////////////////////////////////////////////////////////////

                        // If the player has a Move, Melee, or Shoot Action selected
                        if (selectedAction.IsDefaultAttackAction || selectedAction is MoveAction)
                        {
                            // If the player has a ranged weapon equipped, find the nearest possible Shoot Action attack position
                            if (player.UnitEquipment.RangedWeaponEquipped && player.UnitEquipment.HasValidAmmunitionEquipped() && selectedAction is MeleeAction == false)
                            {
                                if (player.UnitActionHandler.MoveAction.CanMove)
                                    player.UnitActionHandler.MoveAction.QueueAction(player.UnitActionHandler.GetAction<ShootAction>().GetNearestAttackPosition(player.GridPosition, unitAtGridPosition));
                                else
                                    Debug.Log("You cannot move...");
                            }
                            // If the player has a melee weapon equipped or is unarmed, find the nearest possible Melee Action attack position
                            else if (player.UnitEquipment.MeleeWeaponEquipped || player.Stats.CanFightUnarmed)
                            {
                                if (player.UnitActionHandler.MoveAction.CanMove)
                                    player.UnitActionHandler.MoveAction.QueueAction(player.UnitActionHandler.GetAction<MeleeAction>().GetNearestAttackPosition(player.GridPosition, unitAtGridPosition));
                                else
                                    Debug.Log("You cannot move...");
                            }
                            else
                            {
                                player.UnitActionHandler.SetTargetEnemyUnit(null);
                                player.UnitActionHandler.PlayerActionHandler.SetDefaultSelectedAction();
                                return;
                            }
                        }
                        else // The player either doesn't have an attack action selected or has a non-default attack action selected
                        {
                            player.UnitActionHandler.SetTargetEnemyUnit(null);
                            return;
                        }
                    }
                    else // The unit the mouse is hovering over is not an attackable unit (likely an ally or a dead unit) or the Player doesn't have enough energy for the selected action
                    {
                        // Set the selected action to Move if the Player doesn't have enough energy for their selected action
                        if (!player.Stats.HasEnoughEnergy(selectedAction.InitialEnergyCost()))
                            player.UnitActionHandler.PlayerActionHandler.SetDefaultSelectedAction();

                        player.UnitActionHandler.SetTargetEnemyUnit(null);
                        return;
                    }
                }
                // If there's no unit or a dead unit at the mouse position, but the player is still trying to attack this position (probably trying to use a multi-tile attack)
                else if (selectedAction is BaseAttackAction)
                {
                    // Make sure the Player has enough energy for the attack
                    if (!player.Stats.HasEnoughEnergy(selectedAction.InitialEnergyCost()))
                        return;

                    // If there's any enemy or neutral unit within the attack positions
                    if (selectedAction.IsValidUnitInActionArea(mouseGridPosition) == false)
                        return;

                    // Turn towards and attack the target enemy
                    selectedAction.BaseAttackAction.QueueAction(mouseGridPosition);
                }
                // If there's no unit or a dead unit at the mouse position & move action is selected
                else if (selectedAction is MoveAction)
                {
                    // If there's a non-visible Unit at this position and they're one tile away and could directly be within the line of sight (basically just meaning there's no obstacles in the way), just turn to face that tile
                    if (unitAtGridPosition != null && Vector3.Distance(player.WorldPosition, unitAtGridPosition.WorldPosition) <= LevelGrid.diaganolDistance && player.Vision.IsInLineOfSight_Raycast(unitAtGridPosition))
                        player.UnitActionHandler.TurnAction.QueueAction(mouseGridPosition);
                    else
                    {
                        if (player.UnitActionHandler.MoveAction.CanMove)
                            player.UnitActionHandler.MoveAction.QueueAction(mouseGridPosition);
                        else
                            Debug.Log("You cannot move...");
                    }
                }
            }
        }

        void SetupCursorAndLineRenderer()
        {
            if (EventSystem.current.IsPointerOverGameObject())
            {
                HighlightedUnit = null;
                ClearHighlightedInteractable();
                WorldMouse.ChangeCursor(CursorState.Default);
                lastMouseGridPosition = player.GridPosition;
                return;
            }

            mouseGridPosition = WorldMouse.CurrentGridPosition();
            if (mouseGridPosition != lastMouseGridPosition)
                HighlightedUnit = null;

            BaseAction selectedAction = player.UnitActionHandler.PlayerActionHandler.SelectedActionType.GetAction(player);

            if (selectedAction != null)
            {
                if (selectedAction is MoveAction)
                {
                    if (Physics.Raycast(Camera.main.ScreenPointToRay(Input.mousePosition), out RaycastHit interactableHit, 1000, interactableMask))
                    {
                        HighlightedUnit = null;
                        if (HighlightedInteractable == null || HighlightedInteractable.gameObject != interactableHit.transform.gameObject)
                        {
                            if (interactableHit.transform.TryGetComponent(out Interactable interactable))
                            {
                                HighlightedInteractable = interactable;
                                if (interactable is LooseItem)
                                {
                                    LooseItem looseItem = interactable as LooseItem;
                                    TooltipManager.ShowLooseItemTooltip(looseItem, looseItem.ItemData);
                                }
                            }
                        }

                        if (HighlightedInteractable == null)
                            WorldMouse.ChangeCursor(CursorState.Default);
                        else if (HighlightedInteractable is LooseItem)
                        {
                            if (HighlightedInteractable is LooseContainerItem)
                            {
                                LooseContainerItem highlightedContainer = HighlightedInteractable as LooseContainerItem;
                                if (highlightedContainer.ContainerInventoryManager != null && highlightedContainer.ContainerInventoryManager.ContainsAnyItems())
                                    WorldMouse.ChangeCursor(CursorState.LootBag);
                                else
                                    WorldMouse.ChangeCursor(CursorState.PickupItem);
                            }
                            else
                                WorldMouse.ChangeCursor(CursorState.PickupItem);

                        }
                        else if (HighlightedInteractable is Door)
                            WorldMouse.ChangeCursor(CursorState.UseDoor);
                    }
                    else if (Physics.Raycast(Camera.main.ScreenPointToRay(Input.mousePosition), out RaycastHit unitHit, 1000, unitMask))
                    {
                        ClearHighlightedInteractable();

                        if (HighlightedUnit == null || HighlightedUnit.gameObject != unitHit.transform.gameObject)
                        {
                            if (unitHit.transform.TryGetComponent(out Unit targetUnit))
                            {
                                HighlightedUnit = targetUnit;
                                if (HighlightedUnit != player && !HighlightedUnit.Health.IsDead && player.Alliance.IsEnemy(HighlightedUnit) && player.Vision.IsVisible(HighlightedUnit))
                                {
                                    if (player.UnitEquipment.RangedWeaponEquipped && player.UnitEquipment.HasValidAmmunitionEquipped())
                                        TooltipManager.ShowUnitHitChanceTooltips(targetUnit.GridPosition, player.UnitActionHandler.GetAction<ShootAction>());
                                    else
                                        TooltipManager.ShowUnitHitChanceTooltips(targetUnit.GridPosition, player.UnitActionHandler.GetAction<MeleeAction>());
                                }
                                else
                                    TooltipManager.ClearUnitTooltips();
                            }
                            else
                                TooltipManager.ClearUnitTooltips();
                        }
                        
                        if (HighlightedUnit == null)
                            WorldMouse.ChangeCursor(CursorState.Default);
                        else if (HighlightedUnit.Health.IsDead && player.Vision.IsVisible(HighlightedUnit))
                            WorldMouse.ChangeCursor(CursorState.LootBag);
                        else if (player.Alliance.IsEnemy(HighlightedUnit) && player.Vision.IsVisible(HighlightedUnit))
                            SetAttackCursor();
                        else
                            WorldMouse.ChangeCursor(CursorState.Default);
                    }
                    else
                    {
                        Unit unitAtGridPosition = LevelGrid.GetUnitAtGridPosition(mouseGridPosition);
                        if (unitAtGridPosition != null && player.Vision.IsVisible(unitAtGridPosition) && (player.Alliance.IsEnemy(unitAtGridPosition) || selectedAction.IsDefaultAttackAction))
                        {
                            ClearHighlightedInteractable();
                            SetAttackCursor();
                            
                            if (unitAtGridPosition != player && HighlightedUnit != unitAtGridPosition && unitAtGridPosition.Health.IsDead == false && player.Vision.IsVisible(unitAtGridPosition))
                            {
                                if (player.UnitEquipment.RangedWeaponEquipped && player.UnitEquipment.HasValidAmmunitionEquipped())
                                    TooltipManager.ShowUnitHitChanceTooltips(unitAtGridPosition.GridPosition, player.UnitActionHandler.GetAction<ShootAction>());
                                else
                                    TooltipManager.ShowUnitHitChanceTooltips(unitAtGridPosition.GridPosition, player.UnitActionHandler.GetAction<MeleeAction>());
                            }
                            else
                                TooltipManager.ClearUnitTooltips();

                            HighlightedUnit = unitAtGridPosition;
                        }
                        else
                        {
                            TooltipManager.ClearUnitTooltips();

                            HighlightedUnit = null;
                            ClearHighlightedInteractable();
                            WorldMouse.ChangeCursor(CursorState.Default);
                        }
                    }

                    StartCoroutine(ActionLineRenderer.Instance.DrawMovePath());
                }
                else if (selectedAction is BaseAttackAction)
                {
                    ClearHighlightedInteractable();
                    Unit unitAtGridPosition = LevelGrid.GetUnitAtGridPosition(mouseGridPosition);

                    if (lastMouseGridPosition != mouseGridPosition)
                    {
                        TooltipManager.ClearUnitTooltips();
                        if (selectedAction.BaseAttackAction.IsValidUnitInActionArea(mouseGridPosition))
                            TooltipManager.ShowUnitHitChanceTooltips(mouseGridPosition, selectedAction);

                        GridSystemVisual.UpdateAttackGridVisual();
                    }

                    if (unitAtGridPosition != null && !unitAtGridPosition.Health.IsDead && !player.Alliance.IsAlly(unitAtGridPosition) && player.Vision.IsVisible(unitAtGridPosition))
                    {
                        StartCoroutine(ActionLineRenderer.Instance.DrawMovePath());
                        SetAttackCursor();

                        HighlightedUnit = unitAtGridPosition;
                    }
                    else
                    {
                        HighlightedUnit = null;
                        ActionLineRenderer.Instance.HideLineRenderers();
                        WorldMouse.ChangeCursor(CursorState.Default);
                    }
                }
                else if (selectedAction is TurnAction)
                {
                    if (HighlightedUnit != null)
                        TooltipManager.ClearUnitTooltips();

                    HighlightedUnit = null;
                    ClearHighlightedInteractable();

                    player.UnitActionHandler.TurnAction.SetTargetPosition(player.UnitActionHandler.TurnAction.DetermineTargetTurnDirection(LevelGrid.GetGridPosition(WorldMouse.GetPosition())));
                    ActionLineRenderer.Instance.DrawTurnArrow(player.UnitActionHandler.TurnAction.targetPosition);
                    WorldMouse.ChangeCursor(CursorState.Default);
                }
            }

            lastMouseGridPosition = mouseGridPosition;
        }

        void ClearHighlightedInteractable()
        {
            if (HighlightedInteractable != null)
            {
                HighlightedInteractable = null;
                if (!GameControls.gamePlayActions.showLooseItemTooltips.IsPressed)
                    TooltipManager.ClearAllTooltips();
            }
        }

        void SetAttackCursor()
        {
            if (player.UnitEquipment.RangedWeaponEquipped && player.UnitEquipment.HasValidAmmunitionEquipped() && player.UnitActionHandler.PlayerActionHandler.SelectedAction is MeleeAction == false)
            {
                WorldMouse.ChangeCursor(CursorState.RangedAttack);
                if (player.UnitActionHandler.PlayerActionHandler.SelectedAction is BaseAttackAction && player.UnitActionHandler.PlayerActionHandler.SelectedAction.BaseAttackAction.IsInAttackRange(null, player.GridPosition, mouseGridPosition))
                    ActionLineRenderer.Instance.DrawParabola(player.WorldPosition + (player.ShoulderHeight * Vector3.up), mouseGridPosition.WorldPosition);
            }
            else if (player.UnitEquipment.MeleeWeaponEquipped || player.Stats.CanFightUnarmed)
                WorldMouse.ChangeCursor(CursorState.MeleeAttack);
            else
                WorldMouse.ChangeCursor(CursorState.Default);
        }

        bool AttackActionSelected() => player.UnitActionHandler.PlayerActionHandler.SelectedAction is BaseAttackAction;

        public void SetAutoAttack(bool shouldAutoAttack) => AutoAttack = shouldAutoAttack;
    }
}
