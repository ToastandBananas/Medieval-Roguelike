using Pathfinding;
using System.Collections.Generic;
using UnityEngine;

public class Unit : MonoBehaviour
{
    [Header("Unit Info")]
    [SerializeField] float shoulderHeight = 0.25f;

    [Header("Inventories")]
    [SerializeField] Inventory myPocketsInventory;
    [SerializeField] Inventory myBackpackInventory;
    [SerializeField] CharacterEquipment myCharacterEquipment;

    public bool isMyTurn { get; private set; }
    public bool hasStartedTurn { get; private set; }

    public GridPosition gridPosition { get; private set; }

    public SingleNodeBlocker singleNodeBlocker { get; private set; }

    public Alliance alliance { get; private set; }
    public HealthSystem health { get; private set; }
    public Hearing hearing { get; private set; }
    public StateController stateController { get; private set; }
    public Stats stats { get; private set; }
    public UnitActionHandler unitActionHandler { get; private set; }
    public UnitAnimator unitAnimator { get; private set; }
    public UnitMeshManager unitMeshManager { get; private set; }
    public Vision vision { get; private set; }

    void Awake()
    {
        // Center the Unit's position on whatever tile they're on
        CenterPosition();
        
        singleNodeBlocker = GetComponent<SingleNodeBlocker>();
        alliance = GetComponent<Alliance>();
        health = GetComponent<HealthSystem>();
        hearing = GetComponentInChildren<Hearing>();
        stateController = GetComponent<StateController>();
        stats = GetComponent<Stats>();
        unitActionHandler = GetComponent<UnitActionHandler>();
        unitAnimator = GetComponentInChildren<UnitAnimator>();
        unitMeshManager = GetComponent<UnitMeshManager>();
        vision = GetComponentInChildren<Vision>();
    }

    void Start()
    {
        singleNodeBlocker.manager = LevelGrid.Instance.GetBlockManager();
        LevelGrid.Instance.AddSingleNodeBlockerToList(singleNodeBlocker, LevelGrid.Instance.GetUnitSingleNodeBlockerList());

        if (IsNPC())
        {
            BlockCurrentPosition();
            unitMeshManager.HideMeshRenderers();
        }

        gridPosition = LevelGrid.GetGridPosition(transform.position);
        LevelGrid.Instance.AddUnitAtGridPosition(gridPosition, this);
    }

    // Used for debugging
    /*void Update()
    {
        if (isMyTurn && unitActionHandler.isPerformingAction == false)
            unitActionHandler.TakeTurn();
    }*/

    public void UpdateGridPosition()
    {
        GridPosition newGridPosition = LevelGrid.GetGridPosition(transform.position);
        if (newGridPosition != gridPosition)
        {
            // Unit changed Grid Position
            GridPosition oldGridPosition = gridPosition;
            gridPosition = newGridPosition;
            LevelGrid.Instance.UnitMovedGridPosition(this, oldGridPosition, newGridPosition);
        }
    }

    public bool IsCompletelySurrounded(float range)
    {
        List<GridPosition> surroundingGridPositions = LevelGrid.Instance.GetSurroundingGridPositions(gridPosition, range, false);
        for (int i = 0; i < surroundingGridPositions.Count; i++)
        {
            if (LevelGrid.Instance.GridPositionObstructed(surroundingGridPositions[i]) == false)
                return false;
        }
        return true;
    }

    public float GetAttackRange(bool accountForHeight)
    {
        if (myCharacterEquipment.RangedWeaponEquipped())
            return unitMeshManager.GetRangedWeapon().MaxRange(gridPosition, unitActionHandler.targetAttackGridPosition, accountForHeight);
        else if (myCharacterEquipment.MeleeWeaponEquipped())
            return unitMeshManager.GetPrimaryMeleeWeapon().MaxRange(gridPosition, unitActionHandler.targetAttackGridPosition, accountForHeight);
        else
            return unitActionHandler.GetAction<MeleeAction>().UnarmedAttackRange(unitActionHandler.targetAttackGridPosition, accountForHeight);
    }

    public void BlockCurrentPosition() => singleNodeBlocker.BlockAtCurrentPosition();

    public void UnblockCurrentPosition() => singleNodeBlocker.Unblock();

    public void BlockAtPosition(Vector3 position) => singleNodeBlocker.BlockAt(position);

    public bool IsNPC() => gameObject.CompareTag("Player") == false;

    public bool IsPlayer() => gameObject.CompareTag("Player");

    public void SetIsMyTurn(bool isMyTurn) 
    {
        this.isMyTurn = isMyTurn;
        if (IsPlayer())
            GridSystemVisual.UpdateGridVisual();
    }

    public void SetHasStartedTurn(bool hasStartedTurn) => this.hasStartedTurn = hasStartedTurn;

    public Vector3 WorldPosition() => LevelGrid.GetWorldPosition(gridPosition);

    public float ShoulderHeight() => shoulderHeight;

    public void CenterPosition() => transform.position = LevelGrid.GetGridPosition(transform.position).WorldPosition();

    public Inventory BackpackInventory() => myBackpackInventory;

    public Inventory PocketsInventory() => myBackpackInventory;

    public CharacterEquipment CharacterEquipment() => myCharacterEquipment;

    public bool TryAddItemToInventories(ItemData itemData)
    {
        if (myPocketsInventory == null && myBackpackInventory == null)
            return false;
        return myBackpackInventory.TryAddItem(itemData) || myPocketsInventory.TryAddItem(itemData);
    }
}
