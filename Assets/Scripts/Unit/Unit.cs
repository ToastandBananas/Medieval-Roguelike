using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Unit : MonoBehaviour
{
    [Header("Unit Info")]
    [SerializeField] float shoulderHeight = 0.25f;
    [SerializeField] LayerMask actionObstaclesMask;

    bool isMyTurn;

    GridPosition gridPosition;

    void Start()
    {
        gridPosition = LevelGrid.Instance.GetGridPosition(transform.position);
        LevelGrid.Instance.AddUnitAtGridPosition(gridPosition, this);
    }

    public bool IsMyTurn() => isMyTurn;

    public void SetIsMyTurn(bool isMyTurn) => this.isMyTurn = isMyTurn; 
    
    public Vector3 WorldPosition() => LevelGrid.Instance.GetWorldPosition(gridPosition);

    public float ShoulderHeight() => shoulderHeight;

    public LayerMask ActionObstaclesMask() => actionObstaclesMask;
}
