using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class BaseAction : MonoBehaviour
{


    void Start()
    {

    }

    public abstract void TakeAction(GridPosition gridPosition, Action onActionComplete);
}
