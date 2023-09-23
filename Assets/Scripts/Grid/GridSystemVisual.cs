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

    public void ShowAttackGridPositionList(List<GridPosition> gridPositionList)
    {
        if (gridPositionList == null)
            return;

        for (int i = 0; i < gridPositionList.Count; i++)
        {
            GridSystemVisualSingle gridSystemVisualSingle = GetGridVisualSystemSingleFromPool();
            Unit unitAtGridPosition = LevelGrid.Instance.GetUnitAtGridPosition(gridPositionList[i]);

            if (unitAtGridPosition == null || player.vision.IsVisible(unitAtGridPosition) == false)
                gridSystemVisualSingle.SetMaterial(GetGridVisualTypeMaterial(GridVisualType.Red));
            else if (player.alliance.IsEnemy(unitAtGridPosition))
                gridSystemVisualSingle.SetMaterial(GetGridVisualTypeMaterial(GridVisualType.Red));
            else if (player.alliance.IsAlly(unitAtGridPosition))
                gridSystemVisualSingle.SetMaterial(GetGridVisualTypeMaterial(GridVisualType.Blue));
            else
                gridSystemVisualSingle.SetMaterial(GetGridVisualTypeMaterial(GridVisualType.Yellow));

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
        
        if (Instance.player.unitActionHandler.selectedAction.IsAttackAction() == false || Instance.player.CharacterEquipment.RangedWeaponEquipped() == false)
            return;

        Instance.ShowAttackRange(Instance.player.unitActionHandler.selectedAction, Instance.player.gridPosition, GridVisualType.RedSoft);
    }

    public static void UpdateAttackGridVisual()
    {
        UpdateGridVisual();

        if (Instance.player.isMyTurn == false || Instance.player.unitActionHandler.queuedAction != null || Instance.player.unitActionHandler.targetEnemyUnit != null)
            return;

        BaseAction selectedAction = Instance.player.unitActionHandler.selectedAction;
        if (selectedAction.IsAttackAction())
            Instance.ShowAttackGridPositionList(selectedAction.GetActionAreaGridPositions(WorldMouse.currentGridPosition));
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
