using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MoveAction : BaseAction
{
    [Header("Flee State Variables")]
    public LayerMask fleeObstacleMask;
    public float fleeDistance = 20f;
    public bool shouldAlwaysFleeCombat;

    [Header("Follow State Variables")]
    public float startFollowingDistance = 3f;
    public float slowDownDistance = 4f;
    public bool shouldFollowLeader;

    [Header("Patrol State Variables")]
    public Vector2[] patrolPoints;

    [Header("Pursue State Variables")]
    public float maxChaseDistance = 15f;

    [Header("Wandering State Variables")]
    public Vector2 defaultPosition;
    public float minRoamDistance = 5f;
    public float maxRoamDistance = 20f; 
    
    int currentPatrolPointIndex;
    bool initialPatrolPointSet;

    GridPosition targetGridPosition;
    Vector2 roamPosition;
    Vector3 fleeDestination;
    float distToFleeDestination = 0;

    bool isMoving, moveQueued;
    bool roamPositionSet;
    bool needsFleeDestination = true;

    public override void TakeAction(GridPosition targetGridPosition, Action onActionComplete)
    {
        this.targetGridPosition = targetGridPosition;
    }

    public bool IsMoving() => isMoving;

    public void SetIsMoving(bool isMoving) => this.isMoving = isMoving;

    public GridPosition TargetGridPosition() => targetGridPosition;

    public void SetTargetGridPosition(GridPosition targetGridPosition) => this.targetGridPosition = targetGridPosition; 
    
    public void ResetToDefaults()
    {
        roamPositionSet = false;
        needsFleeDestination = true;
        initialPatrolPointSet = false;

        fleeDestination = Vector3.zero;
        distToFleeDestination = 0;
    }
}
