using System.Collections.Generic;
using UnityEngine;
using Pathfinding;
using GridSystem;
using SoundSystem;
using GeneralUI;
using UnitSystem;

namespace Utilities
{
    public class Testing : MonoBehaviour
    {
        void Update()
        {
            if (Input.GetKeyDown(KeyCode.T))
            {

            }
        }

        void ShowDebugPathToMousePosition()
        {
            GridPosition mouseGridPosition = LevelGrid.GetGridPosition(WorldMouse.GetPosition());
            GridPosition startGridPosition = UnitManager.player.GridPosition;
            ABPath path = ABPath.Construct(LevelGrid.GetWorldPosition(startGridPosition), LevelGrid.GetWorldPosition(mouseGridPosition));
            path.traversalProvider = LevelGrid.Instance.DefaultTraversalProvider();

            AstarPath.StartPath(path);
            path.BlockUntilCalculated();

            List<GridPosition> gridPositionList = new List<GridPosition>();

            for (int i = 0; i < path.vectorPath.Count; i++)
            {
                gridPositionList.Add(LevelGrid.GetGridPosition(path.vectorPath[i]));
            }

            for (int i = 0; i < gridPositionList.Count - 1; i++)
            {
                Debug.DrawLine(LevelGrid.GetWorldPosition(gridPositionList[i]), LevelGrid.GetWorldPosition(gridPositionList[i + 1]), Color.white, 4f);
            }
        }

        void PlayTestSound() => AudioManager.PlayRandomSound(AudioManager.Instance.humanMaleGruntSounds, UnitManager.player.WorldPosition, UnitManager.player, true);
    }
}
