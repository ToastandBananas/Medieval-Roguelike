using Pathfinding;
using Pathfinding.Util;
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
    List<GridPosition> gridPositionsList = new List<GridPosition>();

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
            gridSystemVisualSingle.transform.position = LevelGrid.GetWorldPosition(gridPositionList[i]);
            gridSystemVisualSingle.gameObject.SetActive(true);
        }
    }

    public void ShowAttackRange(BaseAction attackAction, GridPosition startGridPosition, GridVisualType gridVisualType)
    {
        gridPositionsList = attackAction.GetActionGridPositionsInRange(startGridPosition);
        ShowGridPositionList(gridPositionsList, gridVisualType);
    }

    public static void UpdateGridVisual()
    {
        HideGridVisual();

        if (Instance.player.isMyTurn == false || Instance.player.unitActionHandler.queuedAction != null || Instance.player.unitActionHandler.targetEnemyUnit != null)
            return;

        BaseAction selectedAction = Instance.player.unitActionHandler.selectedAction;
        if (selectedAction.IsAttackAction() == false)
            return;

        Instance.ShowAttackRange(selectedAction, Instance.player.gridPosition, GridVisualType.RedSoft);
    }

    public static void UpdateAttackGridVisual()
    {
        UpdateGridVisual();

        if (Instance.player.isMyTurn == false || Instance.player.unitActionHandler.queuedAction != null || Instance.player.unitActionHandler.targetEnemyUnit != null)
            return;

        BaseAction selectedAction = Instance.player.unitActionHandler.selectedAction;
        switch (selectedAction)
        {
            case MeleeAction meleeAction:
                Instance.ShowGridPositionList(meleeAction.GetActionAreaGridPositions(WorldMouse.currentGridPosition), GridVisualType.Red);
                break;
            case SwipeAction swipeAction:
                Instance.ShowGridPositionList(swipeAction.GetActionAreaGridPositions(WorldMouse.currentGridPosition), GridVisualType.Red);
                break;
            case ShootAction shootAction:
                Instance.ShowGridPositionList(shootAction.GetActionAreaGridPositions(WorldMouse.currentGridPosition), GridVisualType.Red);
                break;
            //case ThrowAction throwAction:
                //Instance.ShowGridPositionList(throwAction.GetPossibleAttackGridPositions(WorldMouse.currentGridPosition), GridVisualType.Red);
                //break;
            default:
                break;
        }
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
