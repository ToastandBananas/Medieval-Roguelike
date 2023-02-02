using System.Collections.Generic;
using UnityEngine;

public class NPCActionHandler : UnitActionHandler
{
    [Header("Fight State")]
    [SerializeField] float maxChaseDistance = 15f;

    [Header("Flee State")]
    [SerializeField] int defaultFleeDistance = 20;
    [SerializeField] bool shouldAlwaysFleeCombat;
    public Unit unitToFleeFrom;
    int fleeDistance;
    GridPosition unitToFleeFrom_PreviousGridPosition;
    bool needsNewFleeDestination = true;
    float unitToFleeFrom_PreviousDistance;

    [Header("Follow State")]
    [SerializeField] float stopFollowDistance = 3f;
    [SerializeField] Unit leader;
    [SerializeField] public bool shouldFollowLeader { get; private set; }

    [Header("Patrol State")]
    [SerializeField] Vector3[] patrolPoints;
    public int currentPatrolPointIndex { get; private set; }
    bool initialPatrolPointSet, hasAlternativePatrolPoint;
    int patrolIterationCount;
    readonly int maxPatrolIterations = 5;

    [Header("Wander State")]
    [SerializeField] Vector3 defaultPosition;
    [SerializeField] int minWanderDistance = 5;
    [SerializeField] int maxWanderDistance = 20;
    GridPosition wanderGridPosition;
    bool wanderPositionSet;

    void Start()
    {
        if (defaultPosition == Vector3.zero) defaultPosition = transform.position;
    }

    public override void TakeTurn()
    {
        if (unit.isMyTurn && unit.health.IsDead() == false)
        {
            if (canPerformActions == false || unit.stats.CurrentAP() <= 0)
            {
                TurnManager.Instance.FinishTurn(unit); // Unit can't do anything, so skip their turn
                return;
            }
            else if (unit.stateController.currentState == State.Fight && queuedAction == GetAction<MoveAction>() && targetEnemyUnit != null)
            {
                if (unit.RangedWeaponEquipped())
                {
                    Unit closestEnemy = unit.vision.GetClosestEnemy(true);
                    float minShootRange = unit.GetRangedWeapon().itemData.item.Weapon().minRange;

                    // If the closest enemy is too close and this Unit doesn't have a melee weapon, retreat back a few spaces
                    if (TacticsPathfindingUtilities.CalculateWorldSpaceDistance_XYZ(unit.gridPosition, closestEnemy.gridPosition) < minShootRange)
                    {
                        // Flee somewhere
                        StartFlee(unit.vision.GetClosestEnemy(true), Mathf.RoundToInt(minShootRange + Random.Range(2, unit.GetRangedWeapon().itemData.item.Weapon().maxRange - 2)));
                    }
                    else if (GetAction<ShootAction>().IsInAttackRange(targetEnemyUnit))
                    {
                        // Shoot the target enemy
                        ClearActionQueue(true);
                        if (unit.GetRangedWeapon().isLoaded)
                            QueueAction(GetAction<ShootAction>());
                        else
                            QueueAction(GetAction<ReloadAction>());
                        return;
                    }
                }
                else if (unit.MeleeWeaponEquipped() || GetAction<MeleeAction>().CanFightUnarmed())
                {
                    if (GetAction<MeleeAction>().IsInAttackRange(targetEnemyUnit))
                    {
                        // Melee attack the target enemy
                        ClearActionQueue(false);
                        QueueAction(GetAction<MeleeAction>());
                        return;
                    }
                }
            }

            // If not attacking, get/determine the next action
            if (queuedAction != null)
                GetNextQueuedAction();
            else
                DetermineAction(); 
        }
    }

    public override void FinishAction()
    {
        base.FinishAction();

        if (unit.stats.CurrentAP() > 0 && unit.isMyTurn) // Take another action if this is the last NPC who hasn't finished their turn
            StartCoroutine(TurnManager.Instance.StartNextUnitsTurn(unit));
    }

    public void DetermineAction()
    {
        switch (unit.stateController.currentState)
        {
            case State.Idle:
                TurnManager.Instance.FinishTurn(unit);
                break;
            case State.Patrol:
                Patrol();
                break;
            case State.Wander:
                Wander();
                break;
            case State.Follow:
                Follow();
                break;
            case State.MoveToTarget:
                break;
            case State.Fight:
                Fight();
                break;
            case State.Flee:
                Flee();
                break;
            case State.Hunt:
                break;
            case State.FindFood:
                break;
            default:
                break;
        }
    }

    #region Fight
    void Fight()
    {
        FindBestTargetEnemy();

        // If there's no target enemy Unit, try to find one, else switch States
        if (targetEnemyUnit == null)
        {
            if (unit.stateController.DefaultState() == State.Fight)
                unit.stateController.ChangeDefaultState(State.Idle);
            unit.stateController.SetToDefaultState();

            DetermineAction();
            return;
        }

        if ((unit.RangedWeaponEquipped() && GetAction<ShootAction>().IsInAttackRange(targetEnemyUnit)) || ((unit.MeleeWeaponEquipped() || (unit.RangedWeaponEquipped() == false && GetAction<MeleeAction>().CanFightUnarmed())) && GetAction<MeleeAction>().IsInAttackRange(targetEnemyUnit)))
            AttackTargetEnemy();
        else
            PursueTargetEnemy();
    }

    public void StartFight()
    {
        unit.stateController.SetCurrentState(State.Fight);
        FindBestTargetEnemy();
        ClearActionQueue(false);
    }

    void PursueTargetEnemy()
    {
        // Move towards the position behind the enemy Unit, as this will always be an ideal position to attack from. If this Unit gets close enough to attack, they'll attack from whatever position they're in anyways.
        SetTargetGridPosition(targetEnemyUnit.unitActionHandler.GetAction<TurnAction>().GetGridPositionBehindUnit());

        // If there's no space around the enemy unit, try to find another enemy to attack
        if (targetEnemyUnit.IsCompletelySurrounded())
        {
            SwitchTargetEnemies(out Unit oldEnemy, out Unit newEnemy);
            if (oldEnemy == newEnemy)
            {
                // There were no other enemies in range, so just move to the nearest possible position to the current enemy
                SetTargetGridPosition(LevelGrid.Instance.FindNearestValidGridPosition(targetEnemyUnit.gridPosition, unit, 10));
            }
            else
            {
                SetTargetGridPosition(targetEnemyUnit.unitActionHandler.GetAction<TurnAction>().GetGridPositionBehindUnit());
            }
        }

        QueueAction(GetAction<MoveAction>());
    }

    void FindBestTargetEnemy()
    {
        if (unit.vision.visibleEnemies.Count > 0)
        {
            List<EnemyAIAction> enemyAIActions = new List<EnemyAIAction>();
            MeleeAction meleeAction = GetAction<MeleeAction>();
            for (int i = 0; i < unit.vision.visibleEnemies.Count; i++)
            {
                enemyAIActions.Add(meleeAction.GetEnemyAIAction(unit.vision.visibleEnemies[i].gridPosition));
            }

            SetTargetEnemyUnit(meleeAction.GetBestEnemyAIActionFromList(enemyAIActions).unit);
        }
        else
        {
            if (unit.stateController.DefaultState() == State.Fight)
                unit.stateController.ChangeDefaultState(State.Idle);
            unit.stateController.SetToDefaultState();
        }
    }

    void SearchForRandomTargetEnemy()
    {
        if (unit.vision.visibleEnemies.Count > 0)
            targetEnemyUnit = unit.vision.visibleEnemies[Random.Range(0, unit.vision.visibleEnemies.Count)];
        else
            targetEnemyUnit = null;
    }

    void SwitchTargetEnemies(out Unit oldEnemy, out Unit newEnemy)
    {
        oldEnemy = targetEnemyUnit;
        Unit closestEnemy = unit.vision.GetClosestEnemy(false);

        // Debug.Log(unit + " new enemy: " + closestEnemy + " old enemy: " + oldEnemy);
        newEnemy = closestEnemy;
        SetTargetEnemyUnit(closestEnemy);
    }
    #endregion

    #region Flee
    void Flee()
    {
        // If there's no Unit to flee from or if the Unit to flee from died
        if (unitToFleeFrom == null || unitToFleeFrom.health.IsDead())
        {
            if (unit.stateController.DefaultState() == State.Flee)
                unit.stateController.ChangeDefaultState(State.Wander);
            unit.stateController.SetToDefaultState(); // Variables are reset in this method
            DetermineAction();
            return;
        }

        float distanceFromUnitToFleeFrom = TacticsPathfindingUtilities.CalculateWorldSpaceDistance_XZ(unitToFleeFrom.gridPosition, unit.gridPosition);

        // If the Unit has fled far enough
        if (distanceFromUnitToFleeFrom >= fleeDistance)
        {
            if (unit.stateController.DefaultState() == State.Flee)
                unit.stateController.ChangeDefaultState(State.Wander);
            unit.stateController.SetToDefaultState(); // Variables are also reset in this method
            DetermineAction();
            return;
        }

        // The enemy this Unit is fleeing from has moved closer or they have arrived at their flee destination, but are still too close to the enemy, so get a new flee destination
        if (unit.gridPosition == targetGridPosition || (unitToFleeFrom.gridPosition != unitToFleeFrom_PreviousGridPosition && (unitToFleeFrom_PreviousDistance == 0f || distanceFromUnitToFleeFrom + 2f <= unitToFleeFrom_PreviousDistance)))
            needsNewFleeDestination = true;

        if (needsNewFleeDestination)
        {
            needsNewFleeDestination = false;
            unitToFleeFrom_PreviousDistance = TacticsPathfindingUtilities.CalculateWorldSpaceDistance_XZ(unitToFleeFrom.gridPosition, unit.gridPosition);
            SetTargetGridPosition(GetFleeDestination());
        }

        // If there was no valid flee position, just grab a random position within range
        if (targetGridPosition == unit.gridPosition)
            SetTargetGridPosition(LevelGrid.Instance.GetRandomGridPositionInRange(unitToFleeFrom.gridPosition, unit, fleeDistance, fleeDistance + 15));

        QueueAction(GetAction<MoveAction>());
    }

    public void StartFlee(Unit unitToFleeFrom, int fleeDistance)
    {
        unit.stateController.SetCurrentState(State.Flee);
        this.unitToFleeFrom = unitToFleeFrom;
        this.fleeDistance = fleeDistance;
        ClearActionQueue(false);
    }

    GridPosition GetFleeDestination() => LevelGrid.Instance.GetRandomFleeGridPosition(unit, unitToFleeFrom, fleeDistance, fleeDistance + 15);

    public int DefaultFleeDistance() => defaultFleeDistance;

    public void SetUnitToFleeFrom(Unit unitToFleeFrom) => this.unitToFleeFrom = unitToFleeFrom;

    public bool ShouldAlwaysFleeCombat() => shouldAlwaysFleeCombat;
    #endregion

    #region Follow
    void Follow()
    {
        if (leader == null || leader.health.IsDead())
        {
            Debug.LogWarning("Leader for " + unit.name + " is null or dead, but they are in the Follow state.");
            shouldFollowLeader = false;
            if (unit.stateController.DefaultState() == State.Follow)
                unit.stateController.ChangeDefaultState(State.Idle);

            unit.stateController.SetToDefaultState();
            DetermineAction();
            return;
        }

        if (Vector3.Distance(transform.position, leader.WorldPosition()) <= stopFollowDistance)
            TurnManager.Instance.FinishTurn(unit);
        else if (GetAction<MoveAction>().isMoving == false)
        {
            SetTargetGridPosition(leader.unitActionHandler.GetAction<TurnAction>().GetGridPositionBehindUnit());
            QueueAction(GetAction<MoveAction>());
        }
    }

    public Unit Leader() => leader;

    public void SetLeader(Unit newLeader) => leader = newLeader;

    public void SetShouldFollowLeader(bool shouldFollowLeader) => this.shouldFollowLeader = shouldFollowLeader;
    #endregion

    #region Patrol
    void Patrol()
    {
        if (patrolIterationCount >= maxPatrolIterations)
        {
            // Debug.Log("Max patrol iterations reached...");
            patrolIterationCount = 0;
            TurnManager.Instance.FinishTurn(unit);
            return;
        }
        else if (patrolPoints.Length > 0)
        {
            if (initialPatrolPointSet == false)
            {
                // Get the closest Patrol Point to the Unit as the first Patrol Point to move to
                currentPatrolPointIndex = GetNearestPatrolPointIndex();
                initialPatrolPointSet = true;
            }

            GridPosition patrolPointGridPosition = LevelGrid.Instance.GetGridPosition(patrolPoints[currentPatrolPointIndex]);

            // If the Patrol Point is set to an invalid Grid Position
            if (LevelGrid.Instance.IsValidGridPosition(patrolPointGridPosition) == false)
            {
                // Debug.LogWarning(patrolPointGridPosition + " is not a valid grid position...");
                IncreasePatrolPointIndex();
                return;
            }
            // If there's another Unit currently on the Patrol Point or Alternative Patrol Point
            else if ((hasAlternativePatrolPoint == false && LevelGrid.Instance.GridPositionObstructed(patrolPointGridPosition) && LevelGrid.Instance.GetUnitAtGridPosition(patrolPointGridPosition) != unit)
                || (hasAlternativePatrolPoint && LevelGrid.Instance.GridPositionObstructed(targetGridPosition) && LevelGrid.Instance.GetUnitAtGridPosition(targetGridPosition) != unit))
            {
                // Increase the iteration count just in case we had to look for an Alternative Patrol Point due to something obstructing the current Target Grid Position
                patrolIterationCount++;

                // Find the nearest Grid Position to the Patrol Point
                GridPosition nearestGridPositionToPatrolPoint = LevelGrid.Instance.FindNearestValidGridPosition(patrolPointGridPosition, unit, 7);
                if (patrolPointGridPosition == nearestGridPositionToPatrolPoint)
                    IncreasePatrolPointIndex();

                hasAlternativePatrolPoint = true;
                SetTargetGridPosition(nearestGridPositionToPatrolPoint);

                if (nearestGridPositionToPatrolPoint != patrolPointGridPosition && LevelGrid.Instance.GridPositionObstructed(nearestGridPositionToPatrolPoint) == false)
                    patrolIterationCount = 0;
            }

            // If the Unit has arrived at their current Patrol Point or Alternative Patrol Point position
            if (Vector3.Distance(targetGridPosition.WorldPosition(), transform.position) <= 0.1f)
            {
                if (hasAlternativePatrolPoint)
                    hasAlternativePatrolPoint = false;

                // Set the Unit's Target Grid Position as the next Patrol Point
                IncreasePatrolPointIndex();
                patrolPointGridPosition = LevelGrid.Instance.GetGridPosition(patrolPoints[currentPatrolPointIndex]);
                SetTargetGridPosition(patrolPointGridPosition);
            }
            // Otherwise, assign their target position to the Patrol Point if it's not already set
            else if (hasAlternativePatrolPoint == false && targetGridPosition.WorldPosition() != patrolPoints[currentPatrolPointIndex])
            {
                SetTargetGridPosition(patrolPointGridPosition); 
                
                // Debug.Log("Unit Grid Position: " + unit.gridPosition + " / " + "Target Grid Position: " + targetGridPosition);

                // Don't reset the patrol iteration count if the next target position is the Unit's current position, because we'll need to iterate through Patrol again
                if (targetGridPosition != unit.gridPosition)
                    patrolIterationCount = 0;
            }

            // Queue the Move Action if the Unit isn't already moving
            if (GetAction<MoveAction>().isMoving == false)
                QueueAction(GetAction<MoveAction>());
        }
        else // If no Patrol Points set
        {
            Debug.LogWarning("No patrol points set for " + name);
            patrolIterationCount = 0;

            if (unit.stateController.DefaultState() == State.Patrol)
                unit.stateController.ChangeDefaultState(State.Idle);

            unit.stateController.SetCurrentState(State.Idle);
            DetermineAction();
        }
    }

    public void IncreasePatrolPointIndex()
    {
        if (currentPatrolPointIndex == patrolPoints.Length - 1)
            currentPatrolPointIndex = 0;
        else
            currentPatrolPointIndex++;
    }

    public void AssignNextPatrolTargetPosition()
    {
        IncreasePatrolPointIndex();
        GridPosition patrolPointGridPosition = LevelGrid.Instance.GetGridPosition(patrolPoints[currentPatrolPointIndex]);
        SetTargetGridPosition(patrolPointGridPosition);
    }

    int GetNearestPatrolPointIndex()
    {
        int nearestPatrolPointIndex = 0;
        float nearestPatrolPointDistance = 0;

        for (int i = 0; i < patrolPoints.Length; i++)
        {
            if (i == 0)
                nearestPatrolPointDistance = Vector3.Distance(patrolPoints[i], transform.position);
            else
            {
                float dist = Vector3.Distance(patrolPoints[i], transform.position);
                if (dist < nearestPatrolPointDistance)
                {
                    nearestPatrolPointIndex = i;
                    nearestPatrolPointDistance = dist;
                }
            }
        }

        return nearestPatrolPointIndex;
    }

    public void SetHasAlternativePatrolPoint(bool hasAlternativePatrolPoint) => this.hasAlternativePatrolPoint = hasAlternativePatrolPoint;

    public Vector3[] PatrolPoints() => patrolPoints;
    #endregion

    #region Wander
    void Wander()
    {
        if (wanderPositionSet == false)
        {
            wanderGridPosition = GetNewWanderPosition();
            if (wanderGridPosition == unit.gridPosition)
                TurnManager.Instance.FinishTurn(unit);
            else
            {
                wanderPositionSet = true;
                SetTargetGridPosition(wanderGridPosition);
            }

            // Queue the Move Action if the Unit isn't already moving
            if (GetAction<MoveAction>().isMoving == false)
                QueueAction(GetAction<MoveAction>());
        }
        // If the NPC has arrived at their destination
        else if (Vector3.Distance(wanderGridPosition.WorldPosition(), transform.position) <= 0.1f)
        {
            // Get a new Wander Position when the current one is reached
            wanderPositionSet = false;
            Wander();
            return;
        }
        else if (GetAction<MoveAction>().isMoving == false)
        {
            // Get a new Wander Position if there's now another Unit or obstruction there
            if (LevelGrid.Instance.GridPositionObstructed(wanderGridPosition) && LevelGrid.Instance.GetUnitAtGridPosition(wanderGridPosition) != unit)
            {
                wanderGridPosition = GetNewWanderPosition();
                SetTargetGridPosition(wanderGridPosition);
            }

            QueueAction(GetAction<MoveAction>());
        }
    }

    GridPosition GetNewWanderPosition() => LevelGrid.Instance.GetRandomGridPositionInRange(LevelGrid.Instance.GetGridPosition(defaultPosition), unit, minWanderDistance, maxWanderDistance);
    #endregion

    public void ResetToDefaults()
    {
        // Flee
        needsNewFleeDestination = true;
        unitToFleeFrom = null;
        unitToFleeFrom_PreviousDistance = 0f;
        fleeDistance = 0;

        // Patrol
        hasAlternativePatrolPoint = false;
        initialPatrolPointSet = false;
        patrolIterationCount = 0;

        // Wander
        wanderPositionSet = false;
    }
}
