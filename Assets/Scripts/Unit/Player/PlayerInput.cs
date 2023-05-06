using UnityEngine;
using UnityEngine.EventSystems;

public class PlayerInput : MonoBehaviour
{
    public static PlayerInput Instance;

    [SerializeField] LayerMask interactableMask;

    public Interactable highlightedInteractable { get; private set; }
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
        player.SetIsMyTurn(true);
    }

    void Update()
    {
        if (skipTurnCooldownTimer < skipTurnCooldown)
            skipTurnCooldownTimer += Time.deltaTime;

        if (EventSystem.current.IsPointerOverGameObject())
        {
            ActionLineRenderer.Instance.HideLineRenderers();
            WorldMouse.ChangeCursor(CursorState.Default);
            return;
        }

        if (player.health.IsDead() == false)
        {
            // If the player was holding the button for turn mode (the Turn Action) and then they release it
            if (GameControls.gamePlayActions.turnMode.WasReleased && player.unitActionHandler.selectedAction == player.unitActionHandler.GetAction<TurnAction>())
            {
                // Reset the line renderer and go back to the Move Action
                ActionLineRenderer.Instance.ResetCurrentPositions();
                player.unitActionHandler.SetSelectedAction(player.unitActionHandler.GetAction<MoveAction>());
            }
            
            // If the player is trying to cancel their current action
            if (GameControls.gamePlayActions.cancelAction.WasPressed)
            {
                StartCoroutine(player.unitActionHandler.CancelAction());
                ActionLineRenderer.Instance.ResetCurrentPositions();
            }
            // If it's time for the player to choose an action
            else if (player.isMyTurn && player.unitActionHandler.isPerformingAction == false && player.unitActionHandler.GetAction<MoveAction>().isMoving == false)
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

                // If the player has an attack action selected
                if (player.unitActionHandler.selectedAction.IsAttackAction())
                {
                    // Set the target attack grid position to the mouse grid position and update the visuals
                    player.unitActionHandler.SetTargetAttackGridPosition(WorldMouse.GetCurrentGridPosition());
                    GridSystemVisual.UpdateAttackGridVisual();
                }

                // If the player is trying to perform the Turn Action
                if (GameControls.gamePlayActions.turnMode.IsPressed || player.unitActionHandler.selectedAction is TurnAction)
                    HandleTurnMode();
                // If the player selects a grid position to try and perform an action
                else if (GameControls.gamePlayActions.select.WasPressed)
                    HandleActions();
            }
            else if(player.unitActionHandler.queuedAction != null || player.unitActionHandler.GetAction<MoveAction>().isMoving)
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
        TurnAction turnAction = player.unitActionHandler.GetAction<TurnAction>();
        player.unitActionHandler.SetSelectedAction(turnAction);
        turnAction.SetTargetPosition(turnAction.DetermineTargetTurnDirection(WorldMouse.GetCurrentGridPosition()));
        WorldMouse.ChangeCursor(CursorState.Default);

        if (GameControls.gamePlayActions.select.WasPressed && WorldMouse.GetCurrentGridPosition() != player.gridPosition && turnAction.targetDirection != turnAction.currentDirection)
            player.unitActionHandler.QueueAction(turnAction);
    }

    void HandleActions()
    {
        GridPosition mouseGridPosition = WorldMouse.GetCurrentGridPosition();
        player.unitActionHandler.SetTargetInteractable(null);

        // If the mouse is hovering over an Interactable
        if (highlightedInteractable != null)
        {
            // Set the target Interactable
            player.unitActionHandler.SetTargetInteractable(highlightedInteractable);

            // If the player is too far away from the Interactable to interact with it
            if (TacticsPathfindingUtilities.CalculateWorldSpaceDistance_XYZ(player.gridPosition, highlightedInteractable.gridPosition) > LevelGrid.gridSize)
            {
                // Queue a Move Action towards the Interactable
                player.unitActionHandler.SetTargetGridPosition(LevelGrid.Instance.GetNearestSurroundingGridPosition(highlightedInteractable.gridPosition, player.gridPosition, LevelGrid.gridSize));
                player.unitActionHandler.QueueAction(player.unitActionHandler.GetAction<MoveAction>());
            }
            else
            {
                // Queue the Interact Action with the Interactable
                player.unitActionHandler.GetAction<InteractAction>().SetTargetInteractableGridPosition(highlightedInteractable.gridPosition);
                player.unitActionHandler.QueueAction(player.unitActionHandler.GetAction<InteractAction>());
            }
        }
        // Make sure the mouse grid position is a valid position to perform an action
        else if (mouseGridPosition != player.gridPosition && LevelGrid.IsValidGridPosition(mouseGridPosition) && AstarPath.active.GetNearest(mouseGridPosition.WorldPosition()).node.Walkable)
        {
            Unit unitAtGridPosition = LevelGrid.Instance.GetUnitAtGridPosition(mouseGridPosition);
            bool unitIsVisible = true;
            if (unitAtGridPosition != null)
                unitIsVisible = player.vision.IsVisible(unitAtGridPosition);

            // If the mouse is hovering over a unit that's in the player's Vision
            if (unitAtGridPosition != null && unitAtGridPosition.health.IsDead() == false && unitIsVisible)
            {
                // If the unit is someone the player can attack (an enemy, or a neutral unit, but only if we have an attack action selected)
                if (player.alliance.IsEnemy(unitAtGridPosition) || (player.alliance.IsNeutral(unitAtGridPosition) && player.unitActionHandler.selectedAction.IsAttackAction()))
                {
                    // Set the Unit as the target enemy
                    player.unitActionHandler.SetTargetEnemyUnit(unitAtGridPosition);

                    // If the player has an attack action selected
                    if (player.unitActionHandler.selectedAction.IsAttackAction())
                    {
                        // If the target is in attack range
                        if (player.unitActionHandler.selectedAction.IsInAttackRange(unitAtGridPosition))
                        {
                            // If the target enemy unit is already completely surrounded by other units or other obstructions
                            if (unitAtGridPosition.IsCompletelySurrounded(player.GetAttackRange(false)) && player.GetAttackRange(false) < 2f)
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
                    // If the player has a melee weapon equipped or is unarmed and the target enemy is within attack range
                    else if (player.MeleeWeaponEquipped() || player.IsUnarmed())
                    {
                        // Do nothing if the target unit is dead
                        if (unitAtGridPosition.health.IsDead())
                            return;

                        // If the target is in attack range
                        if (player.unitActionHandler.GetAction<MeleeAction>().IsInAttackRange(unitAtGridPosition))
                        {
                            // If the target enemy unit is already completely surrounded by other units or other obstructions
                            if (unitAtGridPosition.IsCompletelySurrounded(player.GetAttackRange(false)) && player.GetAttackRange(false) < 2f)
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
                    else if (player.RangedWeaponEquipped())
                    {
                        // Do nothing if the target unit is dead
                        if (unitAtGridPosition.health.IsDead())
                            return;

                        // If the target is in shooting range
                        if (player.unitActionHandler.GetAction<ShootAction>().IsInAttackRange(unitAtGridPosition)) 
                        {
                            // Turn towards and attack the target enemy
                            player.unitActionHandler.AttackTarget();
                            return;
                        }
                    }

                    //////////////////////////////////////////////////////////////////////////////////////////////////////////////
                    // The target enemy wasn't in attack range, so find and move to the nearest melee or ranged attack position //
                    //////////////////////////////////////////////////////////////////////////////////////////////////////////////

                    // If the player doesn't have an attack action already selected, we will just default to either the MeleeAction or ShootAction (or if one of these actions is already selected)
                    if (player.unitActionHandler.selectedAction.IsAttackAction() == false || player.unitActionHandler.selectedAction.IsDefaultAttackAction())
                    {
                        // If the player has a ranged weapon equipped, find the nearest Shoot Action attack position
                        if (player.RangedWeaponEquipped())
                            player.unitActionHandler.SetTargetGridPosition(player.unitActionHandler.GetAction<ShootAction>().GetNearestAttackPosition(player.gridPosition, unitAtGridPosition));
                        else // If the player has a melee weapon equipped or is unarmed, find the nearest Melee Action attack position
                            player.unitActionHandler.SetTargetGridPosition(player.unitActionHandler.GetAction<MeleeAction>().GetNearestAttackPosition(player.gridPosition, unitAtGridPosition));

                        // Set the target attack position to the target unit's position
                        player.unitActionHandler.SetTargetAttackGridPosition(unitAtGridPosition.gridPosition);

                        // Move towards the nearest attack position
                        player.unitActionHandler.QueueAction(player.unitActionHandler.GetAction<MoveAction>());
                    }
                    else // The player either doesn't have an attack action selected or has a non-default attack action selected
                        player.unitActionHandler.SetTargetEnemyUnit(null);
                }
                else // The unit the mouse is hovering over is not an attackable unit (likely an ally or a dead unit)
                    player.unitActionHandler.SetTargetEnemyUnit(null);
            }
            // If there's no unit at the mouse position, but the player is still trying to attack this position (probably trying to use a multi-tile attack)
            else if (player.unitActionHandler.selectedAction.IsAttackAction())
            {
                // If there's any enemy or neutral unit within the attack positions
                if (player.unitActionHandler.selectedAction.IsValidUnitInActionArea(player.unitActionHandler.targetAttackGridPosition))
                {
                    // Turn towards and attack the target enemy
                    player.unitActionHandler.SetQueuedAttack(player.unitActionHandler.selectedAction);
                    player.unitActionHandler.AttackTarget();
                    return;
                }
            }
            else if (player.unitActionHandler.selectedAction is MoveAction)
            {
                player.unitActionHandler.SetTargetEnemyUnit(null);
                player.unitActionHandler.SetTargetGridPosition(mouseGridPosition);
                player.unitActionHandler.QueueAction(player.unitActionHandler.GetAction<MoveAction>());
            }
        }
        // If the player is trying to perform an action on themselves
        else if (mouseGridPosition == player.gridPosition)
        {
            // TODO: Implement actions that can be performed on oneself (such as healing or a buff)
        }
    }

    void SetupCursorAndLineRenderer()
    {
        if (player.unitActionHandler.selectedAction != null)
        {
            if (player.unitActionHandler.selectedAction is MoveAction)
            {
                Unit unitAtGridPosition = LevelGrid.Instance.GetUnitAtGridPosition(WorldMouse.GetCurrentGridPosition()); 
                if (Physics.Raycast(Camera.main.ScreenPointToRay(Input.mousePosition), out RaycastHit hit, 1000, interactableMask))
                {
                    highlightedInteractable = LevelGrid.Instance.GetInteractableFromTransform(hit.transform.parent);
                    if (highlightedInteractable != null && highlightedInteractable is Door)
                        WorldMouse.ChangeCursor(CursorState.UseDoor);
                }
                else if (unitAtGridPosition != null && unitAtGridPosition.health.IsDead() == false && player.alliance.IsEnemy(unitAtGridPosition) && player.vision.IsVisible(unitAtGridPosition))
                {
                    highlightedInteractable = null;
                    if (player.RangedWeaponEquipped())
                        WorldMouse.ChangeCursor(CursorState.RangedAttack);
                    else
                        WorldMouse.ChangeCursor(CursorState.MeleeAttack);
                }
                else
                {
                    highlightedInteractable = null;
                    WorldMouse.ChangeCursor(CursorState.Default);
                }

                StartCoroutine(ActionLineRenderer.Instance.DrawMovePath());
            }
            else if (player.unitActionHandler.selectedAction.IsAttackAction())
            {
                highlightedInteractable = null;
                Unit unitAtGridPosition = LevelGrid.Instance.GetUnitAtGridPosition(WorldMouse.GetCurrentGridPosition());
                if (unitAtGridPosition != null && unitAtGridPosition.health.IsDead() == false && player.alliance.IsAlly(unitAtGridPosition) == false && player.vision.IsVisible(unitAtGridPosition))
                {
                    StartCoroutine(ActionLineRenderer.Instance.DrawMovePath());
                    if (player.RangedWeaponEquipped())
                        WorldMouse.ChangeCursor(CursorState.RangedAttack);
                    else
                        WorldMouse.ChangeCursor(CursorState.MeleeAttack);
                }
                else
                {
                    ActionLineRenderer.Instance.HideLineRenderers();
                    WorldMouse.ChangeCursor(CursorState.Default);
                }
            }
            else if (player.unitActionHandler.selectedAction is TurnAction)
            {
                highlightedInteractable = null;
                ActionLineRenderer.Instance.DrawTurnArrow(player.unitActionHandler.GetAction<TurnAction>().targetPosition);
                WorldMouse.ChangeCursor(CursorState.Default);
            }
        }
    }

    bool AttackActionSelected() => player.unitActionHandler.selectedAction.IsAttackAction();

    public void SetAutoAttack(bool shouldAutoAttack) => autoAttack = shouldAutoAttack;
}
