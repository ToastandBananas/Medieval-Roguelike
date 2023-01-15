using Pathfinding;
using System.Buffers.Text;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Unit : MonoBehaviour
{
    [Header("Unit Info")]
    [SerializeField] float shoulderHeight = 0.25f;
    [SerializeField] LayerMask actionObstaclesMask;
    [SerializeField] Unit leader;

    bool isMyTurn, isDead;

    GridPosition gridPosition;

    GameManager gm;

    SingleNodeBlocker singleNodeBlocker;
    StateController stateController;
    Stats stats;
    UnitActionHandler unitActionHandler;

    void Awake()
    {
        singleNodeBlocker = GetComponent<SingleNodeBlocker>();
        stateController = GetComponent<StateController>();
        stats = GetComponent<Stats>();
        unitActionHandler = GetComponent<UnitActionHandler>();
    }

    void Start()
    {
        gm = GameManager.Instance;

        singleNodeBlocker.manager = LevelGrid.Instance.GetBlockManager();
        LevelGrid.Instance.AddSingleNodeBlockerToList(singleNodeBlocker, LevelGrid.Instance.GetUnitSingleNodeBlockerList());

        if (IsNPC())
            BlockCurrentPosition();

        gridPosition = LevelGrid.Instance.GetGridPosition(transform.position);
        LevelGrid.Instance.AddUnitAtGridPosition(gridPosition, this);
    }

    public void UpdateGridPosition()
    {
        GridPosition newGridPosition = LevelGrid.Instance.GetGridPosition(transform.position);
        if (newGridPosition != gridPosition)
        {
            // Unit changed Grid Position
            GridPosition oldGridPosition = gridPosition;
            gridPosition = newGridPosition;
            LevelGrid.Instance.UnitMovedGridPosition(this, oldGridPosition, newGridPosition);
        }
    }

    public void BlockCurrentPosition() => singleNodeBlocker.BlockAtCurrentPosition();

    public void UnblockCurrentPosition() => singleNodeBlocker.Unblock();

    public bool IsNPC() => gameObject.CompareTag("Player") == false;

    public bool IsPlayer() => gameObject.CompareTag("Player");

    public bool IsMyTurn() => isMyTurn;

    public void SetIsMyTurn(bool isMyTurn) => this.isMyTurn = isMyTurn;

    public bool IsDead() => isDead;

    public bool SetIsDead(bool isDead) => this.isDead = isDead;

    public Unit Leader() => leader;

    public void SetLeader(Unit newLeader) => leader = newLeader;

    public StateController StateController() => stateController;

    public Stats Stats() => stats;

    public UnitActionHandler UnitActionHandler() => unitActionHandler;

    public Vector3 WorldPosition() => LevelGrid.Instance.GetWorldPosition(gridPosition);

    public GridPosition GridPosition() => gridPosition;

    public float ShoulderHeight() => shoulderHeight;

    public LayerMask ActionObstaclesMask() => actionObstaclesMask;
}
