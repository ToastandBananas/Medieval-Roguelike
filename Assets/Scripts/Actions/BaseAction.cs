using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class BaseAction : MonoBehaviour
{
    public abstract void TakeAction(GridPosition gridPosition, Action onActionComplete);
}
