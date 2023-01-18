using System;
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
        isActive = false;
        //onActionComplete();

        //OnAnyActionCompleted?.Invoke(this, EventArgs.Empty);
    }

    public bool IsActive() => isActive;

    public abstract int GetActionPointsCost();

    public abstract bool IsValidAction();

    public abstract bool ActionIsUsedInstantly();

    public abstract string GetActionName();
}
