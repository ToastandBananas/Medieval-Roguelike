using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TurnAction : BaseAction
{
    public override void TakeAction(GridPosition gridPosition, Action onActionComplete)
    {
        throw new NotImplementedException();
    }

    public Direction DetermineTurnDirection()
    {
        return Direction.North;
    }
}
