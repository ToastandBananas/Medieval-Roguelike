using Pathfinding;
using UnityEngine;

public class Unit : MonoBehaviour
{
    [Header("Unit Info")]
    [SerializeField] float shoulderHeight = 0.25f;
    [SerializeField] Transform leftHeldItemParent, rightHeldItemParent;
    [SerializeField] MeshRenderer[] meshRenderers;
    MeshRenderer leftHeldItemMeshRenderer, rightHeldItemMeshRenderer;
    MeshRenderer[] bowMeshRenderers;
    LineRenderer bowLineRenderer;

    public bool isMyTurn { get; private set; }
    public bool hasStartedTurn { get; private set; }
    public bool isDead { get; private set; }

    public GridPosition gridPosition { get; private set; }

    SingleNodeBlocker singleNodeBlocker;

    public Alliance alliance { get; private set; }
    public StateController stateController { get; private set; }
    public Stats stats { get; private set; }
    public UnitActionHandler unitActionHandler { get; private set; }
    public UnitAnimator unitAnimator { get; private set; }
    public Vision vision { get; private set; }

    void Awake()
    {
        // Center the Unit's position on whatever tile they're on
        CenterPosition();

        singleNodeBlocker = GetComponent<SingleNodeBlocker>();
        alliance = GetComponent<Alliance>();
        stateController = GetComponent<StateController>();
        stats = GetComponent<Stats>();
        unitActionHandler = GetComponent<UnitActionHandler>();
        unitAnimator = GetComponent<UnitAnimator>();
        vision = GetComponentInChildren<Vision>();

        if (leftHeldItemParent.childCount > 0)
        {
            if (leftHeldItemParent.GetChild(0).childCount > 0) // The item is a Bow
            {
                bowMeshRenderers = leftHeldItemParent.GetComponentsInChildren<MeshRenderer>();
                bowLineRenderer = leftHeldItemParent.GetComponentInChildren<LineRenderer>();
            }
            else
                leftHeldItemMeshRenderer = leftHeldItemParent.GetComponentInChildren<MeshRenderer>();
        }

        if (rightHeldItemParent.childCount > 0)
            rightHeldItemMeshRenderer = rightHeldItemParent.GetComponentInChildren<MeshRenderer>();
    }

    void Start()
    {
        singleNodeBlocker.manager = LevelGrid.Instance.GetBlockManager();
        LevelGrid.Instance.AddSingleNodeBlockerToList(singleNodeBlocker, LevelGrid.Instance.GetUnitSingleNodeBlockerList());

        if (IsNPC())
        {
            BlockCurrentPosition();
            HideMeshRenderers();
        }

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

    public void ShowMeshRenderers()
    {
        for (int i = 0; i < meshRenderers.Length; i++)
        {
            meshRenderers[i].shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
        }

        if (leftHeldItemMeshRenderer != null)
            leftHeldItemMeshRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;

        if (rightHeldItemMeshRenderer != null)
            rightHeldItemMeshRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;

        for (int i = 0; i < bowMeshRenderers.Length; i++)
        {
            bowMeshRenderers[i].shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
        }

        if (bowLineRenderer != null)
            bowLineRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
    }

    public void HideMeshRenderers()
    {
        for (int i = 0; i < meshRenderers.Length; i++)
        {
            meshRenderers[i].shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.ShadowsOnly;
        }

        if (leftHeldItemMeshRenderer != null)
            leftHeldItemMeshRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.ShadowsOnly;

        if (rightHeldItemMeshRenderer != null)
            rightHeldItemMeshRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.ShadowsOnly;

        for (int i = 0; i < bowMeshRenderers.Length; i++)
        {
            bowMeshRenderers[i].shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.ShadowsOnly;
        }

        if (bowLineRenderer != null)
            bowLineRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.ShadowsOnly;
    }

    public void BlockCurrentPosition() => singleNodeBlocker.BlockAtCurrentPosition();

    public void UnblockCurrentPosition() => singleNodeBlocker.Unblock();

    public void BlockAtPosition(Vector3 position) => singleNodeBlocker.BlockAt(position);

    public bool IsNPC() => gameObject.CompareTag("Player") == false;

    public bool IsPlayer() => gameObject.CompareTag("Player");

    public bool IsVisibleOnScreen() => meshRenderers[0].isVisible;

    public void SetIsMyTurn(bool isMyTurn) => this.isMyTurn = isMyTurn;

    public void SetHasStartedTurn(bool hasStartedTurn) => this.hasStartedTurn = hasStartedTurn;

    public bool SetIsDead(bool isDead) => this.isDead = isDead;

    public Vector3 WorldPosition() => LevelGrid.Instance.GetWorldPosition(gridPosition);

    public float ShoulderHeight() => shoulderHeight;

    public void CenterPosition() => transform.position = LevelGrid.Instance.GetGridPosition(transform.position).WorldPosition();
}
