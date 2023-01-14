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

    StateController stateController;
    Stats stats;
    UnitActionHandler unitActionHandler;

    void Start()
    {
        gm = GameManager.Instance;

        stateController = GetComponent<StateController>();
        stats = GetComponent<Stats>();
        unitActionHandler = GetComponent<UnitActionHandler>();

        gridPosition = LevelGrid.Instance.GetGridPosition(transform.position);
        LevelGrid.Instance.AddUnitAtGridPosition(gridPosition, this);
    }

    public bool IsNPC() => gameObject.CompareTag("Player") == false;

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

    public float ShoulderHeight() => shoulderHeight;

    public LayerMask ActionObstaclesMask() => actionObstaclesMask;
}
