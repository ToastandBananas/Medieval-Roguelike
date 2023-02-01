using Pathfinding;
using System.Collections.Generic;
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
    
    public HeldItem leftHeldItem { get; private set; }
    public HeldItem rightHeldItem { get; private set; }

    public bool isMyTurn { get; private set; }
    public bool hasStartedTurn { get; private set; }

    public GridPosition gridPosition { get; private set; }

    public SingleNodeBlocker singleNodeBlocker { get; private set; }

    public Alliance alliance { get; private set; }
    public HealthSystem health { get; private set; }
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
        health = GetComponent<HealthSystem>();
        stateController = GetComponent<StateController>();
        stats = GetComponent<Stats>();
        unitActionHandler = GetComponent<UnitActionHandler>();
        unitAnimator = GetComponent<UnitAnimator>();
        vision = GetComponentInChildren<Vision>();

        SetLeftHeldItem();
        SetRightHeldItem();

        if (leftHeldItem != null)
        {
            if (leftHeldItem.itemData.item.itemType == ItemType.RangedWeapon) // The item is a Bow
            {
                bowMeshRenderers = leftHeldItem.GetComponentsInChildren<MeshRenderer>();
                bowLineRenderer = leftHeldItem.GetComponentInChildren<LineRenderer>();
            }
            else
                leftHeldItemMeshRenderer = leftHeldItem.GetComponentInChildren<MeshRenderer>();
        }

        if (rightHeldItem != null)
            rightHeldItemMeshRenderer = rightHeldItem.GetComponentInChildren<MeshRenderer>();
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
        if (meshRenderers != null)
        {
            for (int i = 0; i < meshRenderers.Length; i++)
            {
                meshRenderers[i].shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
            }
        }

        if (leftHeldItemMeshRenderer != null)
            leftHeldItemMeshRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;

        if (rightHeldItemMeshRenderer != null)
            rightHeldItemMeshRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;

        if (bowMeshRenderers != null)
        {
            for (int i = 0; i < bowMeshRenderers.Length; i++)
            {
                bowMeshRenderers[i].shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
            }

            if (bowLineRenderer != null)
                bowLineRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        }
    }

    public void HideMeshRenderers()
    {
        if (meshRenderers != null)
        {
            for (int i = 0; i < meshRenderers.Length; i++)
            {
                meshRenderers[i].shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.ShadowsOnly;
            }
        }

        if (leftHeldItemMeshRenderer != null)
            leftHeldItemMeshRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.ShadowsOnly;

        if (rightHeldItemMeshRenderer != null)
            rightHeldItemMeshRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.ShadowsOnly;

        if (bowMeshRenderers != null)
        {
            for (int i = 0; i < bowMeshRenderers.Length; i++)
            {
                bowMeshRenderers[i].shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.ShadowsOnly;
            }

            if (bowLineRenderer != null)
                bowLineRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.ShadowsOnly;
        }
    }

    public bool IsCompletelySurrounded()
    {
        List<GridPosition> surroundingGridPositions = LevelGrid.Instance.GetSurroundingGridPositions(gridPosition);
        for (int i = 0; i < surroundingGridPositions.Count; i++)
        {
            if (LevelGrid.Instance.GridPositionObstructed(surroundingGridPositions[i]) == false)
                return false;
        }
        return true;
    }

    public void SetLeftHeldItem()
    {
        if (leftHeldItemParent.childCount > 0)
        {
            leftHeldItem = leftHeldItemParent.GetChild(0).GetComponent<HeldItem>();
            unitAnimator.SetLeftHeldItemAnim(leftHeldItem.GetComponent<Animator>());
        }
    }

    public void SetRightHeldItem()
    {
        if (rightHeldItemParent.childCount > 0)
        {
            rightHeldItem = rightHeldItemParent.GetChild(0).GetComponent<HeldItem>();
            unitAnimator.SetRightHeldItemAnim(rightHeldItem.GetComponent<Animator>());
        }
    }

    public bool IsDualWielding() => leftHeldItem != null && rightHeldItem != null && leftHeldItem.itemData.item.IsMeleeWeapon() && rightHeldItem.itemData.item.IsMeleeWeapon();

    public bool MeleeWeaponEquipped() => (leftHeldItem != null && leftHeldItem.itemData.item.IsMeleeWeapon()) || (rightHeldItem != null && rightHeldItem.itemData.item.IsMeleeWeapon());

    public bool RangedWeaponEquipped() => leftHeldItem != null && leftHeldItem.itemData.item.IsRangedWeapon();

    public bool IsUnarmed() => leftHeldItem == null && rightHeldItem == null;

    public HeldRangedWeapon GetEquippedRangedWeapon() => leftHeldItem as HeldRangedWeapon;

    public void BlockCurrentPosition() => singleNodeBlocker.BlockAtCurrentPosition();

    public void UnblockCurrentPosition() => singleNodeBlocker.Unblock();

    public void BlockAtPosition(Vector3 position) => singleNodeBlocker.BlockAt(position);

    public bool IsNPC() => gameObject.CompareTag("Player") == false;

    public bool IsPlayer() => gameObject.CompareTag("Player");

    public bool IsVisibleOnScreen() => meshRenderers[0].isVisible && UnitManager.Instance.player.vision.visibleUnits.Contains(this);

    public void SetIsMyTurn(bool isMyTurn) => this.isMyTurn = isMyTurn;

    public void SetHasStartedTurn(bool hasStartedTurn) => this.hasStartedTurn = hasStartedTurn;

    public Vector3 WorldPosition() => LevelGrid.Instance.GetWorldPosition(gridPosition);

    public float ShoulderHeight() => shoulderHeight;

    public void CenterPosition() => transform.position = LevelGrid.Instance.GetGridPosition(transform.position).WorldPosition();
}
