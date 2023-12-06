using System;
using System.Collections.Generic;
using UnityEngine;
using UnitSystem.ActionSystem;
using GeneralUI;
using UnitSystem;
using Utilities;
using InventorySystem;
using UnitSystem.ActionSystem.Actions;

namespace GridSystem
{
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

            player = UnitManager.player;

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
                Unit unitAtGridPosition = LevelGrid.GetUnitAtGridPosition(gridPositionList[i]);

                if (unitAtGridPosition == null || player.Vision.IsVisible(unitAtGridPosition) == false)
                    gridSystemVisualSingle.SetMaterial(GetGridVisualTypeMaterial(GridVisualType.Red));
                else if (player.Alliance.IsEnemy(unitAtGridPosition))
                    gridSystemVisualSingle.SetMaterial(GetGridVisualTypeMaterial(GridVisualType.Red));
                else if (player.Alliance.IsAlly(unitAtGridPosition))
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

        /// <summary>Highlights grid spaces that can be reached by the currently selected attack.</summary>
        public static void UpdateAttackRangeGridVisual()
        {
            HideGridVisual();
            
            if (Instance.player.IsMyTurn == false || Instance.player.UnitActionHandler.QueuedActions.Count > 0 
                || Instance.player.UnitActionHandler.PlayerActionHandler.SelectedAction is BaseAttackAction == false || !Instance.player.UnitActionHandler.PlayerActionHandler.SelectedAction.BaseAttackAction.CanShowAttackRange())
                return;

            Instance.ShowAttackRange(Instance.player.UnitActionHandler.PlayerActionHandler.SelectedAction, Instance.player.GridPosition, GridVisualType.RedSoft);
        }

        /// <summary>Highlights grid spaces that will be hit by the currently selected attack, based off of the current mouse position. Color depends on Alliance to Player.</summary>
        public static void UpdateAttackGridVisual()
        {
            UpdateAttackRangeGridVisual();

            BaseAction selectedAction = Instance.player.UnitActionHandler.PlayerActionHandler.SelectedAction;
            if (Instance.player.IsMyTurn == false || Instance.player.UnitActionHandler.QueuedActions.Count > 0
                || selectedAction is BaseAttackAction == false || !selectedAction.BaseAttackAction.CanShowAttackRange())
                return;

            Instance.ShowAttackGridPositionList(selectedAction.GetActionAreaGridPositions(WorldMouse.currentGridPosition));

            if (selectedAction.BaseAttackAction.IsRangedAttackAction())
                ActionLineRenderer.Instance.DrawParabola(Instance.player.WorldPosition + (Instance.player.ShoulderHeight * Vector3.up), WorldMouse.currentGridPosition.WorldPosition);
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
}