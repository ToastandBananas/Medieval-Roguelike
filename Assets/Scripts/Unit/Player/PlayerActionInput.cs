using UnityEngine;

public class PlayerActionInput : MonoBehaviour
{
    public static PlayerActionInput Instance;

    public bool autoAttack { get; private set; }

    Unit unit;

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
        unit = GetComponent<Unit>();
        unit.SetIsMyTurn(true);
    }

    void Update()
    {
        if (skipTurnCooldownTimer < skipTurnCooldown)
            skipTurnCooldownTimer += Time.deltaTime;

        if (unit.health.IsDead() == false)
        {
            if (GameControls.gamePlayActions.turnMode.WasReleased && unit.unitActionHandler.selectedAction == unit.unitActionHandler.GetAction<TurnAction>())
            {
                ActionLineRenderer.Instance.ResetCurrentPositions();
                unit.unitActionHandler.SetSelectedAction(unit.unitActionHandler.GetAction<MoveAction>());
            }
            
            if (GameControls.gamePlayActions.cancelAction.WasPressed)
            {
                StartCoroutine(unit.unitActionHandler.CancelAction());
                ActionLineRenderer.Instance.ResetCurrentPositions();
            }
            else if (unit.isMyTurn && unit.unitActionHandler.isPerformingAction == false && unit.unitActionHandler.GetAction<MoveAction>().isMoving == false)
            {
                if (GameControls.gamePlayActions.skipTurn.IsPressed && skipTurnCooldownTimer >= skipTurnCooldown)
                {
                    skipTurnCooldownTimer = 0f;
                    TurnManager.Instance.FinishTurn(unit);
                }

                if (unit.unitActionHandler.selectedAction != null)
                {
                    if (unit.unitActionHandler.selectedAction is MoveAction)
                        StartCoroutine(ActionLineRenderer.Instance.DrawMovePath());
                    else if (unit.unitActionHandler.selectedAction is TurnAction)
                        ActionLineRenderer.Instance.DrawTurnArrow(unit.unitActionHandler.GetAction<TurnAction>().targetPosition);
                }

                if (GameControls.gamePlayActions.turnMode.IsPressed)
                {
                    unit.unitActionHandler.SetSelectedAction(unit.unitActionHandler.GetAction<TurnAction>());
                    unit.unitActionHandler.GetAction<TurnAction>().SetTargetPosition(unit.unitActionHandler.GetAction<TurnAction>().DetermineTargetTurnDirection(LevelGrid.Instance.GetGridPosition(WorldMouse.GetPosition())));

                    if (GameControls.gamePlayActions.select.WasPressed && unit.unitActionHandler.GetAction<TurnAction>().targetDirection != unit.unitActionHandler.GetAction<TurnAction>().currentDirection)
                        unit.unitActionHandler.QueueAction(unit.unitActionHandler.GetAction<TurnAction>());
                }
                else if (GameControls.gamePlayActions.select.WasPressed)
                {
                    GridPosition mouseGridPosition = GetMouseGridPosition();
                    if (mouseGridPosition != unit.gridPosition && LevelGrid.Instance.IsValidGridPosition(mouseGridPosition) && AstarPath.active.GetNearest(mouseGridPosition.WorldPosition()).node.Walkable)
                    {
                        if (LevelGrid.Instance.HasAnyUnitOnGridPosition(mouseGridPosition))
                        {
                            Unit unitAtGridPosition = LevelGrid.Instance.GetUnitAtGridPosition(mouseGridPosition);
                            if (unit.alliance.IsEnemy(unitAtGridPosition.alliance.CurrentFaction()))
                            {
                                unit.unitActionHandler.SetTargetEnemyUnit(unitAtGridPosition);
                                if ((UnitManager.Instance.player.MeleeWeaponEquipped() || UnitManager.Instance.player.IsUnarmed()) && UnitManager.Instance.player.unitActionHandler.GetAction<MeleeAction>().IsInAttackRange(unitAtGridPosition))
                                {
                                    if (unitAtGridPosition.IsCompletelySurrounded())
                                        return;
                                }
                                else if (UnitManager.Instance.player.RangedWeaponEquipped() && UnitManager.Instance.player.unitActionHandler.GetAction<ShootAction>().IsInAttackRange(unitAtGridPosition))
                                {
                                    unit.unitActionHandler.AttackTargetEnemy();
                                    return;
                                }
                            }
                            else
                            {
                                unit.unitActionHandler.SetTargetEnemyUnit(null);
                                return;
                            }
                        }
                        else
                            unit.unitActionHandler.SetTargetEnemyUnit(null);

                        unit.unitActionHandler.SetTargetGridPosition(mouseGridPosition);
                        unit.unitActionHandler.QueueAction(unit.unitActionHandler.GetAction<MoveAction>());
                    }
                }
            }
            else if(unit.unitActionHandler.queuedAction != null || unit.unitActionHandler.GetAction<MoveAction>().isMoving)
            {
                ActionLineRenderer.Instance.HideLineRenderers();
            }
        }
    }

    GridPosition GetMouseGridPosition()
    {
        // Debug.Log("Mouse Grid Position: " + LevelGrid.Instance.GetGridPosition(WorldMouse.GetPosition()));
        return LevelGrid.Instance.GetGridPosition(WorldMouse.GetPosition());
    }

    public void SetAutoAttack(bool shouldAutoAttack) => autoAttack = shouldAutoAttack;
}
