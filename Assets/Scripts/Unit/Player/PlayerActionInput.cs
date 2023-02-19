using UnityEngine;
using UnityEngine.EventSystems;

public class PlayerActionInput : MonoBehaviour
{
    public static PlayerActionInput Instance;

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
            if (GameControls.gamePlayActions.turnMode.WasReleased && player.unitActionHandler.selectedAction == player.unitActionHandler.GetAction<TurnAction>())
            {
                ActionLineRenderer.Instance.ResetCurrentPositions();
                player.unitActionHandler.SetSelectedAction(player.unitActionHandler.GetAction<MoveAction>());
            }
            
            if (GameControls.gamePlayActions.cancelAction.WasPressed)
            {
                StartCoroutine(player.unitActionHandler.CancelAction());
                ActionLineRenderer.Instance.ResetCurrentPositions();
            }
            else if (player.isMyTurn && player.unitActionHandler.isPerformingAction == false && player.unitActionHandler.GetAction<MoveAction>().isMoving == false)
            {
                if (GameControls.gamePlayActions.skipTurn.IsPressed && skipTurnCooldownTimer >= skipTurnCooldown)
                {
                    skipTurnCooldownTimer = 0f;
                    player.unitActionHandler.SkipPlayersTurn();
                }

                if (player.unitActionHandler.selectedAction != null)
                {
                    if (player.unitActionHandler.selectedAction is MoveAction)
                    {
                        Unit unitAtGridPosition = LevelGrid.Instance.GetUnitAtGridPosition(WorldMouse.GetCurrentGridPosition());
                        if (unitAtGridPosition != null && unitAtGridPosition.health.IsDead() == false && player.vision.IsVisible(unitAtGridPosition) && player.alliance.IsEnemy(unitAtGridPosition))
                        {
                            if (player.RangedWeaponEquipped())
                                WorldMouse.ChangeCursor(CursorState.RangedAttack);
                            else
                                WorldMouse.ChangeCursor(CursorState.MeleeAttack);
                        }
                        else
                            WorldMouse.ChangeCursor(CursorState.Default);

                        StartCoroutine(ActionLineRenderer.Instance.DrawMovePath());
                    }
                    else if (player.unitActionHandler.selectedAction is MeleeAction || player.unitActionHandler.selectedAction is ShootAction)
                    {
                        Unit unitAtGridPosition = LevelGrid.Instance.GetUnitAtGridPosition(WorldMouse.GetCurrentGridPosition());
                        if (unitAtGridPosition != null && unitAtGridPosition.health.IsDead() == false && player.vision.IsVisible(unitAtGridPosition) && player.alliance.IsAlly(unitAtGridPosition) == false)
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
                        ActionLineRenderer.Instance.DrawTurnArrow(player.unitActionHandler.GetAction<TurnAction>().targetPosition);
                }

                if (GameControls.gamePlayActions.turnMode.IsPressed || player.unitActionHandler.selectedAction is TurnAction)
                {
                    TurnAction turnAction = player.unitActionHandler.GetAction<TurnAction>();
                    player.unitActionHandler.SetSelectedAction(turnAction);
                    turnAction.SetTargetPosition(turnAction.DetermineTargetTurnDirection(WorldMouse.GetCurrentGridPosition()));
                    WorldMouse.ChangeCursor(CursorState.Default);

                    if (GameControls.gamePlayActions.select.WasPressed && turnAction.targetDirection != turnAction.currentDirection)
                        player.unitActionHandler.QueueAction(turnAction);
                }
                else if (GameControls.gamePlayActions.select.WasPressed)
                {
                    GridPosition mouseGridPosition = WorldMouse.GetCurrentGridPosition();
                    if (mouseGridPosition != player.gridPosition && LevelGrid.Instance.IsValidGridPosition(mouseGridPosition) && AstarPath.active.GetNearest(mouseGridPosition.WorldPosition()).node.Walkable)
                    {
                        Unit unitAtGridPosition = LevelGrid.Instance.GetUnitAtGridPosition(mouseGridPosition);
                        bool unitIsVisible = true;
                        if (unitAtGridPosition != null)
                            unitIsVisible = player.vision.IsVisible(unitAtGridPosition);

                        if (unitAtGridPosition != null && unitIsVisible)
                        {
                            if (unitAtGridPosition.health.IsDead() == false && (player.alliance.IsEnemy(unitAtGridPosition) || (player.alliance.IsNeutral(unitAtGridPosition) && (player.unitActionHandler.selectedAction is MeleeAction || player.unitActionHandler.selectedAction is ShootAction))))
                            {
                                player.unitActionHandler.SetTargetEnemyUnit(unitAtGridPosition);
                                if ((player.MeleeWeaponEquipped() || player.IsUnarmed()) && player.unitActionHandler.GetAction<MeleeAction>().IsInAttackRange(unitAtGridPosition))
                                {
                                    if (unitAtGridPosition.IsCompletelySurrounded())
                                    {
                                        player.unitActionHandler.SetTargetEnemyUnit(null);
                                        return;
                                    }

                                    player.unitActionHandler.AttackTargetEnemy();
                                    return;
                                }
                                else if (player.RangedWeaponEquipped() && player.unitActionHandler.GetAction<ShootAction>().IsInAttackRange(unitAtGridPosition))
                                {
                                    player.unitActionHandler.AttackTargetEnemy();
                                    return;
                                }

                                if (player.RangedWeaponEquipped())
                                    player.unitActionHandler.SetTargetGridPosition(player.unitActionHandler.GetAction<ShootAction>().GetNearestShootPosition(player.gridPosition, unitAtGridPosition.gridPosition));
                                else
                                    player.unitActionHandler.SetTargetGridPosition(player.unitActionHandler.GetAction<MeleeAction>().GetNearestMeleePosition(player.gridPosition, unitAtGridPosition.gridPosition));

                                player.unitActionHandler.QueueAction(player.unitActionHandler.GetAction<MoveAction>());
                            }
                            else
                                player.unitActionHandler.SetTargetEnemyUnit(null);
                        }
                        else if (player.unitActionHandler.selectedAction is MoveAction)
                        {
                            player.unitActionHandler.SetTargetEnemyUnit(null); 
                            player.unitActionHandler.SetTargetGridPosition(mouseGridPosition);
                            player.unitActionHandler.QueueAction(player.unitActionHandler.GetAction<MoveAction>());
                        }
                    }
                }
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

    public void SetAutoAttack(bool shouldAutoAttack) => autoAttack = shouldAutoAttack;
}
