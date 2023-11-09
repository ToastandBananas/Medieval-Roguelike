using UnityEngine;
using UnityEngine.EventSystems;
using InteractableObjects;
using GridSystem;
using InventorySystem;
using Controls;
using GeneralUI;
using UnitSystem;
using Utilities;
using ContextMenu = GeneralUI.ContextMenu;

namespace ActionSystem
{
    public class PlayerInput : MonoBehaviour
    {
        public static PlayerInput Instance;

        [SerializeField] LayerMask interactableMask;
        [SerializeField] LayerMask unitMask;

        public Interactable highlightedInteractable { get; private set; }
        public Unit highlightedUnit { get; private set; }
        public bool autoAttack { get; private set; }

        Unit player;

        float skipTurnCooldown = 0.1f;
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

            if (InventoryUI.isDraggingItem || ActionSystemUI.isDraggingAction)
                return;

            if (EventSystem.current.IsPointerOverGameObject())
            {
                ActionLineRenderer.Instance.HideLineRenderers();
                WorldMouse.ChangeCursor(CursorState.Default);
                ClearHighlightedInteractable();
                return;
            }
            
            if (player.health.IsDead() == false)
            {
                // If the Player was holding the button for turn mode (the Turn Action) and then they release it
                if (GameControls.gamePlayActions.turnMode.WasReleased && player.unitActionHandler.selectedActionType.GetAction(player) is TurnAction)
                {
                    // Reset the line renderer and go back to the Move Action
                    ActionLineRenderer.ResetCurrentPositions();
                    player.unitActionHandler.SetDefaultSelectedAction();
                }

                // If the Player is trying to cancel their current action
                if (GameControls.gamePlayActions.cancelAction.WasPressed)
                {
                    player.unitActionHandler.CancelActions();
                    ActionLineRenderer.ResetCurrentPositions();
                }
                // If the Player wants to revert back to the default selected action
                else if (GameControls.gamePlayActions.menuContext.WasPressed && player.unitActionHandler.DefaultActionIsSelected == false)
                {
                    player.unitActionHandler.SetDefaultSelectedAction();
                    ContextMenu.StartContextMenuCooldown();
                }
                // If it's time for the Player to choose an action
                else if (player.IsMyTurn && player.unitActionHandler.isPerformingAction == false && player.unitActionHandler.isMoving == false)
                {
                    // If the player wants to skip their turn
                    if (GameControls.gamePlayActions.skipTurn.IsPressed && skipTurnCooldownTimer >= skipTurnCooldown)
                    {
                        skipTurnCooldownTimer = 0f;
                        ActionLineRenderer.Instance.HideLineRenderers();
                        player.unitActionHandler.SkipTurn();
                        return;
                    }

                    // Display the appropriate mouse cursor and line renderer, depending on what/who is at mouse grid position and which action is currently selected by the player
                    SetupCursorAndLineRenderer();

                    // If the Player has an attack action selected
                    BaseAction selectedAction = player.unitActionHandler.SelectedAction;
                    if (selectedAction is BaseAttackAction)
                        GridSystemVisual.UpdateAttackGridVisual();

                    // If the Player is trying to perform the Turn Action
                    if (GameControls.gamePlayActions.turnMode.IsPressed || selectedAction is TurnAction)
                        HandleTurnMode();
                    // If the Player is trying to swap their weapon set
                    else if (GameControls.gamePlayActions.swapWeapons.WasPressed && GameControls.gamePlayActions.turnMode.IsPressed == false)
                        player.unitActionHandler.GetAction<SwapWeaponSetAction>().QueueAction();
                    // If the Player selects a grid position to try and perform an action
                    else if (GameControls.gamePlayActions.select.WasPressed)
                        HandleActions();
                }
                else if (player.unitActionHandler.queuedActions.Count > 0 || player.unitActionHandler.isMoving)
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
            TurnAction turnAction = player.unitActionHandler.turnAction;
            player.unitActionHandler.SetSelectedActionType(player.unitActionHandler.FindActionTypeByName(turnAction.GetType().Name));
            WorldMouse.ChangeCursor(CursorState.Default);

            if (GameControls.gamePlayActions.select.WasPressed && WorldMouse.CurrentGridPosition() != player.GridPosition)
                turnAction.QueueAction(WorldMouse.CurrentGridPosition());
        }

        void HandleActions()
        {
            GridPosition mouseGridPosition = WorldMouse.CurrentGridPosition();
            
            // If the mouse is hovering over an Interactable
            if (highlightedInteractable != null)
            {
                // Do nothing if the player is in the same grid position as the interactable (such as an open door...loose items are okay though)
                if (player.GridPosition == highlightedInteractable.GridPosition() && highlightedInteractable.CanInteractAtMyGridPosition() == false)
                    return;

                // If the player is too far away from the Interactable to interact with it
                if (TacticsPathfindingUtilities.CalculateWorldSpaceDistance_XYZ(player.GridPosition, highlightedInteractable.GridPosition()) > LevelGrid.diaganolDistance)
                {
                    if (player.unitActionHandler.moveAction.canMove)
                    {
                        // Queue a Move Action towards the Interactable
                        player.unitActionHandler.GetAction<InteractAction>().SetTargetInteractable(highlightedInteractable);
                        player.unitActionHandler.moveAction.QueueAction(LevelGrid.GetNearestSurroundingGridPosition(highlightedInteractable.GridPosition(), player.GridPosition, LevelGrid.diaganolDistance, highlightedInteractable.CanInteractAtMyGridPosition()));
                    }
                    else
                        Debug.Log("You cannot move...");
                }
                else
                {
                    // Queue the Interact Action with the Interactable
                    player.unitActionHandler.GetAction<InteractAction>().QueueAction(highlightedInteractable);
                }
            }
            // If the mouse is hovering over a dead Unit
            else if (highlightedUnit != null && highlightedUnit.health.IsDead())
            {
                // If the player is too far away from the Interactable to interact with it
                if (TacticsPathfindingUtilities.CalculateWorldSpaceDistance_XYZ(player.GridPosition, highlightedUnit.GridPosition) > LevelGrid.diaganolDistance)
                {
                    if (player.unitActionHandler.moveAction.canMove)
                    {
                        // Queue a Move Action towards the Interactable
                        player.unitActionHandler.GetAction<InteractAction>().SetTargetInteractable(highlightedUnit.unitInteractable);
                        player.unitActionHandler.moveAction.QueueAction(LevelGrid.GetNearestSurroundingGridPosition(highlightedUnit.GridPosition, player.GridPosition, LevelGrid.diaganolDistance, highlightedUnit.unitInteractable.CanInteractAtMyGridPosition()));
                    }
                    else
                        Debug.Log("You cannot move...");
                }
                else
                {
                    // Queue the Interact Action with the Interactable
                    player.unitActionHandler.GetAction<InteractAction>().QueueAction(highlightedUnit.unitInteractable);
                }
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
                BaseAction selectedAction = player.unitActionHandler.selectedActionType.GetAction(player);
                
                // If the mouse is hovering over a living unit that's in the player's Vision
                if (unitAtGridPosition != null && unitAtGridPosition.health.IsDead() == false && player.vision.IsVisible(unitAtGridPosition))
                {
                    // If the unit is someone the player can attack (an enemy, or a neutral unit, but only if we have an attack action selected)
                    if (player.stats.HasEnoughEnergy(selectedAction.GetEnergyCost()) && (player.alliance.IsEnemy(unitAtGridPosition) || (player.alliance.IsNeutral(unitAtGridPosition) && selectedAction is BaseAttackAction)))
                    {
                        // Set the Unit as the target enemy
                        player.unitActionHandler.SetTargetEnemyUnit(unitAtGridPosition);

                        // If the player has an attack action selected
                        if (selectedAction is BaseAttackAction)
                        {
                            // If the target is in attack range
                            if (selectedAction.BaseAttackAction.IsInAttackRange(unitAtGridPosition, player.GridPosition, mouseGridPosition))
                            {
                                // If the target enemy unit is already completely surrounded by other units or other obstructions
                                float attackRange = player.GetAttackRange(unitAtGridPosition, false);
                                if (unitAtGridPosition.IsCompletelySurrounded(attackRange) && attackRange < 2f)
                                {
                                    // Remove the unit as the target enemy
                                    player.unitActionHandler.SetTargetEnemyUnit(null);
                                    return;
                                }

                                // Turn towards and attack the target enemy
                                player.unitActionHandler.AttackTarget();
                                return;
                            }
                        }
                        // If the player has a ranged weapon equipped and the target enemy is within attack range
                        else if (player.UnitEquipment.RangedWeaponEquipped() && player.UnitEquipment.HasValidAmmunitionEquipped())
                        {
                            // Do nothing if the target unit is dead
                            if (unitAtGridPosition.health.IsDead())
                                return;

                            // If the target is in shooting range
                            if (player.unitActionHandler.GetAction<ShootAction>().IsInAttackRange(unitAtGridPosition, player.GridPosition, mouseGridPosition))
                            {
                                // Turn towards and attack the target enemy
                                player.unitActionHandler.AttackTarget();
                                return;
                            }
                        }
                        // If the player has a melee weapon equipped or is unarmed and the target enemy is within attack range
                        else if (player.UnitEquipment.MeleeWeaponEquipped() || player.UnitEquipment.IsUnarmed() || (player.UnitEquipment.RangedWeaponEquipped() && player.UnitEquipment.HasValidAmmunitionEquipped() == false))
                        {
                            // Do nothing if the target unit is dead
                            if (unitAtGridPosition.health.IsDead())
                                return;

                            // If the target is in attack range
                            if (player.unitActionHandler.GetAction<MeleeAction>().IsInAttackRange(unitAtGridPosition, player.GridPosition, mouseGridPosition))
                            {
                                // If the target enemy unit is already completely surrounded by other units or other obstructions
                                float attackRange = player.GetAttackRange(unitAtGridPosition, false);
                                if (unitAtGridPosition.IsCompletelySurrounded(attackRange) && attackRange < 2f)
                                {
                                    // Remove the unit as the target enemy
                                    player.unitActionHandler.SetTargetEnemyUnit(null);
                                    return;
                                }

                                // Turn towards and attack the target enemy
                                player.unitActionHandler.AttackTarget();
                                return;
                            }
                        }

                        //////////////////////////////////////////////////////////////////////////////////////////////////////////////
                        // The target enemy wasn't in attack range, so find and move to the nearest melee or ranged attack position //
                        //////////////////////////////////////////////////////////////////////////////////////////////////////////////

                        // If the player has a Move, Melee, or Shoot Action selected
                        if (selectedAction.IsDefaultAttackAction() || selectedAction is MoveAction)
                        {
                            // If the player has a ranged weapon equipped, find the nearest possible Shoot Action attack position
                            if (player.UnitEquipment.RangedWeaponEquipped() && player.UnitEquipment.HasValidAmmunitionEquipped() && selectedAction is MeleeAction == false)
                            {
                                if (player.unitActionHandler.moveAction.canMove)
                                    player.unitActionHandler.moveAction.QueueAction(player.unitActionHandler.GetAction<ShootAction>().GetNearestAttackPosition(player.GridPosition, unitAtGridPosition));
                                else
                                    Debug.Log("You cannot move...");
                            }
                            // If the player has a melee weapon equipped or is unarmed, find the nearest possible Melee Action attack position
                            else if (player.UnitEquipment.MeleeWeaponEquipped() || player.stats.CanFightUnarmed)
                            {
                                if (player.unitActionHandler.moveAction.canMove)
                                    player.unitActionHandler.moveAction.QueueAction(player.unitActionHandler.GetAction<MeleeAction>().GetNearestAttackPosition(player.GridPosition, unitAtGridPosition));
                                else
                                    Debug.Log("You cannot move...");
                            }
                            else
                            {
                                player.unitActionHandler.SetTargetEnemyUnit(null);
                                player.unitActionHandler.SetDefaultSelectedAction();
                                return;
                            }
                        }
                        else // The player either doesn't have an attack action selected or has a non-default attack action selected
                            player.unitActionHandler.SetTargetEnemyUnit(null);
                    }
                    else // The unit the mouse is hovering over is not an attackable unit (likely an ally or a dead unit) or the Player doesn't have enough energy for the selected action
                    {
                        // Set the selected action to Move if the Player doesn't have enough energy for their selected action
                        if (player.stats.HasEnoughEnergy(selectedAction.GetEnergyCost()) == false)
                            player.unitActionHandler.SetDefaultSelectedAction();

                        player.unitActionHandler.SetTargetEnemyUnit(null);
                    }
                }
                // If there's no unit or a dead unit at the mouse position, but the player is still trying to attack this position (probably trying to use a multi-tile attack)
                else if (selectedAction is BaseAttackAction)
                {
                    // Make sure the Player has enough energy for the attack
                    if (player.stats.HasEnoughEnergy(selectedAction.GetEnergyCost()) == false)
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
                    if (unitAtGridPosition != null && TacticsPathfindingUtilities.CalculateWorldSpaceDistance_XZ(player.GridPosition, unitAtGridPosition.GridPosition) <= LevelGrid.diaganolDistance && player.vision.IsInLineOfSight_Raycast(unitAtGridPosition))
                        player.unitActionHandler.turnAction.QueueAction(mouseGridPosition);
                    else
                    {
                        if (player.unitActionHandler.moveAction.canMove)
                            player.unitActionHandler.moveAction.QueueAction(mouseGridPosition);
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
                highlightedUnit = null;
                ClearHighlightedInteractable();
                WorldMouse.ChangeCursor(CursorState.Default);
                return;
            }

            BaseAction selectedAction = player.unitActionHandler.selectedActionType.GetAction(player);
            if (selectedAction != null)
            {
                if (selectedAction is MoveAction)
                {
                    if (Physics.Raycast(Camera.main.ScreenPointToRay(Input.mousePosition), out RaycastHit interactableHit, 1000, interactableMask))
                    {
                        highlightedUnit = null;
                        if (highlightedInteractable == null || highlightedInteractable.gameObject != interactableHit.transform.gameObject)
                        {
                            if (interactableHit.transform.TryGetComponent(out Interactable interactable))
                            {
                                highlightedInteractable = interactable;
                                if (interactable is LooseItem)
                                {
                                    LooseItem looseItem = interactable as LooseItem;
                                    TooltipManager.ShowLooseItemTooltip(looseItem, looseItem.ItemData);
                                }
                            }
                        }

                        if (highlightedInteractable == null)
                            WorldMouse.ChangeCursor(CursorState.Default);
                        else if (highlightedInteractable is LooseItem)
                        {
                            if (highlightedInteractable is LooseContainerItem)
                            {
                                LooseContainerItem highlightedContainer = highlightedInteractable as LooseContainerItem;
                                if (highlightedContainer.ContainerInventoryManager != null && highlightedContainer.ContainerInventoryManager.ContainsAnyItems())
                                    WorldMouse.ChangeCursor(CursorState.LootBag);
                                else
                                    WorldMouse.ChangeCursor(CursorState.PickupItem);
                            }
                            else
                                WorldMouse.ChangeCursor(CursorState.PickupItem);

                        }
                        else if (highlightedInteractable is Door)
                            WorldMouse.ChangeCursor(CursorState.UseDoor);
                    }
                    else if (Physics.Raycast(Camera.main.ScreenPointToRay(Input.mousePosition), out RaycastHit unitHit, 1000, unitMask))
                    {
                        ClearHighlightedInteractable();
                        if (highlightedUnit == null || highlightedUnit.gameObject != unitHit.transform.gameObject)
                        {
                            if (unitHit.transform.TryGetComponent(out Unit unit))
                                highlightedUnit = unit;
                        }
                        if (highlightedUnit == null)
                            WorldMouse.ChangeCursor(CursorState.Default);
                        else if (highlightedUnit.health.IsDead() && player.vision.IsVisible(highlightedUnit))
                            WorldMouse.ChangeCursor(CursorState.LootBag);
                        else if (player.alliance.IsEnemy(highlightedUnit) && player.vision.IsVisible(highlightedUnit))
                            SetAttackCursor();
                        else
                            WorldMouse.ChangeCursor(CursorState.Default);
                    }
                    else
                    {
                        Unit unitAtGridPosition = LevelGrid.GetUnitAtGridPosition(WorldMouse.CurrentGridPosition());
                        if (unitAtGridPosition != null && player.vision.IsVisible(unitAtGridPosition) && (player.alliance.IsEnemy(unitAtGridPosition) || selectedAction.IsDefaultAttackAction()))
                        {
                            ClearHighlightedInteractable();
                            SetAttackCursor();
                        }
                        else
                        {
                            highlightedUnit = null;
                            ClearHighlightedInteractable();
                            WorldMouse.ChangeCursor(CursorState.Default);
                        }
                    }

                    StartCoroutine(ActionLineRenderer.Instance.DrawMovePath());
                }
                else if (selectedAction is BaseAttackAction)
                {
                    ClearHighlightedInteractable();
                    Unit unitAtGridPosition = LevelGrid.GetUnitAtGridPosition(WorldMouse.CurrentGridPosition());
                    highlightedUnit = unitAtGridPosition;

                    if (unitAtGridPosition != null && unitAtGridPosition.health.IsDead() == false && player.alliance.IsAlly(unitAtGridPosition) == false && player.vision.IsVisible(unitAtGridPosition))
                    {
                        StartCoroutine(ActionLineRenderer.Instance.DrawMovePath());
                        SetAttackCursor();
                    }
                    else
                    {
                        ActionLineRenderer.Instance.HideLineRenderers();
                        WorldMouse.ChangeCursor(CursorState.Default);
                    }
                }
                else if (selectedAction is TurnAction)
                {
                    highlightedUnit = null;
                    ClearHighlightedInteractable();

                    player.unitActionHandler.turnAction.SetTargetPosition(player.unitActionHandler.turnAction.DetermineTargetTurnDirection(LevelGrid.GetGridPosition(WorldMouse.GetPosition())));
                    ActionLineRenderer.Instance.DrawTurnArrow(player.unitActionHandler.turnAction.targetPosition);
                    WorldMouse.ChangeCursor(CursorState.Default);
                }
            }
        }

        void ClearHighlightedInteractable()
        {
            if (highlightedInteractable != null)
            {
                highlightedInteractable = null;
                if (GameControls.gamePlayActions.showLooseItemTooltips.IsPressed == false)
                    TooltipManager.ClearTooltips();
            }
        }

        void SetAttackCursor()
        {
            if (player.UnitEquipment.RangedWeaponEquipped() && player.UnitEquipment.HasValidAmmunitionEquipped() && player.unitActionHandler.SelectedAction is MeleeAction == false)
                WorldMouse.ChangeCursor(CursorState.RangedAttack);
            else if (player.UnitEquipment.MeleeWeaponEquipped() || player.stats.CanFightUnarmed)
                WorldMouse.ChangeCursor(CursorState.MeleeAttack);
            else
                WorldMouse.ChangeCursor(CursorState.Default);
        }

        bool AttackActionSelected() => player.unitActionHandler.SelectedAction is BaseAttackAction;

        public void SetAutoAttack(bool shouldAutoAttack) => autoAttack = shouldAutoAttack;
    }
}
