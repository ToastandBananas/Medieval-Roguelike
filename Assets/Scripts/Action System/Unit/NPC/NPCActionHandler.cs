using Pathfinding.Util;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using GridSystem;
using UnitSystem;
using Utilities;
using System.Collections;

namespace ActionSystem
{
    public class NPCActionHandler : UnitActionHandler
    {
        [Header("Fight State")]
        [SerializeField] float maxChaseDistance = 25f;
        GridPosition startChaseGridPosition;
        bool shouldStopChasing;

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

        [Header("Inspect Sound State")]
        GridPosition inspectSoundGridPosition;
        public GridPosition soundGridPosition { get; private set; }
        int inspectSoundIterations;
        int maxInspectSoundIterations;
        bool needsNewSoundInspectPosition = true;

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

        List<NPCAIAction> npcAIActions = new List<NPCAIAction>();

        void Start()
        {
            if (defaultPosition == Vector3.zero) defaultPosition = transform.position;
        }

        public override void TakeTurn()
        {
            if (unit.isMyTurn && unit.health.IsDead() == false)
            {
                unit.vision.FindVisibleUnitsAndObjects();

                if (canPerformActions == false || unit.stats.currentAP <= 0)
                {
                    SkipTurn(); // Unit can't do anything, so skip their turn
                    return;
                }
                else if (queuedActions.Count == 0 && queuedAttack != null)
                {
                    bool canAttack = false;
                    List<GridPosition> actionGridPositionsInRange = ListPool<GridPosition>.Claim();
                    actionGridPositionsInRange = queuedAttack.GetActionGridPositionsInRange(unit.GridPosition);

                    // Check if there's any enemies in the valid attack positions
                    for (int i = 0; i < actionGridPositionsInRange.Count; i++)
                    {
                        if (canAttack) break;

                        foreach (GridPosition gridPosition in queuedAttack.GetActionAreaGridPositions(actionGridPositionsInRange[i]))
                        {
                            if (LevelGrid.Instance.HasAnyUnitOnGridPosition(gridPosition) == false)
                                continue;

                            Unit unitAtGridPosition = LevelGrid.Instance.GetUnitAtGridPosition(gridPosition);
                            if (unit.alliance.IsEnemy(unitAtGridPosition))
                            {
                                canAttack = true;
                                break;
                            }
                        }
                    }

                    ListPool<GridPosition>.Release(actionGridPositionsInRange);

                    // If so, queue the attack and return out of this method
                    if (canAttack)
                    {
                        queuedAttack.QueueAction(targetEnemyUnit);
                        return;
                    }
                }
                else if (unit.stateController.currentState == State.Fight && queuedActions.Count > 0 && queuedActions[0] == GetAction<MoveAction>() && targetEnemyUnit != null)
                {
                    if (targetEnemyUnit.health.IsDead())
                    {
                        unit.stateController.SetToDefaultState();
                        CancelAction();
                        TurnManager.Instance.FinishTurn(unit);
                        return;
                    }

                    if (unit.UnitEquipment.RangedWeaponEquipped() && unit.UnitEquipment.HasValidAmmunitionEquipped())
                    {
                        Unit closestEnemy = unit.vision.GetClosestEnemy(true);
                        float minShootRange = unit.unitMeshManager.GetHeldRangedWeapon().itemData.Item.Weapon.MinRange;

                        // If the closest enemy is too close and this Unit doesn't have a melee weapon, retreat back a few spaces
                        if (TacticsPathfindingUtilities.CalculateWorldSpaceDistance_XYZ(unit.GridPosition, closestEnemy.GridPosition) < minShootRange + 1.4f)
                        {
                            // TO DO: If the Unit has a melee weapon, switch to it (need to do inventory system first)
                            // else
                            if (unit.stats.CanFightUnarmed)
                            {
                                if (GetAction<MeleeAction>().IsInAttackRange(closestEnemy))
                                    GetAction<MeleeAction>().QueueAction(closestEnemy);
                                //else
                                    //GetAction<MoveAction>().QueueAction(GetAction<MeleeAction>().GetNearestAttackPosition(unit.GridPosition, closestEnemy));
                                return;
                            }

                            // Else flee somewhere
                            StartFlee(unit.vision.GetClosestEnemy(true), Mathf.RoundToInt(minShootRange + Random.Range(2, unit.unitMeshManager.GetHeldRangedWeapon().itemData.Item.Weapon.MaxRange - 2)));
                        }
                        else if (GetAction<ShootAction>().IsInAttackRange(targetEnemyUnit))
                        {
                            // Shoot the target enemy
                            if (unit.unitMeshManager.GetHeldRangedWeapon().isLoaded)
                                GetAction<ShootAction>().QueueAction(targetEnemyUnit);
                            else
                                GetAction<ReloadAction>().QueueAction();
                            return;
                        }
                        else
                        {
                            //GetAction<MoveAction>().QueueAction(GetAction<ShootAction>().GetNearestAttackPosition(unit.GridPosition, targetEnemyUnit));
                            //return;
                        }
                    }
                    else if (unit.UnitEquipment.MeleeWeaponEquipped() || GetAction<MeleeAction>().CanFightUnarmed)
                    {
                        if (GetAction<MeleeAction>().IsInAttackRange(targetEnemyUnit))
                        {
                            // Melee attack the target enemy
                            GetAction<MeleeAction>().QueueAction(targetEnemyUnit);
                            return;
                        }
                        else
                        {
                            //GetAction<MoveAction>().QueueAction(GetAction<MeleeAction>().GetNearestAttackPosition(unit.GridPosition, targetEnemyUnit));
                            //return;
                        }
                    }
                }

                // If not attacking, get/determine the next action
                if (queuedActions.Count > 0)
                    StartCoroutine(GetNextQueuedAction());
                else
                    DetermineAction();
            }
        }

        public override void SkipTurn()
        {
            base.SkipTurn();

            // unit.stats.UseAP(unit.stats.currentAP);
            TurnManager.Instance.FinishTurn(unit);
        }

        public override IEnumerator GetNextQueuedAction()
        {
            while (isAttacking)
                yield return null;

            if (queuedActions.Count > 0 && isPerformingAction == false)
            {
                int APRemainder = unit.stats.UseAPAndGetRemainder(queuedAPs[0]);
                lastQueuedAction = queuedActions[0];
                if (unit.health.IsDead())
                {
                    ClearActionQueue(true);
                    yield break;
                }

                if (APRemainder <= 0)
                {
                    if (queuedActions.Count > 0) // This can become null after a time tick update
                    {
                        isPerformingAction = true;
                        queuedActions[0].TakeAction();
                    }
                    else
                    {
                        CancelAction();
                        TurnManager.Instance.FinishTurn(unit);
                    }
                }
                else
                {
                    isPerformingAction = false;
                    queuedAPs[0] = APRemainder;
                    TurnManager.Instance.FinishTurn(unit);
                }
            }
            else if (queuedActions.Count == 0)
            {
                Debug.LogWarning("Queued action is null for " + unit.name);
                TurnManager.Instance.FinishTurn(unit);
            }
        }

        public override void FinishAction()
        {
            base.FinishAction();

            // If the character has no AP remaining, end their turn
            if (unit.stats.currentAP <= 0)
            {
                if (unit.isMyTurn)
                    TurnManager.Instance.FinishTurn(unit);
            }

            if (unit.isMyTurn)
                TurnManager.Instance.StartNextUnitsTurn(unit);
        }

        public void DetermineAction()
        {
            if (unit.unitActionHandler.targetEnemyUnit != null && unit.unitActionHandler.targetEnemyUnit.health.IsDead())
                unit.unitActionHandler.SetTargetEnemyUnit(null);

            switch (unit.stateController.currentState)
            {
                case State.Idle:
                    SkipTurn();
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
                case State.InspectSound:
                    InspectSound();
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
            if (unit.UnitEquipment.RangedWeaponEquipped())
            {
                Unit closestEnemy = unit.vision.GetClosestEnemy(true);
                float minShootRange = unit.unitMeshManager.GetHeldRangedWeapon().itemData.Item.Weapon.MinRange;

                // If the closest enemy is too close and this Unit doesn't have a melee weapon, retreat back a few spaces or switch to a melee weapon
                if (closestEnemy != null && TacticsPathfindingUtilities.CalculateWorldSpaceDistance_XYZ(unit.GridPosition, closestEnemy.GridPosition) < minShootRange + 1.4f)
                {
                    // TO DO: If the Unit has a melee weapon, switch to it

                    // Else flee somewhere
                    StartFlee(closestEnemy, Mathf.RoundToInt(minShootRange + Random.Range(2, unit.unitMeshManager.GetHeldRangedWeapon().itemData.Item.Weapon.MaxRange - 2)));
                }
            }

            FindBestTargetEnemy();

            if (shouldStopChasing || TacticsPathfindingUtilities.CalculateWorldSpaceDistance_XYZ(startChaseGridPosition, unit.GridPosition) >= maxChaseDistance)
            {
                shouldStopChasing = true;
                GetAction<MoveAction>().QueueAction(LevelGrid.GetGridPosition(defaultPosition));
                return;
            }

            // If there's no target enemy Unit, try to find one, else switch States
            if (targetEnemyUnit == null || targetEnemyUnit.health.IsDead())
            {
                SetTargetEnemyUnit(null);
                unit.stateController.SetToDefaultState();
                DetermineAction();
                return;
            }

            if (IsInAttackRange(targetEnemyUnit, false))
                ChooseCombatAction();
            else
                PursueTargetEnemy();
        }

        public void ChooseCombatAction() => StartCoroutine(ChooseCombatAction_Coroutine());

        public IEnumerator ChooseCombatAction_Coroutine()
        {
            BaseAttackAction chosenCombatAction = null;
            npcAIActions.Clear();

            // Loop through all combat actions
            for (int i = 0; i < availableCombatActions.Count; i++)
            {
                if (availableCombatActions[i].IsValidAction() == false)
                    continue;

                if (unit.stats.HasEnoughEnergy(availableCombatActions[i].GetEnergyCost()) == false)
                    continue;

                // Loop through every grid position in range of the combat action
                foreach (GridPosition gridPositionInRange in availableCombatActions[i].GetActionGridPositionsInRange(unit.GridPosition))
                {
                    // For each of these grid positions, get the best one for this combat action
                    npcAIActions.Add(availableCombatActions[i].GetNPCAIAction_ActionGridPosition(gridPositionInRange));
                }
            }

            // Sort the list of best NPCAIActions by the highest action value
            npcAIActions = npcAIActions.OrderByDescending(npcAIAction => npcAIAction.actionValue).ToList();

            // If no NPCAIActions were valid, just pursue the target enemy
            if (npcAIActions.Count == 0 || npcAIActions[0].actionValue <= 0)
                PursueTargetEnemy();
            else
            {
                List<NPCAIAction> filteredNPCAIActions = ListPool<NPCAIAction>.Claim();
                List<int> accumulatedWeights = ListPool<int>.Claim();
                int totalWeight = 0;

                // Get rid of any NPCAIActions that weren't valid (have a less than or equal to 0 value)
                filteredNPCAIActions = npcAIActions.Where(npcAIAction => npcAIAction.actionValue > 0).ToList();

                // Add each NPCAIAction's actionValue to the totalWeight, we'll use this for a weighted random selection of the best NPCAIActions
                for (int i = 0; i < filteredNPCAIActions.Count; i++)
                {
                    totalWeight += filteredNPCAIActions[i].actionValue;
                    accumulatedWeights.Add(totalWeight);
                }

                // Generate a random number between 0 and the total weight
                int randomWeight = Random.Range(0, totalWeight);

                // Find the index of the first accumulated weight greater than or equal to the random weight
                int selectedIndex = accumulatedWeights.FindIndex(weight => weight >= randomWeight);

                // Get the BaseAction from the corresponding NPCAIAction
                chosenCombatAction = filteredNPCAIActions[selectedIndex].baseAction as BaseAttackAction;

                // If an action was found
                if (chosenCombatAction != null)
                {
                    Unit unitAtActionGridPosition = LevelGrid.Instance.GetUnitAtGridPosition(filteredNPCAIActions[selectedIndex].actionGridPosition);
                    if (unitAtActionGridPosition != null && unitAtActionGridPosition.unitActionHandler.isMoving)
                    {
                        while (unitAtActionGridPosition.unitActionHandler.isMoving)
                            yield return null;

                        if (chosenCombatAction.IsInAttackRange(unitAtActionGridPosition) == false) // If the target Unit moved out of range
                        {
                            targetEnemyUnit = unitAtActionGridPosition;
                            PursueTargetEnemy();

                            ListPool<NPCAIAction>.Release(filteredNPCAIActions);
                            ListPool<int>.Release(accumulatedWeights);
                            yield break;
                        }
                    }

                    // Set the unit's target attack position to the one corresponding to the NPCAIAction that was chosen
                    SetQueuedAttack(chosenCombatAction, filteredNPCAIActions[selectedIndex].actionGridPosition);
                    chosenCombatAction.QueueAction(filteredNPCAIActions[selectedIndex].actionGridPosition);
                }
                else // If no combat action was found, just move towards the target enemy
                    PursueTargetEnemy();

                ListPool<NPCAIAction>.Release(filteredNPCAIActions);
                ListPool<int>.Release(accumulatedWeights);
            }
        }

        public void StartFight()
        {
            unit.stateController.SetCurrentState(State.Fight);
            FindBestTargetEnemy();
            ClearActionQueue(false);

            if (targetEnemyUnit == null || targetEnemyUnit.health.IsDead())
            {
                SetTargetEnemyUnit(null);
                unit.stateController.SetToDefaultState();
                DetermineAction();
                return;
            }
        }

        void PursueTargetEnemy()
        {
            if (targetEnemyUnit == null)
            {
                DetermineAction();
                return;
            }

            // If there's no space around the enemy unit, try to find another enemy to attack
            if (targetEnemyUnit.IsCompletelySurrounded(unit.GetAttackRange(targetEnemyUnit, false)))
            {
                SwitchTargetEnemies(out Unit oldEnemy, out Unit newEnemy);
                GetAction<MoveAction>().QueueAction(LevelGrid.Instance.FindNearestValidGridPosition(targetEnemyUnit.GridPosition, unit, 10));
            }
            else
                GetAction<MoveAction>().QueueAction(LevelGrid.Instance.FindNearestValidGridPosition(targetEnemyUnit.GridPosition, unit, 10));
        }

        void FindBestTargetEnemy()
        {
            if (unit.vision.knownEnemies.Count > 0)
            {
                // If there's only one visible enemy, then there's no need to figure out the best enemy AI action
                if (unit.vision.knownEnemies.Count == 1)
                    SetTargetEnemyUnit(unit.vision.knownEnemies[0]);
                else
                {
                    npcAIActions.Clear();
                    if (unit.UnitEquipment.RangedWeaponEquipped() && unit.UnitEquipment.HasValidAmmunitionEquipped())
                    {
                        ShootAction shootAction = GetAction<ShootAction>();
                        for (int i = 0; i < unit.vision.knownEnemies.Count; i++)
                        {
                            npcAIActions.Add(shootAction.GetNPCAIAction_Unit(unit.vision.knownEnemies[i]));
                        }

                        if (npcAIActions.Count == 1 && npcAIActions[0].actionValue <= -1)
                        {
                            TurnManager.Instance.FinishTurn(unit);
                            return;
                        }

                        SetTargetEnemyUnit(LevelGrid.Instance.GetUnitAtGridPosition(shootAction.GetBestNPCAIActionFromList(npcAIActions).actionGridPosition));

                    }
                    else if (unit.UnitEquipment.MeleeWeaponEquipped() || GetAction<MeleeAction>().CanFightUnarmed)
                    {
                        MeleeAction meleeAction = GetAction<MeleeAction>();
                        for (int i = 0; i < unit.vision.knownEnemies.Count; i++)
                        {
                            npcAIActions.Add(meleeAction.GetNPCAIAction_Unit(unit.vision.knownEnemies[i]));
                        }

                        if (npcAIActions.Count == 1 && npcAIActions[0].actionValue <= -1)
                        {
                            TurnManager.Instance.FinishTurn(unit);
                            return;
                        }

                        SetTargetEnemyUnit(LevelGrid.Instance.GetUnitAtGridPosition(meleeAction.GetBestNPCAIActionFromList(npcAIActions).actionGridPosition));
                    }
                    else
                    {
                        StartFlee(unit.vision.GetClosestEnemy(true), defaultFleeDistance);
                        DetermineAction();
                        return;
                    }
                }
            }
        }

        void GetRandomTargetEnemy()
        {
            if (unit.vision.knownEnemies.Count > 0)
                targetEnemyUnit = unit.vision.knownEnemies[Random.Range(0, unit.vision.knownEnemies.Count)];
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

        public override void SetTargetEnemyUnit(Unit target)
        {
            if (target != null && target != targetEnemyUnit)
            {
                startChaseGridPosition = unit.GridPosition;
                shouldStopChasing = false;
            }

            base.SetTargetEnemyUnit(target);
        }

        #endregion

        #region Flee
        void Flee()
        {
            // If there's no Unit to flee from or if the Unit to flee from died
            if (unitToFleeFrom == null || unitToFleeFrom.health.IsDead())
            {
                unit.stateController.SetToDefaultState(); // Variables are reset in this method
                DetermineAction();
                return;
            }

            float distanceFromUnitToFleeFrom = TacticsPathfindingUtilities.CalculateWorldSpaceDistance_XZ(unitToFleeFrom.GridPosition, unit.GridPosition);

            // If the Unit has fled far enough
            if (distanceFromUnitToFleeFrom >= fleeDistance)
            {
                unit.stateController.SetToDefaultState(); // Variables are also reset in this method
                DetermineAction();
                return;
            }

            // The enemy this Unit is fleeing from has moved closer or they have arrived at their flee destination, but are still too close to the enemy, so get a new flee destination
            MoveAction moveAction = GetAction<MoveAction>();
            if (unit.GridPosition == moveAction.targetGridPosition || (unitToFleeFrom.GridPosition != unitToFleeFrom_PreviousGridPosition && (unitToFleeFrom_PreviousDistance == 0f || distanceFromUnitToFleeFrom + 2f <= unitToFleeFrom_PreviousDistance)))
                needsNewFleeDestination = true;

            GridPosition targetGridPosition = moveAction.targetGridPosition;
            if (needsNewFleeDestination)
            {
                needsNewFleeDestination = false;
                unitToFleeFrom_PreviousDistance = TacticsPathfindingUtilities.CalculateWorldSpaceDistance_XZ(unitToFleeFrom.GridPosition, unit.GridPosition);
                targetGridPosition = GetFleeDestination();
            }

            // If there was no valid flee position, just grab a random position within range
            if (moveAction.targetGridPosition == unit.GridPosition)
                targetGridPosition = LevelGrid.Instance.GetRandomGridPositionInRange(unitToFleeFrom.GridPosition, unit, fleeDistance, fleeDistance + 15);

            moveAction.QueueAction(targetGridPosition);
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
                unit.stateController.SetToDefaultState();
                DetermineAction();
                return;
            }

            if (TacticsPathfindingUtilities.CalculateWorldSpaceDistance_XZ(unit.GridPosition, leader.GridPosition) <= stopFollowDistance)
                TurnManager.Instance.FinishTurn(unit);
            else if (isMoving == false)
                GetAction<MoveAction>().QueueAction(leader.unitActionHandler.GetAction<TurnAction>().GetGridPositionBehindUnit());
        }

        public Unit Leader() => leader;

        public void SetLeader(Unit newLeader) => leader = newLeader;

        public void SetShouldFollowLeader(bool shouldFollowLeader) => this.shouldFollowLeader = shouldFollowLeader;
        #endregion

        #region Inspect Sound
        void InspectSound()
        {
            if (needsNewSoundInspectPosition)
            {
                if (inspectSoundIterations == maxInspectSoundIterations)
                {
                    unit.stateController.SetToDefaultState();
                    DetermineAction();
                    return;
                }

                needsNewSoundInspectPosition = false;

                inspectSoundGridPosition = LevelGrid.Instance.GetRandomGridPositionInRange(soundGridPosition, unit, 0 + inspectSoundIterations, 2 + inspectSoundIterations, true);
                GetAction<MoveAction>().QueueAction(inspectSoundGridPosition);
            }
            else if (Vector3.Distance(inspectSoundGridPosition.WorldPosition(), transform.position) <= 0.1f)
            {
                // Get a new Inspect Sound Position when the current one is reached
                inspectSoundIterations++;
                needsNewSoundInspectPosition = true;
                InspectSound();
            }
            else if (isMoving == false)
            {
                // Get a new Inspect Sound Position if there's now another Unit or obstruction there
                if (LevelGrid.Instance.GridPositionObstructed(inspectSoundGridPosition))
                    inspectSoundGridPosition = LevelGrid.Instance.GetNearestSurroundingGridPosition(inspectSoundGridPosition, unit.GridPosition, LevelGrid.diaganolDistance, true);

                GetAction<MoveAction>().QueueAction(inspectSoundGridPosition);
            }
        }

        public void SetSoundGridPosition(Vector3 soundPosition)
        {
            soundGridPosition = LevelGrid.GetGridPosition(soundPosition);
            maxInspectSoundIterations = Random.Range(3, 7);
        }
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

                GridPosition patrolPointGridPosition = LevelGrid.GetGridPosition(patrolPoints[currentPatrolPointIndex]);

                // If the Patrol Point is set to an invalid Grid Position
                if (LevelGrid.IsValidGridPosition(patrolPointGridPosition) == false)
                {
                    // Debug.LogWarning(patrolPointGridPosition + " is not a valid grid position...");
                    IncreasePatrolPointIndex();
                    return;
                }
                // If there's another Unit currently on the Patrol Point or Alternative Patrol Point
                else if ((hasAlternativePatrolPoint == false && LevelGrid.Instance.GridPositionObstructed(patrolPointGridPosition) && LevelGrid.Instance.GetUnitAtGridPosition(patrolPointGridPosition) != unit)
                    || (hasAlternativePatrolPoint && LevelGrid.Instance.GridPositionObstructed(GetAction<MoveAction>().targetGridPosition) && LevelGrid.Instance.GetUnitAtGridPosition(GetAction<MoveAction>().targetGridPosition) != unit))
                {
                    // Increase the iteration count just in case we had to look for an Alternative Patrol Point due to something obstructing the current Target Grid Position
                    patrolIterationCount++;

                    // Find the nearest Grid Position to the Patrol Point
                    GridPosition nearestGridPositionToPatrolPoint = LevelGrid.Instance.FindNearestValidGridPosition(patrolPointGridPosition, unit, 7);
                    if (patrolPointGridPosition == nearestGridPositionToPatrolPoint)
                        IncreasePatrolPointIndex();

                    hasAlternativePatrolPoint = true;
                    GetAction<MoveAction>().SetTargetGridPosition(nearestGridPositionToPatrolPoint);

                    if (nearestGridPositionToPatrolPoint != patrolPointGridPosition && LevelGrid.Instance.GridPositionObstructed(nearestGridPositionToPatrolPoint) == false)
                        patrolIterationCount = 0;
                }

                // If the Unit has arrived at their current Patrol Point or Alternative Patrol Point position
                if (Vector3.Distance(GetAction<MoveAction>().targetGridPosition.WorldPosition(), transform.position) <= 0.1f)
                {
                    if (hasAlternativePatrolPoint)
                        hasAlternativePatrolPoint = false;

                    // Set the Unit's Target Grid Position as the next Patrol Point
                    IncreasePatrolPointIndex();
                    patrolPointGridPosition = LevelGrid.GetGridPosition(patrolPoints[currentPatrolPointIndex]);
                    GetAction<MoveAction>().SetTargetGridPosition(patrolPointGridPosition);
                }
                // Otherwise, assign their target position to the Patrol Point if it's not already set
                else if (hasAlternativePatrolPoint == false && GetAction<MoveAction>().targetGridPosition.WorldPosition() != patrolPoints[currentPatrolPointIndex])
                {
                    GetAction<MoveAction>().SetTargetGridPosition(patrolPointGridPosition);

                    // Don't reset the patrol iteration count if the next target position is the Unit's current position, because we'll need to iterate through Patrol again
                    if (GetAction<MoveAction>().targetGridPosition != unit.GridPosition)
                        patrolIterationCount = 0;
                }

                // Queue the Move Action if the Unit isn't already moving
                if (isMoving == false)
                    GetAction<MoveAction>().QueueAction(GetAction<MoveAction>().targetGridPosition);
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
            GridPosition patrolPointGridPosition = LevelGrid.GetGridPosition(patrolPoints[currentPatrolPointIndex]);
            GetAction<MoveAction>().SetTargetGridPosition(patrolPointGridPosition);
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
                if (wanderGridPosition == unit.GridPosition)
                    TurnManager.Instance.FinishTurn(unit);
                else
                {
                    wanderPositionSet = true;
                    GetAction<MoveAction>().SetTargetGridPosition(wanderGridPosition);
                }

                // Queue the Move Action if the Unit isn't already moving
                if (isMoving == false)
                    GetAction<MoveAction>().QueueAction(wanderGridPosition);
            }
            // If the NPC has arrived at their destination
            else if (Vector3.Distance(wanderGridPosition.WorldPosition(), transform.position) <= 0.1f)
            {
                // Get a new Wander Position when the current one is reached
                wanderPositionSet = false;
                Wander();
            }
            else if (isMoving == false)
            {
                // Get a new Wander Position if there's now another Unit or obstruction there
                if (LevelGrid.Instance.GridPositionObstructed(wanderGridPosition))
                    wanderGridPosition = GetNewWanderPosition();

                GetAction<MoveAction>().QueueAction(wanderGridPosition);
            }
        }

        GridPosition GetNewWanderPosition()
        {
            float distance = Random.Range(minWanderDistance, maxWanderDistance);
            Vector3 randomDirection = Random.insideUnitSphere;
            randomDirection.y = 0;
            Vector3 randomPosition = randomDirection * distance + transform.position;
            return LevelGrid.GetGridPosition((Vector3)AstarPath.active.GetNearest(randomPosition).node.position);
        }
        #endregion

        public void ResetToDefaults()
        {
            // Fight
            shouldStopChasing = false;

            // Flee
            needsNewFleeDestination = true;
            unitToFleeFrom = null;
            unitToFleeFrom_PreviousDistance = 0f;
            fleeDistance = 0;

            // Inspect Sound
            needsNewSoundInspectPosition = true;
            inspectSoundIterations = 0;

            // Patrol
            hasAlternativePatrolPoint = false;
            initialPatrolPointSet = false;
            patrolIterationCount = 0;

            // Wander
            wanderPositionSet = false;
        }
    }
}
