using System.Buffers.Text;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Unit : MonoBehaviour
{
    [Header("Unit Info")]
    [SerializeField] float shoulderHeight = 0.25f;
    [SerializeField] LayerMask actionObstaclesMask;

    bool isMyTurn, isPerformingAction;

    GridPosition gridPosition;

    List<IEnumerator> actions = new List<IEnumerator>();
    List<int> queuedAP = new List<int>();

    GameManager gm;

    Stats stats;

    void Start()
    {
        gm = GameManager.Instance;
        stats = GetComponent<Stats>();

        gridPosition = LevelGrid.Instance.GetGridPosition(transform.position);
        LevelGrid.Instance.AddUnitAtGridPosition(gridPosition, this);
    }

    #region Action Queue
    public void TakeTurn()
    {
        if (isMyTurn /*&& status.isDead == false*/)
        {
            if (stats.CurrentAP() <= 0)
                StartCoroutine(TurnManager.Instance.FinishTurn(this));
            else
            {
                //vision.CheckEnemyVisibility();

                if (actions.Count > 0)
                    StartCoroutine(GetNextQueuedAction());
                //else if (IsNPC())
                    //stateController.DoAction();
            }
        }
    }

    public void QueueAction(IEnumerator action, int APCost)
    {
        // if (isNPC) Debug.Log(name + " queued " + action);
        actions.Add(action);
        queuedAP.Add(APCost);
        if (isMyTurn && stats.CurrentAP() > 0)
            StartCoroutine(GetNextQueuedAction());

        // Update AP text
        //if (IsNPC() == false)
            //gm.apManager.UpdateLastAPUsed(APCost);
    }

    public IEnumerator GetNextQueuedAction()
    {
        //if (status.isDead)
            //yield break;

        if (actions.Count > 0 && isPerformingAction == false)
        {
            int APRemainder = stats.UseAPAndGetRemainder(queuedAP[0]);
            if (APRemainder <= 0)
            {
                isPerformingAction = true;
                yield return StartCoroutine(actions[0]);
                // if (isNPC == false) Debug.Log("Got next queued action. Actions still queued: " + actions.Count);
            }
            else
            {
                // if (isNPC == false) Debug.Log("Can't do next queued action yet. Remaining AP: " + APRemainder);
                queuedAP[0] = APRemainder;
                StartCoroutine(TurnManager.Instance.FinishTurn(this));
            }
        }
        else
            yield return null;
    }

    public void FinishAction()
    {
        if (queuedAP.Count > 0)
            queuedAP.Remove(queuedAP[0]);
        if (actions.Count > 0)
            actions.Remove(actions[0]);

        isPerformingAction = false;

        // If the character has no AP remaining, end their turn
        if (stats.CurrentAP() <= 0)
            StartCoroutine(TurnManager.Instance.FinishTurn(this));
        else /*if (movement.isMoving == false)*/ // Take another action
            TakeTurn();
    }

    public void ResetActionsQueue()
    {
        actions.Clear();
        queuedAP.Clear();
    }
    #endregion

    public bool IsNPC() => gameObject.CompareTag("Player") == false;

    public bool IsMyTurn() => isMyTurn;

    public void SetIsMyTurn(bool isMyTurn) => this.isMyTurn = isMyTurn; 
    
    public Vector3 WorldPosition() => LevelGrid.Instance.GetWorldPosition(gridPosition);

    public float ShoulderHeight() => shoulderHeight;

    public LayerMask ActionObstaclesMask() => actionObstaclesMask;
}
