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
using UnitSystem.UI;

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

            if (InventoryUI.IsDraggingItem || ActionSystemUI.IsDraggingAction)
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
            
            if (!player.HealthSystem.IsDead)
            {
                // If the Player was holding the button for turn mode (the Turn Action) and then they release it
                if (GameControls.gamePlayActions.turnMode.WasReleased && player.UnitActionHandler.PlayerActionHandler.SelectedActionType.GetAction(player) is Action_Turn)
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
                        Action_VersatileStance versatileStanceAction = player.UnitActionHandler.GetAction<Action_VersatileStance>();
                        if (versatileStanceAction != null && versatileStanceAction.IsValidAction())
                            player.UnitActionHandler.GetAction<Action_VersatileStance>().QueueAction();
                        return;
                    }

                    if (GameControls.gamePlayActions.sneak.WasPressed)
                        player.UnitActionHandler.MoveAction.SetMoveMode(MoveMode.Sneak);
                    else if (GameControls.gamePlayActions.walk.WasPressed)
                        player.UnitActionHandler.MoveAction.SetMoveMode(MoveMode.Walk);
                    else if (GameControls.gamePlayActions.run.WasPressed)
                        player.UnitActionHandler.MoveAction.SetMoveMode(MoveMode.Run);
                    else if (GameControls.gamePlayActions.sprint.WasPressed)
                        player.UnitActionHandler.MoveAction.SetMoveMode(MoveMode.Sprint);

                    // Display the appropriate mouse cursor and line renderer, depending on what/who is at mouse grid position and which action is currently selected by the player
                    SetupCursorAndLineRenderer();

                    // If the Player is trying to perform the Turn Action
                    if (GameControls.gamePlayActions.turnMode.IsPressed || player.UnitActionHandler.PlayerActionHandler.SelectedAction is Action_Turn)
                        HandleTurnMode();
                    // If the Player is trying to swap their weapon set
                    else if (GameControls.gamePlayActions.swapWeapons.WasPressed && !GameControls.gamePlayActions.turnMode.IsPressed)
                        player.UnitActionHandler.GetAction<Action_SwapWeaponSet>().QueueAction();
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
            Action_Turn turnAction = player.UnitActionHandler.TurnAction;
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
            else if (HighlightedUnit != null && HighlightedUnit.HealthSystem.IsDead)
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
                Action_Base selectedAction = player.UnitActionHandler.PlayerActionHandler.SelectedActionType.GetAction(player);

                // If the mouse is hovering over a living unit that's in the player's Vision
                if (unitAtGridPosition != null && unitAtGridPosition.HealthSystem.IsDead == false && player.Vision.IsVisible(unitAtGridPosition))
                {
                    // If the unit is someone the player can attack (an enemy, or a neutral unit, but only if we have an attack action selected)
                    if (player.Stats.HasEnoughEnergy(selectedAction.EnergyCost()) && (player.Alliance.IsEnemy(unitAtGridPosition) || (player.Alliance.IsNeutral(unitAtGridPosition) && selectedAction is Action_BaseAttack)))
                    {
                        // Set the Unit as the target enemy
                        player.UnitActionHandler.SetTargetEnemyUnit(unitAtGridPosition);

                        // If the player has an attack action selected
                        if (selectedAction is Action_BaseAttack)
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
                            if (unitAtGridPosition.HealthSystem.IsDead)
                                return;

                            // If the target is in shooting range
                            if (player.UnitActionHandler.GetAction<Action_Shoot>().IsInAttackRange(unitAtGridPosition, player.GridPosition, mouseGridPosition))
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
                            if (unitAtGridPosition.HealthSystem.IsDead)
                                return;

                            // If the target is in attack range
                            if (player.UnitActionHandler.GetAction<Action_Melee>().IsInAttackRange(unitAtGridPosition, player.GridPosition, mouseGridPosition))
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
                        if (selectedAction.IsDefaultAttackAction || selectedAction is Action_Move)
                        {
                            // If the player has a ranged weapon equipped, find the nearest possible Shoot Action attack position
                            if (player.UnitEquipment.RangedWeaponEquipped && player.UnitEquipment.HasValidAmmunitionEquipped() && selectedAction is Action_Melee == false)
                            {
                                if (player.UnitActionHandler.MoveAction.CanMove)
                                    player.UnitActionHandler.MoveAction.QueueAction(player.UnitActionHandler.GetAction<Action_Shoot>().GetNearestAttackPosition(player.GridPosition, unitAtGridPosition));
                                else
                                    Debug.Log("You cannot move...");
                            }
                            // If the player has a melee weapon equipped or is unarmed, find the nearest possible Melee Action attack position
                            else if (player.UnitEquipment.MeleeWeaponEquipped || player.Stats.CanFightUnarmed)
                            {
                                if (player.UnitActionHandler.MoveAction.CanMove)
                                    player.UnitActionHandler.MoveAction.QueueAction(player.UnitActionHandler.GetAction<Action_Melee>().GetNearestAttackPosition(player.GridPosition, unitAtGridPosition));
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
                        if (!player.Stats.HasEnoughEnergy(selectedAction.EnergyCost()))
                            player.UnitActionHandler.PlayerActionHandler.SetDefaultSelectedAction();

                        player.UnitActionHandler.SetTargetEnemyUnit(null);
                        return;
                    }
                }
                // If there's no unit or a dead unit at the mouse position, but the player is still trying to attack this position (probably trying to use a multi-tile attack)
                else if (selectedAction is Action_BaseAttack)
                {
                    // Make sure the Player has enough energy for the attack
                    if (!player.Stats.HasEnoughEnergy(selectedAction.EnergyCost()))
                        return;

                    // If there's any enemy or neutral unit within the attack positions
                    if (selectedAction.IsValidUnitInActionArea(mouseGridPosition) == false)
                        return;

                    // Turn towards and attack the target enemy
                    selectedAction.BaseAttackAction.QueueAction(mouseGridPosition);
                }
                // If there's no unit or a dead unit at the mouse position & move action is selected
                else if (selectedAction is Action_Move)
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

            Action_Base selectedAction = player.UnitActionHandler.PlayerActionHandler.SelectedActionType.GetAction(player);

            if (selectedAction != null)
            {
                if (selectedAction is Action_Move)
                {
                    if (Physics.Raycast(Camera.main.ScreenPointToRay(Input.mousePosition), out RaycastHit interactableHit, 1000, interactableMask))
                    {
                        HighlightedUnit = null;
                        if (HighlightedInteractable == null || HighlightedInteractable.gameObject != interactableHit.transform.gameObject)
                        {
                            if (interactableHit.transform.TryGetComponent(out Interactable interactable))
                            {
                                HighlightedInteractable = interactable;
                                if (interactable is Interactable_LooseItem)
                                {
                                    Interactable_LooseItem looseItem = interactable as Interactable_LooseItem;
                                    TooltipManager.ShowLooseItemTooltip(looseItem, looseItem.ItemData);
                                }
                            }
                        }

                        if (HighlightedInteractable == null)
                            WorldMouse.ChangeCursor(CursorState.Default);
                        else if (HighlightedInteractable is Interactable_LooseItem)
                        {
                            if (HighlightedInteractable is Interactable_LooseContainerItem)
                            {
                                Interactable_LooseContainerItem highlightedContainer = HighlightedInteractable as Interactable_LooseContainerItem;
                                if (highlightedContainer.ContainerInventoryManager != null && highlightedContainer.ContainerInventoryManager.ContainsAnyItems())
                                    WorldMouse.ChangeCursor(CursorState.LootBag);
                                else
                                    WorldMouse.ChangeCursor(CursorState.PickupItem);
                            }
                            else
                                WorldMouse.ChangeCursor(CursorState.PickupItem);

                        }
                        else if (HighlightedInteractable is Interactable_Door)
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
                                mouseGridPosition = targetUnit.GridPosition;

                                if (HighlightedUnit != player && !HighlightedUnit.HealthSystem.IsDead && player.Alliance.IsEnemy(HighlightedUnit) && player.Vision.IsVisible(HighlightedUnit))
                                {
                                    if (player.UnitEquipment.RangedWeaponEquipped && player.UnitEquipment.HasValidAmmunitionEquipped())
                                        TooltipManager.ShowUnitHitChanceTooltips(targetUnit.GridPosition, player.UnitActionHandler.GetAction<Action_Shoot>());
                                    else
                                        TooltipManager.ShowUnitHitChanceTooltips(targetUnit.GridPosition, player.UnitActionHandler.GetAction<Action_Melee>());
                                }
                                else
                                    TooltipManager.ClearUnitTooltips();
                            }
                            else
                                TooltipManager.ClearUnitTooltips();
                        }
                        
                        if (HighlightedUnit == null)
                            WorldMouse.ChangeCursor(CursorState.Default);
                        else if (HighlightedUnit.HealthSystem.IsDead && player.Vision.IsVisible(HighlightedUnit))
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
                            
                            if (unitAtGridPosition != player && HighlightedUnit != unitAtGridPosition && !unitAtGridPosition.HealthSystem.IsDead && player.Vision.IsVisible(unitAtGridPosition))
                            {
                                if (player.UnitEquipment.RangedWeaponEquipped && player.UnitEquipment.HasValidAmmunitionEquipped())
                                    TooltipManager.ShowUnitHitChanceTooltips(unitAtGridPosition.GridPosition, player.UnitActionHandler.GetAction<Action_Shoot>());
                                else
                                    TooltipManager.ShowUnitHitChanceTooltips(unitAtGridPosition.GridPosition, player.UnitActionHandler.GetAction<Action_Melee>());
                            }

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
                else if (selectedAction is Action_BaseAttack)
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

                    if (unitAtGridPosition != null && !unitAtGridPosition.HealthSystem.IsDead && !player.Alliance.IsAlly(unitAtGridPosition) && player.Vision.IsVisible(unitAtGridPosition))
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
                else if (selectedAction is Action_Turn)
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

            ShowFloatingStatBars();
            lastMouseGridPosition = mouseGridPosition;
        }

        void ShowFloatingStatBars()
        {
            if (HighlightedUnit != null && HighlightedUnit != player)
            {
                if (HighlightedUnit.StatBarManager != null)
                    HighlightedUnit.StatBarManager.Show(HighlightedUnit);
                else
                    Pool_FloatingStatBar.GetFloatingStatBarsFromPool().Show(HighlightedUnit);
            }
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
            if (player.UnitEquipment.RangedWeaponEquipped && player.UnitEquipment.HasValidAmmunitionEquipped() && player.UnitActionHandler.PlayerActionHandler.SelectedAction is Action_Melee == false)
                WorldMouse.ChangeCursor(CursorState.RangedAttack);
            else if (player.UnitEquipment.MeleeWeaponEquipped || player.Stats.CanFightUnarmed)
                WorldMouse.ChangeCursor(CursorState.MeleeAttack);
            else
                WorldMouse.ChangeCursor(CursorState.Default);
        }

        bool AttackActionSelected() => player.UnitActionHandler.PlayerActionHandler.SelectedAction is Action_BaseAttack;

        public void SetAutoAttack(bool shouldAutoAttack) => AutoAttack = shouldAutoAttack;
    }
}
