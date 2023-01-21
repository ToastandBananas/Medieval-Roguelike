using Pathfinding;
using UnityEngine;

public class Unit : MonoBehaviour
{
    [Header("Unit Info")]
    [SerializeField] float shoulderHeight = 0.25f;
    [SerializeField] public LayerMask actionObstaclesMask { get; private set; }

    public bool isMyTurn { get; private set; }
    public bool isDead { get; private set; }

    public GridPosition gridPosition { get; private set; }

    SingleNodeBlocker singleNodeBlocker;
    public StateController stateController { get; private set; }
    public Stats stats { get; private set; }
    public UnitActionHandler unitActionHandler { get; private set; }
    public UnitAnimator unitAnimator { get; private set; }

    public MeshRenderer unitBaseMeshRenderer { get; private set; }

    void Awake()
    {
        singleNodeBlocker = GetComponent<SingleNodeBlocker>();
        stateController = GetComponent<StateController>();
        stats = GetComponent<Stats>();
        unitActionHandler = GetComponent<UnitActionHandler>();
        unitAnimator = GetComponent<UnitAnimator>();

        unitBaseMeshRenderer = transform.GetChild(0).GetComponent<MeshRenderer>();
    }

    void Start()
    {
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

    public void BlockAtPosition(Vector3 position) => singleNodeBlocker.BlockAt(position);

    public bool IsNPC() => gameObject.CompareTag("Player") == false;

    public bool IsPlayer() => gameObject.CompareTag("Player");

    public void SetIsMyTurn(bool isMyTurn) => this.isMyTurn = isMyTurn;

    public bool SetIsDead(bool isDead) => this.isDead = isDead;

    public Vector3 WorldPosition() => LevelGrid.Instance.GetWorldPosition(gridPosition);

    public float ShoulderHeight() => shoulderHeight;
}
