using Pathfinding;
using System.Collections.Generic;
using UnityEngine;
using InteractableObjects;
using GridSystem;
using ActionSystem;
using InventorySystem;

namespace UnitSystem
{
    /// <summary>Used to determine which armor models to use when equipping.</summary>
    public enum Gender { Male, Female }

    public class Unit : MonoBehaviour
    {
        [Header("Transforms")]
        [SerializeField] Transform actionsParent;

        [Header("Unit Info")]
        [SerializeField] Gender gender;
        [SerializeField] float shoulderHeight = 0.25f;

        [Header("Inventories")]
        [SerializeField] UnitInventoryManager unitInventoryManager;
        [SerializeField] UnitEquipment myUnitEquipment;

        public SingleNodeBlocker singleNodeBlocker { get; private set; }

        public Alliance alliance { get; private set; }
        public Health health { get; private set; }
        public Hearing hearing { get; private set; }
        public OpportunityAttackTrigger opportunityAttackTrigger { get; private set; }
        public Rigidbody rigidBody { get; private set; }
        public Seeker seeker { get; private set; }
        public StateController stateController { get; private set; }
        public Stats stats { get; private set; }
        public UnitActionHandler unitActionHandler { get; private set; }
        public UnitAnimator unitAnimator { get; private set; }
        public UnitInteractable unitInteractable { get; private set; }
        public UnitMeshManager unitMeshManager { get; private set; }
        public Vision vision { get; private set; }

        public List<Unit> unitsWhoCouldOpportunityAttackMe { get; private set; }

        GridPosition gridPosition;

        void Awake()
        {
            // Center the Unit's position on whatever tile they're on
            CenterPosition();

            singleNodeBlocker = GetComponent<SingleNodeBlocker>();
            alliance = GetComponent<Alliance>();
            if (TryGetComponent(out UnitInteractable unitInteractable))
                this.unitInteractable = unitInteractable;
            health = GetComponent<Health>();
            hearing = GetComponentInChildren<Hearing>();
            opportunityAttackTrigger = GetComponentInChildren<OpportunityAttackTrigger>();
            rigidBody = GetComponent<Rigidbody>();
            seeker = GetComponent<Seeker>();
            stateController = GetComponent<StateController>();
            stats = GetComponent<Stats>();
            unitActionHandler = GetComponent<UnitActionHandler>();
            unitAnimator = GetComponentInChildren<UnitAnimator>();
            unitMeshManager = GetComponent<UnitMeshManager>();
            vision = GetComponentInChildren<Vision>();

            unitsWhoCouldOpportunityAttackMe = new List<Unit>();
        }

        void Start()
        {
            singleNodeBlocker.manager = LevelGrid.BlockManager;
            LevelGrid.AddSingleNodeBlockerToList(singleNodeBlocker, LevelGrid.UnitSingleNodeBlockerList);

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

            gridPosition.Set(transform.position);
            LevelGrid.AddUnitAtGridPosition(gridPosition, this);
        }

        // Used for debugging
        /*void Update()
        {
            if (isMyTurn && unitActionHandler.isPerformingAction == false)
                unitActionHandler.TakeTurn();
        }*/

        public void UpdateGridPosition()
        {
            // Unit changed Grid Position
            if (gridPosition != transform.position)
            {
                LevelGrid.RemoveUnitAtGridPosition(gridPosition);
                gridPosition.Set(transform.position);
                LevelGrid.AddUnitAtGridPosition(gridPosition, this);
            }
        }

        public void SetGridPosition(GridPosition gridPosition) => this.gridPosition = gridPosition;

        public bool IsCompletelySurrounded(float range)
        {
            List<GridPosition> surroundingGridPositions = LevelGrid.GetSurroundingGridPositions(gridPosition, range, true, false);
            for (int i = 0; i < surroundingGridPositions.Count; i++)
            {
                if (LevelGrid.GridPositionObstructed(surroundingGridPositions[i]) == false)
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

        public bool IsMyTurn => TurnManager.Instance.activeUnit == this;

        public Vector3 WorldPosition => LevelGrid.GetWorldPosition(gridPosition);

        public float ShoulderHeight => shoulderHeight;

        public void CenterPosition() => transform.position = LevelGrid.SnapPosition(transform.position);

        public BaseAction SelectedAction => unitActionHandler.selectedActionType.GetAction(this);

        public UnitInventoryManager UnitInventoryManager => unitInventoryManager;
        public ContainerInventoryManager BackpackInventoryManager => unitInventoryManager.BackpackInventoryManager;
        public ContainerInventoryManager QuiverInventoryManager => unitInventoryManager.QuiverInventoryManager;

        public UnitEquipment UnitEquipment => myUnitEquipment;

        public Transform ActionsParent => actionsParent;

        public GridPosition GridPosition => health.IsDead() ? LevelGrid.GetGridPosition(transform.position) : gridPosition;

        public Gender Gender => gender;
    }
}
