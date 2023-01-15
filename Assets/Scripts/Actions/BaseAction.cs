using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class BaseAction : MonoBehaviour
{
    protected bool isActive;

    public abstract void TakeAction(GridPosition gridPosition, Action onActionComplete); 
    
    protected void StartAction(Action onActionComplete)
    {
        isActive = true;
        //this.onActionComplete = onActionComplete;

        //OnAnyActionStarted?.Invoke(this, EventArgs.Empty);
    }

    public virtual void CompleteAction()
    {
        Debug.Log("Action Complete");
        isActive = false;
        //onActionComplete();

        //OnAnyActionCompleted?.Invoke(this, EventArgs.Empty);
    }
}
