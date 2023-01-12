using System.Collections;
using System.Collections.Generic;
using UnityEngine;

enum Direction { North, East, South, West, NorthWest, NorthEast, SouthWest, SouthEast }

public class PlayerActionInput : MonoBehaviour
{
    [SerializeField] Unit unit;

    Direction directionFacing = Direction.North;

    void Start()
    {
        unit.SetIsMyTurn(true);
    }

    void Update()
    {
        if (unit.IsMyTurn())
        {
            if (GameControls.gamePlayActions.leftMouseClick.WasPressed)
            {
                GridPosition mouseGridPosition = GetMouseGridPosition();
            }
        }
    }

    GridPosition GetMouseGridPosition()
    {
        Debug.Log(LevelGrid.Instance.GetGridPosition(WorldMouse.GetPosition()));
        return LevelGrid.Instance.GetGridPosition(WorldMouse.GetPosition());
    }
}
