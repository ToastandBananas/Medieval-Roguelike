using Pathfinding;
using System;
using System.Collections.Generic;
using UnityEngine;

public class GridSystemVisual : MonoBehaviour
{
    public static GridSystemVisual Instance { get; private set; }

    [Serializable]
    public struct GridVisualTypeMaterial
    {
        public GridVisualType gridVisualType;
        public Material material;
    }

    public enum GridVisualType
    {
        White = 0,
        Blue = 10,
        Red = 20,
        RedSoft = 21,
        Yellow = 30
    }

    [SerializeField] GridSystemVisualSingle gridSystemVisualSinglePrefab;
    [SerializeField] int amountToPool = 50;
    [SerializeField] List<GridVisualTypeMaterial> gridVisualTypeMaterialList;

    List<GridSystemVisualSingle> gridSystemVisualSingleList = new List<GridSystemVisualSingle>();

    Unit player;

    void Awake()
    {
        if (Instance != null)
        {
            Debug.LogError("There's more than one GridSystemVisual! " + transform + " - " + Instance);
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    void Start()
    {
        player = UnitManager.Instance.player;

        for (int i = 0; i < amountToPool; i++)
        {
            GridSystemVisualSingle newGridSystemVisualSingle = CreateNewGridSystemVisualSingle();
            newGridSystemVisualSingle.gameObject.SetActive(false);
        }

        //UnitActionSystem.Instance.OnSelectedActionChanged += UnitActionSystem_OnSelectedActionChanged;
        //UnitActionSystem.Instance.OnUnitDeselected += UnitActionSystem_OnUnitDeselected;

        //if (TurnManager.Instance.ActiveUnit() != null)
            //UpdateGridVisual();
    }

    public GridSystemVisualSingle GetGridVisualSystemSingleFromPool()
    {
        for (int i = 0; i < gridSystemVisualSingleList.Count; i++)
        {
            if (gridSystemVisualSingleList[i].gameObject.activeSelf == false)
                return gridSystemVisualSingleList[i];
        }

        return CreateNewGridSystemVisualSingle();
    }

    GridSystemVisualSingle CreateNewGridSystemVisualSingle()
    {
        GridSystemVisualSingle newGridSystemVisualSingle = Instantiate(gridSystemVisualSinglePrefab, transform);
        gridSystemVisualSingleList.Add(newGridSystemVisualSingle);
        return newGridSystemVisualSingle;
    }

    public void HideAllGridPositions()
    {
        for (int i = 0; i < gridSystemVisualSingleList.Count; i++)
        {
            gridSystemVisualSingleList[i].gameObject.SetActive(false);
        }
    }

    public void ShowGridPositionList(List<GridPosition> gridPositionList, GridVisualType gridVisualType)
    {
        if (gridPositionList == null)
            return;

        for (int i = 0; i < gridPositionList.Count; i++)
        {
            GridSystemVisualSingle gridSystemVisualSingle = GetGridVisualSystemSingleFromPool();
            gridSystemVisualSingle.SetMaterial(GetGridVisualTypeMaterial(gridVisualType));
            gridSystemVisualSingle.transform.position = LevelGrid.Instance.GetWorldPosition(gridPositionList[i]);
            gridSystemVisualSingle.gameObject.SetActive(true);
        }
    }

    public void ShowGridPositionRange(GridPosition gridPosition, float minRange, float maxRange, GridVisualType gridVisualType)
    {
        List<GridPosition> gridPositionList = new List<GridPosition>(); 
        
        ConstantPath path = ConstantPath.Construct(LevelGrid.Instance.GetWorldPosition(gridPosition), 100000 + 1);

        // Schedule the path for calculation
        AstarPath.StartPath(path);

        // Force the path request to complete immediately
        // This assumes the graph is small enough that this will not cause any lag
        path.BlockUntilCalculated();
        
        for (int i = 0; i < path.allNodes.Count; i++)
        {
            GridPosition nodeGridPosition = new GridPosition((Vector3)path.allNodes[i].position);

            if (LevelGrid.Instance.IsValidGridPosition(nodeGridPosition) == false)
                continue;

            float distance = TacticsPathfindingUtilities.CalculateWorldSpaceDistance_XZ(gridPosition, nodeGridPosition);
            if (distance > maxRange || distance < minRange)
                continue;

            float sphereCastRadius = 0.1f;
            Vector3 shootDir =  (gridPosition.WorldPosition() + (Vector3.up * player.ShoulderHeight()) - ((Vector3)path.allNodes[i].position + (Vector3.up * player.ShoulderHeight()))).normalized;
            if (Physics.SphereCast((Vector3)path.allNodes[i].position + (Vector3.up * player.ShoulderHeight()), sphereCastRadius, shootDir, out RaycastHit hit, Vector3.Distance(player.WorldPosition() + (Vector3.up * player.ShoulderHeight()), (Vector3)path.allNodes[i].position + (Vector3.up * player.ShoulderHeight())), player.unitActionHandler.AttackObstacleMask()))
                continue; // Blocked by an obstacle

            // Debug.Log(gridPosition);
            gridPositionList.Add(nodeGridPosition);
        }

        ShowGridPositionList(gridPositionList, gridVisualType);
    }

    public void UpdateGridVisual()
    {
        HideAllGridPositions();

        BaseAction selectedAction = player.unitActionHandler.selectedAction;
        GridVisualType gridVisualType;
        switch (selectedAction)
        {
            case MeleeAction meleeAction:
                gridVisualType = GridVisualType.Red;
                Weapon meleeWeapon = player.GetPrimaryMeleeWeapon().itemData.item.Weapon();
                ShowGridPositionRange(player.gridPosition, meleeWeapon.minRange, meleeWeapon.maxRange, GridVisualType.RedSoft);
                break;
            case ShootAction shootAction:
                gridVisualType = GridVisualType.Red;
                Weapon rangedWeapon = player.GetRangedWeapon().itemData.item.Weapon();
                ShowGridPositionRange(player.gridPosition, rangedWeapon.minRange, rangedWeapon.maxRange, GridVisualType.RedSoft);
                break;
            //case ThrowAction throwBombAction:
                //gridVisualType = GridVisualType.Red;
                //ShowGridPositionRange(player.gridPosition, throwBombAction.MinThrowDistance(), throwBombAction.MaxThrowDistance(), GridVisualType.RedSoft);
                //break;
            //case InteractAction interactAction:
                //gridVisualType = GridVisualType.Yellow;
                //break;
            default:
                gridVisualType = GridVisualType.White;
                break;
        }

        ShowGridPositionList(selectedAction.GetValidActionGridPositionList(player.gridPosition), gridVisualType);
    }

    void UnitActionSystem_OnSelectedActionChanged(object sender, EventArgs e)
    {
        //if (UnitActionSystem.Instance.SelectedUnit() != null && TurnManager.Instance.IsPlayerTurn() && UnitActionSystem.Instance.SelectedUnit().GetAction<MoveAction>().IsActive() == false)
            //UpdateGridVisual();
    }

    void UnitActionSystem_OnUnitDeselected(object sender, EventArgs e)
    {
        HideAllGridPositions();
    }

    Material GetGridVisualTypeMaterial(GridVisualType gridVisualType)
    {
        for (int i = 0; i < gridVisualTypeMaterialList.Count; i++)
        {
            if (gridVisualTypeMaterialList[i].gridVisualType == gridVisualType)
                return gridVisualTypeMaterialList[i].material;
        }

        Debug.LogError("Could not find GridVisualTypeMaterial for " + gridVisualType + ". Assign a material in the inspector on GridSystemVisual.");
        return null;
    }
}
