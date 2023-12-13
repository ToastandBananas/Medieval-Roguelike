using Pathfinding;
using System.Collections.Generic;
using UnityEngine;
using InteractableObjects;
using GridSystem;
using UnitSystem.ActionSystem;
using InventorySystem;
using UnitSystem.ActionSystem.Actions;

namespace UnitSystem
{
    /// <summary>Used to determine which armor models to use when equipping.</summary>
    public enum Gender { Male, Female }

    public class Unit : MonoBehaviour
    {
        [Header("Unit Info")]
        [SerializeField] Gender gender;
        [SerializeField] float shoulderHeight = 0.25f;

        [Header("Inventories")]
        [SerializeField] UnitInventoryManager unitInventoryManager;
        [SerializeField] UnitEquipment myUnitEquipment;

        public SingleNodeBlocker SingleNodeBlocker { get; private set; }

        public Alliance Alliance { get; private set; }
        public HealthSystem HealthSystem { get; private set; }
        public Hearing Hearing { get; private set; }
        public OpportunityAttackTrigger OpportunityAttackTrigger { get; private set; }
        public Rigidbody RigidBody { get; private set; }
        public Seeker Seeker { get; private set; }
        public StateController StateController { get; private set; }
        public Stats Stats { get; private set; }
        public UnitActionHandler UnitActionHandler { get; private set; }
        public UnitAnimator UnitAnimator { get; private set; }
        public Interactable_Unit UnitInteractable { get; private set; }
        public UnitMeshManager UnitMeshManager { get; private set; }
        public Vision Vision { get; private set; }

        public List<Unit> UnitsWhoCouldOpportunityAttackMe { get; private set; }

        GridPosition gridPosition;

        void Awake()
        {
            // Center the Unit's position on whatever tile they're on
            CenterPosition();

            SingleNodeBlocker = GetComponent<SingleNodeBlocker>();
            Alliance = GetComponent<Alliance>();
            HealthSystem = GetComponent<HealthSystem>();
            Hearing = GetComponentInChildren<Hearing>();
            OpportunityAttackTrigger = GetComponentInChildren<OpportunityAttackTrigger>();
            RigidBody = GetComponent<Rigidbody>();
            Seeker = GetComponent<Seeker>();
            StateController = GetComponent<StateController>();
            Stats = GetComponent<Stats>();
            UnitActionHandler = GetComponent<UnitActionHandler>();
            UnitAnimator = GetComponentInChildren<UnitAnimator>();
            UnitMeshManager = GetComponent<UnitMeshManager>();
            Vision = GetComponentInChildren<Vision>();

            if (IsNPC)
            {
                if (TryGetComponent(out Interactable_Unit unitInteractable)) UnitInteractable = unitInteractable;
            }

            UnitsWhoCouldOpportunityAttackMe = new List<Unit>();
        }

        void Start()
        {
            SingleNodeBlocker.manager = LevelGrid.BlockManager;
            LevelGrid.AddSingleNodeBlockerToList(SingleNodeBlocker, LevelGrid.UnitSingleNodeBlockerList);

            if (IsNPC)
            {
                if (HealthSystem.IsDead)
                {
                    UnblockCurrentPosition();
                    if (UnitInteractable != null)
                        UnitInteractable.enabled = true;
                }
                else
                    BlockCurrentPosition();

                UnitMeshManager.HideMeshRenderers();
            }

            gridPosition.Set(transform.position);
            LevelGrid.AddUnitAtGridPosition(gridPosition, this);
        }

        // Used for debugging
        /*void Update()
        {
            //if (IsMyTurn && unitActionHandler.isPerformingAction == false)
                //unitActionHandler.SkipTurn();
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

        public float GetAttackRange()
        {
            if (myUnitEquipment.RangedWeaponEquipped && myUnitEquipment.HasValidAmmunitionEquipped())
                return UnitMeshManager.GetHeldRangedWeapon().ItemData.Item.Weapon.MaxRange;
            else if (myUnitEquipment.IsDualWielding)
            {
                float primaryWeaponAttackRange = UnitMeshManager.GetPrimaryHeldMeleeWeapon().ItemData.Item.Weapon.MaxRange;
                float secondaryWeaponAttackRange = UnitMeshManager.GetLeftHeldMeleeWeapon().ItemData.Item.Weapon.MaxRange;
                return Mathf.Max(primaryWeaponAttackRange, secondaryWeaponAttackRange);
            }
            else if (myUnitEquipment.MeleeWeaponEquipped)
                return UnitMeshManager.GetPrimaryHeldMeleeWeapon().ItemData.Item.Weapon.MaxRange;
            else
                return Stats.UnarmedAttackRange;
        }

        public void BlockCurrentPosition() => SingleNodeBlocker.BlockAtCurrentPosition();

        public void BlockAtPosition(Vector3 position) => SingleNodeBlocker.BlockAt(position);

        public void UnblockCurrentPosition() => SingleNodeBlocker.Unblock();

        public bool IsNPC => gameObject.CompareTag("Player") == false;

        public bool IsPlayer => gameObject.CompareTag("Player");

        public bool IsMyTurn => TurnManager.Instance.activeUnit == this;

        public Vector3 WorldPosition => LevelGrid.GetWorldPosition(gridPosition);

        public float ShoulderHeight => shoulderHeight;

        public void CenterPosition() => transform.position = LevelGrid.SnapPosition(transform.position);

        public Action_Base SelectedAction => UnitActionHandler.PlayerActionHandler.SelectedActionType.GetAction(this);

        public UnitInventoryManager UnitInventoryManager => unitInventoryManager;
        public ContainerInventoryManager BackpackInventoryManager => unitInventoryManager.BackpackInventoryManager;
        public ContainerInventoryManager BeltInventoryManager => unitInventoryManager.BeltInventoryManager;
        public ContainerInventoryManager QuiverInventoryManager => unitInventoryManager.QuiverInventoryManager;

        public UnitEquipment UnitEquipment => myUnitEquipment;

        public GridPosition GridPosition => HealthSystem.IsDead ? LevelGrid.GetGridPosition(transform.position) : gridPosition;

        public Gender Gender => gender;
    }
}
