using System.Collections.Generic;
using UnityEngine;
using Pathfinding;

public class Testing : MonoBehaviour
{
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.T))
        {
            ShowDebugPathToMousePosition();
        }
    }

    void ShowDebugPathToMousePosition()
    {
        GridPosition mouseGridPosition = LevelGrid.Instance.GetGridPosition(WorldMouse.GetPosition());
        GridPosition startGridPosition = UnitActionSystem.Instance.SelectedUnit().GridPosition();
        ABPath path = ABPath.Construct(LevelGrid.Instance.GetWorldPosition(startGridPosition), LevelGrid.Instance.GetWorldPosition(mouseGridPosition));
        path.traversalProvider = LevelGrid.Instance.DefaultTraversalProvider();

        AstarPath.StartPath(path);
        path.BlockUntilCalculated();

        List<GridPosition> gridPositionList = new List<GridPosition>();

        for (int i = 0; i < path.vectorPath.Count; i++)
        {
            gridPositionList.Add(LevelGrid.Instance.GetGridPosition(path.vectorPath[i]));
        }

        for (int i = 0; i < gridPositionList.Count - 1; i++)
        {
            Debug.DrawLine(LevelGrid.Instance.GetWorldPosition(gridPositionList[i]), LevelGrid.Instance.GetWorldPosition(gridPositionList[i + 1]), Color.white, 4f);
        }
    }
}
