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

    public static void HideGridVisual()
    {
        for (int i = 0; i < Instance.gridSystemVisualSingleList.Count; i++)
        {
            Instance.gridSystemVisualSingleList[i].gameObject.SetActive(false);
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

    public void ShowGridPositionMeleeRange(GridPosition gridPosition, float minRange, float maxRange, GridVisualType gridVisualType)
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

            float maxRangeToNodePosition = maxRange - Mathf.Abs(nodeGridPosition.y - gridPosition.y);
            if (maxRangeToNodePosition < 0f) maxRangeToNodePosition = 0f;

            float distance = TacticsPathfindingUtilities.CalculateWorldSpaceDistance_XZ(gridPosition, nodeGridPosition);
            if (distance > maxRangeToNodePosition || distance < minRange)
                continue;

            float sphereCastRadius = 0.1f;
            Vector3 shootDir = (gridPosition.WorldPosition() + (Vector3.up * player.ShoulderHeight() * 2f) - ((Vector3)path.allNodes[i].position + (Vector3.up * player.ShoulderHeight() * 2f))).normalized;
            if (Physics.SphereCast((Vector3)path.allNodes[i].position + (Vector3.up * player.ShoulderHeight() * 2f), sphereCastRadius, shootDir, out RaycastHit hit, Vector3.Distance(player.WorldPosition() + (Vector3.up * player.ShoulderHeight() * 2f), (Vector3)path.allNodes[i].position + (Vector3.up * player.ShoulderHeight() * 2f)), player.unitActionHandler.AttackObstacleMask()))
                continue; // Blocked by an obstacle

            // Debug.Log(gridPosition);
            gridPositionList.Add(nodeGridPosition);
        }

        ShowGridPositionList(gridPositionList, gridVisualType);
    }

    public void ShowGridPositionShootRange(GridPosition gridPosition, float minRange, float maxRange, GridVisualType gridVisualType)
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

            float maxRangeToNodePosition = maxRange + (gridPosition.y - nodeGridPosition.y);
            if (maxRangeToNodePosition < 0f) maxRangeToNodePosition = 0f;
            
            float distance = TacticsPathfindingUtilities.CalculateWorldSpaceDistance_XZ(gridPosition, nodeGridPosition);
            if (distance > maxRangeToNodePosition || distance < minRange)
                continue;

            float sphereCastRadius = 0.1f;
            Vector3 shootDir =  (gridPosition.WorldPosition() + (Vector3.up * player.ShoulderHeight() * 2f) - ((Vector3)path.allNodes[i].position + (Vector3.up * player.ShoulderHeight() * 2f))).normalized;
            if (Physics.SphereCast((Vector3)path.allNodes[i].position + (Vector3.up * player.ShoulderHeight() * 2f), sphereCastRadius, shootDir, out RaycastHit hit, Vector3.Distance(player.WorldPosition() + (Vector3.up * player.ShoulderHeight() * 2f), (Vector3)path.allNodes[i].position + (Vector3.up * player.ShoulderHeight() * 2f)), player.unitActionHandler.AttackObstacleMask()))
                continue; // Blocked by an obstacle

            // Debug.Log(gridPosition);
            gridPositionList.Add(nodeGridPosition);
        }

        ShowGridPositionList(gridPositionList, gridVisualType);
    }

    public static void UpdateGridVisual()
    {
        HideGridVisual();

        BaseAction selectedAction = Instance.player.unitActionHandler.selectedAction;
        GridVisualType gridVisualType;
        GridVisualType secondaryGridVisualType;
        switch (selectedAction)
        {
            case MeleeAction meleeAction:
                gridVisualType = GridVisualType.Red;
                secondaryGridVisualType = GridVisualType.Yellow;
                if (Instance.player.MeleeWeaponEquipped())
                {
                    Weapon meleeWeapon = Instance.player.GetPrimaryMeleeWeapon().itemData.item.Weapon();
                    Instance.ShowGridPositionMeleeRange(Instance.player.gridPosition, meleeWeapon.minRange, meleeWeapon.maxRange, GridVisualType.RedSoft);
                }
                else
                    Instance.ShowGridPositionMeleeRange(Instance.player.gridPosition, 1f, Instance.player.unitActionHandler.GetAction<MeleeAction>().UnarmedAttackRange(Instance.player.gridPosition, false), GridVisualType.RedSoft);
                break;
            case ShootAction shootAction:
                gridVisualType = GridVisualType.Red;
                secondaryGridVisualType = GridVisualType.Yellow;
                Weapon rangedWeapon = Instance.player.GetRangedWeapon().itemData.item.Weapon();
                Instance.ShowGridPositionShootRange(Instance.player.gridPosition, rangedWeapon.minRange, rangedWeapon.maxRange, GridVisualType.RedSoft);
                break;
            //case ThrowAction throwBombAction:
                //gridVisualType = GridVisualType.Red;
                //ShowGridPositionRange(player.gridPosition, throwBombAction.MinThrowDistance(), throwBombAction.MaxThrowDistance(), GridVisualType.RedSoft);
                //break;
            //case InteractAction interactAction:
                //gridVisualType = GridVisualType.Yellow;
                //break;
            default:
                return;
        }

        Instance.ShowGridPositionList(selectedAction.GetValidActionGridPositionList(Instance.player.gridPosition), gridVisualType);
        Instance.ShowGridPositionList(selectedAction.GetValidActionGridPositionList_Secondary(Instance.player.gridPosition), secondaryGridVisualType);
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
