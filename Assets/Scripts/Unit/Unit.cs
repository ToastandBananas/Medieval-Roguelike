using Pathfinding;
using System.Collections.Generic;
using UnityEngine;
using InteractableObjects;
using GridSystem;
using ActionSystem;
using InventorySystem;

namespace UnitSystem
{
    public class Unit : MonoBehaviour
    {
        [Header("Transforms")]
        [SerializeField] Transform actionsParent;

        [Header("Unit Info")]
        [SerializeField] float shoulderHeight = 0.25f;

        [Header("Inventories")]
        [SerializeField] UnitInventoryManager mainInventoryManager;
        [SerializeField] ContainerInventoryManager backpackInventoryManager;
        [SerializeField] ContainerInventoryManager quiverInventoryManager;
        [SerializeField] UnitEquipment myUnitEquipment;

        public bool isMyTurn { get; private set; }
        public bool hasStartedTurn { get; private set; }

        public SingleNodeBlocker singleNodeBlocker { get; private set; }

        public Alliance alliance { get; private set; }
        public Health health { get; private set; }
        public Hearing hearing { get; private set; }
        public Seeker seeker { get; private set; }
        public StateController stateController { get; private set; }
        public Stats stats { get; private set; }
        public UnitActionHandler unitActionHandler { get; private set; }
        public UnitAnimator unitAnimator { get; private set; }
        public UnitInteractable unitInteractable { get; private set; }
        public UnitMeshManager unitMeshManager { get; private set; }
        public Vision vision { get; private set; }

        GridPosition gridPosition;

        void Awake()
        {
            // Center the Unit's position on whatever tile they're on
            CenterPosition();

            singleNodeBlocker = GetComponent<SingleNodeBlocker>();
            alliance = GetComponent<Alliance>();
            if (TryGetComponent(out UnitInteractable deadUnit))
                this.unitInteractable = deadUnit;
            health = GetComponent<Health>();
            hearing = GetComponentInChildren<Hearing>();
            seeker = GetComponent<Seeker>();
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

            if (IsNPC)
            {
                if (health.IsDead())
                {
                    UnblockCurrentPosition();
                    if (unitInteractable != null)
                        unitInteractable.enabled = true;
                }
                else
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

        public float GetAttackRange(Unit targetUnit, bool accountForHeight)
        {
            if (myUnitEquipment.RangedWeaponEquipped() && myUnitEquipment.HasValidAmmunitionEquipped())
                return unitMeshManager.GetHeldRangedWeapon().MaxRange(gridPosition, targetUnit.GridPosition, accountForHeight);
            else if (myUnitEquipment.MeleeWeaponEquipped())
                return unitMeshManager.GetPrimaryMeleeWeapon().MaxRange(gridPosition, targetUnit.GridPosition, accountForHeight);
            else
                return unitActionHandler.GetAction<MeleeAction>().UnarmedAttackRange(targetUnit.GridPosition, accountForHeight);
        }

        public void BlockCurrentPosition() => singleNodeBlocker.BlockAtCurrentPosition();

        public void UnblockCurrentPosition() => singleNodeBlocker.Unblock();

        public void BlockAtPosition(Vector3 position) => singleNodeBlocker.BlockAt(position);

        public bool IsNPC => gameObject.CompareTag("Player") == false;

        public bool IsPlayer => gameObject.CompareTag("Player");

        public void SetIsMyTurn(bool isMyTurn)
        {
            this.isMyTurn = isMyTurn;
            if (IsPlayer)
                GridSystemVisual.UpdateAttackGridVisual();
        }

        public void SetHasStartedTurn(bool hasStartedTurn) => this.hasStartedTurn = hasStartedTurn;

        public Vector3 WorldPosition => LevelGrid.GetWorldPosition(gridPosition);

        public float ShoulderHeight => shoulderHeight;

        public void CenterPosition() => transform.position = LevelGrid.GetGridPosition(transform.position).WorldPosition();

        public BaseAction SelectedAction => unitActionHandler.selectedActionType.GetAction(this);

        public UnitInventoryManager MainInventoryManager => mainInventoryManager;

        public ContainerInventoryManager BackpackInventoryManager => backpackInventoryManager;

        public ContainerInventoryManager QuiverInventoryManager => quiverInventoryManager;

        public Inventory MainInventory => mainInventoryManager.MainInventory;

        public UnitEquipment UnitEquipment => myUnitEquipment;

        public Transform ActionsParent => actionsParent;

        public bool TryAddItemToInventories(ItemData itemData)
        {
            if (itemData == null || itemData.Item == null)
                return false;

            Inventory itemDatasInventory = itemData.MyInventory();
            if (itemData.Item is Ammunition && myUnitEquipment != null && quiverInventoryManager != null && myUnitEquipment.QuiverEquipped() && quiverInventoryManager.TryAddItem(itemData))
            {
                if (myUnitEquipment.slotVisualsCreated)
                    myUnitEquipment.GetEquipmentSlot(EquipSlot.Quiver).InventoryItem.QuiverInventoryItem.UpdateQuiverSprites();

                if (itemDatasInventory != null && itemDatasInventory is ContainerInventory && itemDatasInventory.ContainerInventory.LooseItem != null && itemDatasInventory.ContainerInventory.LooseItem is LooseQuiverItem)
                    itemDatasInventory.ContainerInventory.LooseItem.LooseQuiverItem.UpdateArrowMeshes();

                return true;
            }

            if (mainInventoryManager != null && MainInventory.TryAddItem(itemData))
                return true;

            if (myUnitEquipment != null)
            {
                if (backpackInventoryManager != null && myUnitEquipment.BackpackEquipped() && backpackInventoryManager.TryAddItem(itemData))
                    return true;
            }

            return false;
        }

        public GridPosition GridPosition => gridPosition;
    }
}
