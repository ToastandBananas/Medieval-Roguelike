using Pathfinding.Util;
using System.Collections.Generic;
using UnityEngine;
using GridSystem;
using System.Collections;
using InventorySystem;
using UnitSystem.ActionSystem.GOAP.Goals;
using UnitSystem.ActionSystem.Actions;
using UnitSystem.ActionSystem.GOAP;
using UnitSystem.ActionSystem.GOAP.GoalActions;

namespace UnitSystem.ActionSystem
{
    public class NPCActionHandler : UnitActionHandler
    {
        /* // Old variables
        [Header("Fight State")]
        [SerializeField] float maxChaseDistance = 25f;
        public float MaxChaseDistance => maxChaseDistance;
        public GridPosition StartChaseGridPosition { get; private set; }
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
        [SerializeField] bool shouldFollowLeader;
        public bool ShouldFollowLeader => shouldFollowLeader;

        [Header("Inspect Sound State")]
        GridPosition inspectSoundGridPosition;
        public GridPosition SoundGridPosition { get; private set; }
        int inspectSoundIterations;
        int maxInspectSoundIterations;
        bool needsNewSoundInspectPosition = true;

        [Header("Patrol State")]
        [SerializeField] Vector3[] patrolPoints;
        public int CurrentPatrolPointIndex { get; private set; }
        bool initialPatrolPointSet, hasAlternativePatrolPoint;
        int patrolIterationCount;
        readonly int maxPatrolIterations = 5;

        [Header("Wander State")]
        [SerializeField] int minWanderDistance = 5;
        [SerializeField] int maxWanderDistance = 20;
        GridPosition wanderGridPosition;
        bool wanderPositionSet;
        */

        [Header("Goal Planner")]
        [SerializeField] GoalPlanner goalPlanner;
        [SerializeField] Vector3 defaultPosition;

        public GoalPlanner GoalPlanner => goalPlanner;

        public List<Goal_Base> Goals { get; private set; }

        List<NPCAIAction> npcAIActions = new();

        public override void Awake()
        {
            base.Awake();

            if (Unit.IsNPC)
            {
                Goals = new List<Goal_Base>();
                Goals.AddRange(GetComponentsInChildren<Goal_Base>());
            }
        }

        void Start()
        {
            if (defaultPosition == Vector3.zero) defaultPosition = Unit.WorldPosition;
        }

        public override void TakeTurn()
        {
            if (Unit.IsMyTurn && !Unit.Health.IsDead)
            {
                Unit.Vision.FindVisibleUnitsAndObjects();

                if (CanPerformActions == false || Unit.Stats.CurrentAP <= 0)
                {
                    SkipTurn(); // Unit can't do anything, so skip their turn
                    return;
                }
                else if (QueuedActions.Count == 0 && QueuedAttack != null)
                {
                    bool canAttack = false;
                    List<GridPosition> actionGridPositionsInRange = ListPool<GridPosition>.Claim();
                    actionGridPositionsInRange.AddRange(QueuedAttack.GetActionGridPositionsInRange(Unit.GridPosition));

                    // Check if there's any enemies in the valid attack positions
                    for (int i = 0; i < actionGridPositionsInRange.Count; i++)
                    {
                        if (canAttack) break;

                        foreach (GridPosition gridPosition in QueuedAttack.GetActionAreaGridPositions(actionGridPositionsInRange[i]))
                        {
                            if (!LevelGrid.HasUnitAtGridPosition(gridPosition, out Unit unitAtGridPosition))
                                continue;

                            if (Unit.Alliance.IsEnemy(unitAtGridPosition))
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
                        QueuedAttack.QueueAction(TargetEnemyUnit);
                        return;
                    }
                }
                else if (Unit.StateController.CurrentState == GoalState.Fight && QueuedActions.Count > 0 && QueuedActions[0] == MoveAction && TargetEnemyUnit != null)
                {
                    if (TargetEnemyUnit.Health.IsDead)
                    {
                        CancelActions();
                        TurnManager.Instance.FinishTurn(Unit);
                        return;
                    }

                    if (Unit.UnitEquipment.RangedWeaponEquipped && Unit.UnitEquipment.HasValidAmmunitionEquipped())
                    {
                        Unit closestEnemy = Unit.Vision.GetClosestEnemy(true);
                        float minShootRange = Unit.UnitMeshManager.GetHeldRangedWeapon().ItemData.Item.Weapon.MinRange;

                        // If the closest enemy is too close and this Unit doesn't have a melee weapon, retreat back a few spaces
                        if (Vector3.Distance(Unit.WorldPosition, closestEnemy.WorldPosition) < minShootRange + LevelGrid.diaganolDistance)
                        {
                            if (Unit.UnitEquipment.OtherWeaponSet_IsMelee())
                            {
                                GetAction<Action_SwapWeaponSet>().QueueAction();
                                return;
                            }
                            else if (Unit.UnitInventoryManager.ContainsMeleeWeaponInAnyInventory(out ItemData weaponItemData))
                            {
                                GetAction<Action_SwapWeaponSet>().QueueAction();
                                GetAction<Action_Equip>().QueueAction(weaponItemData, weaponItemData.Item.Equipment.EquipSlot, null);
                                return;
                            }
                            else if (Unit.Stats.CanFightUnarmed && Random.Range(0, 2) == 0) // 50% chance to fight unarmed vs to flee
                            {
                                if (GetAction<Action_Melee>().IsInAttackRange(closestEnemy, Unit.GridPosition, closestEnemy.GridPosition))
                                    GetAction<Action_Melee>().QueueAction(closestEnemy);
                                else
                                    MoveAction.QueueAction(GetAction<Action_Melee>().GetNearestAttackPosition(Unit.GridPosition, closestEnemy));
                                return;
                            }

                            // Else flee somewhere
                            GoalAction_Flee fleeAction = goalPlanner.GetGoalAction(typeof(GoalAction_Flee)) as GoalAction_Flee;
                            if (fleeAction != null)
                                fleeAction.StartFlee(closestEnemy, Mathf.RoundToInt(minShootRange + Random.Range(2, Unit.UnitMeshManager.GetHeldRangedWeapon().ItemData.Item.Weapon.MaxRange - 2)));
                        }
                        else if (GetAction<Action_Shoot>().IsInAttackRange(TargetEnemyUnit, Unit.GridPosition, TargetEnemyUnit.GridPosition))
                        {
                            // Shoot the target enemy
                            if (Unit.UnitMeshManager.GetHeldRangedWeapon().IsLoaded)
                                GetAction<Action_Shoot>().QueueAction(TargetEnemyUnit);
                            else
                                GetAction<Action_Reload>().QueueAction();
                            return;
                        }
                        else
                        {
                            MoveAction.QueueAction(GetAction<Action_Shoot>().GetNearestAttackPosition(Unit.GridPosition, TargetEnemyUnit));
                            return;
                        }
                    }
                    else if (Unit.UnitEquipment.MeleeWeaponEquipped || Unit.Stats.CanFightUnarmed)
                    {
                        // Melee attack the target enemy
                        if (GetAction<Action_Melee>().IsInAttackRange(TargetEnemyUnit, Unit.GridPosition, TargetEnemyUnit.GridPosition))
                            GetAction<Action_Melee>().QueueAction(TargetEnemyUnit);
                        else
                            MoveAction.QueueAction(GetAction<Action_Melee>().GetNearestAttackPosition(Unit.GridPosition, TargetEnemyUnit));
                        return;
                    }
                }

                // If not attacking, get/determine the next action
                if (QueuedActions.Count > 0)
                    StartCoroutine(GetNextQueuedAction());
                else
                    DetermineAction();
            }
        }

        public override void SkipTurn()
        {
            base.SkipTurn();

            // unit.stats.UseAP(unit.stats.currentAP);
            TurnManager.Instance.FinishTurn(Unit);
        }

        public override IEnumerator GetNextQueuedAction()
        {
            while (IsAttacking || Unit.UnitAnimator.beingKnockedBack)
                yield return null;

            if (!Unit.IsMyTurn)
                yield break;

            if (QueuedActions.Count > 0 && QueuedAPs.Count > 0 && !IsPerformingAction)
            {
                int APRemainder = Unit.Stats.UseAPAndGetRemainder(QueuedAPs[0]);
                LastQueuedAction = QueuedActions[0];
                if (Unit.Health.IsDead)
                {
                    ClearActionQueue(true, true);
                    yield break;
                }

                if (APRemainder <= 0)
                {
                    if (QueuedActions.Count > 0) // This can become null after a time tick update
                    {
                        IsPerformingAction = true;
                        QueuedActions[0].TakeAction();
                    }
                    else
                    {
                        CancelActions();
                        TurnManager.Instance.FinishTurn(Unit);
                    }
                }
                else
                {
                    IsPerformingAction = false;
                    QueuedAPs[0] = APRemainder;
                    TurnManager.Instance.FinishTurn(Unit);
                }
            }
            else if (QueuedActions.Count == 0)
            {
                // Debug.LogWarning("Queued action is null for " + Unit.name);
                TurnManager.Instance.FinishTurn(Unit);
            }
        }

        public override void FinishAction()
        {
            base.FinishAction();

            // If the character has no AP remaining, end their turn
            if (Unit.Stats.CurrentAP <= 0)
                TurnManager.Instance.FinishTurn(Unit);
            else if (Unit.IsMyTurn)
                TurnManager.Instance.StartNextUnitsTurn(Unit);
        }

        public void DetermineAction()
        {
            if (Unit.UnitActionHandler.TargetEnemyUnit != null && Unit.UnitActionHandler.TargetEnemyUnit.Health.IsDead)
                Unit.UnitActionHandler.SetTargetEnemyUnit(null);

            goalPlanner.DetermineGoal();

            /* // Old method
            switch (Unit.StateController.CurrentState)
            {
                case GoalState.Idle:
                    SkipTurn();
                    break;
                case GoalState.Patrol:
                    Patrol();
                    break;
                case GoalState.Wander:
                    Wander();
                    break;
                case GoalState.Follow:
                    Follow();
                    break;
                case GoalState.InspectSound:
                    InspectSound();
                    break;
                case GoalState.Fight:
                    Fight();
                    break;
                case GoalState.Flee:
                    Flee();
                    break;
                case GoalState.Hunt:
                    break;
                case GoalState.FindFood:
                    break;
                default:
                    break;
            }*/
        }

        public void OnTickGoals()
        {
            for (int i = 0; i < Goals.Count; i++)
                Goals[i].OnTickGoal();
        }

        public Vector3 DefaultPosition => defaultPosition;

        /*// Old Methods
        #region Fight
        void Fight()
        {
            if (Unit.UnitEquipment.IsUnarmed)
            {
                bool weaponNearby = TryFindNearbyWeapon(out LooseItem foundLooseWeapon, out float distanceToWeapon);

                // If a weapon was found and it's next to this Unit, pick it up (the weapon is likely there from fumbling it)
                if (weaponNearby && distanceToWeapon <= LevelGrid.diaganolDistance)
                {
                    InteractAction.QueueAction(foundLooseWeapon);
                    return;
                }

                // If no weapons whatsoever are equipped
                if (Unit.UnitEquipment.OtherWeaponSet_IsEmpty())
                {
                    // Equip any weapon from their inventory if they have one
                    if (Unit.UnitInventoryManager.ContainsMeleeWeaponInAnyInventory(out ItemData weaponItemData))
                    {
                        GetAction<EquipAction>().QueueAction(weaponItemData, weaponItemData.Item.Equipment.EquipSlot, null);
                        return;
                    }
                    // Else, try pickup the nearby weapon
                    else if (weaponNearby)
                    {
                        InteractAction.QueueAction(foundLooseWeapon);
                        return;
                    }
                }
                else // If there are weapons in the other weapon set
                {
                    // Swap to their melee weapon set if they have one
                    if (Unit.UnitEquipment.OtherWeaponSet_IsMelee())
                    {
                        GetAction<SwapWeaponSetAction>().QueueAction();
                        return;
                    }
                    // Else, swap to their ranged weapon set if they have ammo
                    else if (Unit.UnitEquipment.OtherWeaponSet_IsRanged() && Unit.UnitEquipment.HasValidAmmunitionEquipped(Unit.UnitEquipment.GetRangedWeaponFromOtherWeaponSet().Item as RangedWeapon))
                    {
                        GetAction<SwapWeaponSetAction>().QueueAction();
                        return;
                    }
                }
            }
            else if (Unit.UnitEquipment.RangedWeaponEquipped)
            {
                Unit closestEnemy = Unit.Vision.GetClosestEnemy(true);
                float minShootRange = Unit.UnitMeshManager.GetHeldRangedWeapon().ItemData.Item.Weapon.MinRange;

                // If the closest enemy is too close and this Unit doesn't have a melee weapon, retreat back a few spaces or switch to a melee weapon
                if (closestEnemy != null && Vector3.Distance(Unit.WorldPosition, closestEnemy.WorldPosition) < minShootRange + LevelGrid.diaganolDistance)
                {
                    // If the Unit has a melee weapon, switch to it
                    if (Unit.UnitEquipment.OtherWeaponSet_IsMelee())
                    {
                        GetAction<SwapWeaponSetAction>().QueueAction();
                        return;
                    }
                    else if (Unit.UnitInventoryManager.ContainsMeleeWeaponInAnyInventory(out ItemData weaponItemData))
                    {
                        GetAction<SwapWeaponSetAction>().QueueAction();
                        GetAction<EquipAction>().QueueAction(weaponItemData, weaponItemData.Item.Equipment.EquipSlot, null);
                        return;
                    }
                    else
                    {
                        // Else flee somewhere
                        StartFlee(closestEnemy, Mathf.RoundToInt(minShootRange + Random.Range(2, Unit.UnitMeshManager.GetHeldRangedWeapon().ItemData.Item.Weapon.MaxRange - 2)));
                        DetermineAction();
                        return;
                    }
                }
            }

            if (shouldStopChasing)
            {
                if (Unit.GridPosition == LevelGrid.GetGridPosition(defaultPosition))
                {
                    SetTargetEnemyUnit(null);
                    Unit.StateController.SetToDefaultState();
                    DetermineAction();
                }
                else
                    MoveAction.QueueAction(LevelGrid.GetGridPosition(defaultPosition));
                return;
            }
            else
            {
                if (Vector3.Distance(StartChaseGridPosition.WorldPosition, Unit.WorldPosition) >= maxChaseDistance)
                {
                    shouldStopChasing = true;
                    MoveAction.QueueAction(LevelGrid.GetGridPosition(defaultPosition));
                    return;
                }
            }

            FindBestTargetEnemy();

            // If there's no target enemy Unit, try to find one, else switch States
            if (TargetEnemyUnit == null || TargetEnemyUnit.Health.IsDead)
            {
                SetTargetEnemyUnit(null);
                Unit.StateController.SetToDefaultState();
                DetermineAction();
                return;
            }

            if (IsInAttackRange(TargetEnemyUnit, false))
                ChooseCombatAction();
            else
                PursueTargetEnemy();
        }

        public void SetStartChaseGridPosition(GridPosition newGridPosition)
        {
            StartChaseGridPosition = newGridPosition;
            shouldStopChasing = false;
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

                if (Unit.Stats.HasEnoughEnergy(availableCombatActions[i].InitialEnergyCost()) == false)
                    continue;

                // Loop through every grid position in range of the combat action
                foreach (GridPosition gridPositionInRange in availableCombatActions[i].GetActionGridPositionsInRange(Unit.GridPosition))
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

                // Get the BaseAttackAction from the corresponding NPCAIAction
                chosenCombatAction = filteredNPCAIActions[selectedIndex].baseAction as BaseAttackAction;

                // If an action was found
                if (chosenCombatAction != null)
                {
                    Unit unitAtActionGridPosition = LevelGrid.GetUnitAtGridPosition(filteredNPCAIActions[selectedIndex].actionGridPosition);
                    if (unitAtActionGridPosition != null && unitAtActionGridPosition.UnitActionHandler.MoveAction.IsMoving)
                    {
                        while (unitAtActionGridPosition.UnitActionHandler.MoveAction.IsMoving)
                            yield return null;

                        if (chosenCombatAction.IsInAttackRange(unitAtActionGridPosition, Unit.GridPosition, unitAtActionGridPosition.GridPosition) == false) // If the target Unit moved out of range
                        {
                            TargetEnemyUnit = unitAtActionGridPosition;
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
            Unit.StateController.SetCurrentState(GoalState.Fight);
            FindBestTargetEnemy();
            ClearActionQueue(false);

            if (TargetEnemyUnit == null || TargetEnemyUnit.Health.IsDead)
            {
                SetTargetEnemyUnit(null);
                Unit.StateController.SetToDefaultState();
                DetermineAction();
                return;
            }
        }

        void PursueTargetEnemy()
        {
            if (TargetEnemyUnit == null)
            {
                DetermineAction();
                return;
            }
            
            // If there's no space around the enemy unit, try to find another enemy to attack
            if (TargetEnemyUnit.IsCompletelySurrounded(Unit.GetAttackRange()))
                SwitchTargetEnemies(out Unit oldEnemy, out Unit newEnemy);

            MoveAction.QueueAction(LevelGrid.FindNearestValidGridPosition(TargetEnemyUnit.GridPosition, Unit, 10));
        }

        void FindBestTargetEnemy()
        {
            if (Unit.Health.IsDead)
                return;

            if (Unit.Vision.knownEnemies.Count > 0)
            {
                // If there's only one visible enemy, then there's no need to figure out the best enemy AI action
                if (Unit.Vision.knownEnemies.Count == 1)
                    SetTargetEnemyUnit(Unit.Vision.knownEnemies[0]);
                else
                {
                    npcAIActions.Clear();
                    if (Unit.UnitEquipment.RangedWeaponEquipped && Unit.UnitEquipment.HasValidAmmunitionEquipped())
                    {
                        ShootAction shootAction = GetAction<ShootAction>();
                        for (int i = 0; i < Unit.Vision.knownEnemies.Count; i++)
                        {
                            npcAIActions.Add(shootAction.GetNPCAIAction_Unit(Unit.Vision.knownEnemies[i]));
                        }

                        if (npcAIActions.Count == 1 && npcAIActions[0].actionValue <= -1)
                        {
                            TurnManager.Instance.FinishTurn(Unit);
                            return;
                        }

                        SetTargetEnemyUnit(LevelGrid.GetUnitAtGridPosition(shootAction.GetBestNPCAIActionFromList(npcAIActions).actionGridPosition));

                    }
                    else if (Unit.UnitEquipment.MeleeWeaponEquipped || Unit.Stats.CanFightUnarmed)
                    {
                        MeleeAction meleeAction = GetAction<MeleeAction>();
                        for (int i = 0; i < Unit.Vision.knownEnemies.Count; i++)
                        {
                            npcAIActions.Add(meleeAction.GetNPCAIAction_Unit(Unit.Vision.knownEnemies[i]));
                        }

                        if (npcAIActions.Count == 1 && npcAIActions[0].actionValue <= -1)
                        {
                            TurnManager.Instance.FinishTurn(Unit);
                            return;
                        }

                        SetTargetEnemyUnit(LevelGrid.GetUnitAtGridPosition(meleeAction.GetBestNPCAIActionFromList(npcAIActions).actionGridPosition));
                    }
                    else
                    {
                        StartFlee(Unit.Vision.GetClosestEnemy(true), defaultFleeDistance);
                        DetermineAction();
                    }
                }
            }
        }

        void GetRandomTargetEnemy()
        {
            if (Unit.Vision.knownEnemies.Count > 0)
                TargetEnemyUnit = Unit.Vision.knownEnemies[Random.Range(0, Unit.Vision.knownEnemies.Count)];
            else
                TargetEnemyUnit = null;
        }

        void SwitchTargetEnemies(out Unit oldEnemy, out Unit newEnemy)
        {
            oldEnemy = TargetEnemyUnit;
            Unit closestEnemy = Unit.Vision.GetClosestEnemy(false);

            // Debug.Log(unit + " new enemy: " + closestEnemy + " old enemy: " + oldEnemy);
            newEnemy = closestEnemy;
            SetTargetEnemyUnit(closestEnemy);
        }

        public override void SetTargetEnemyUnit(Unit target)
        {
            if (target != null && target != TargetEnemyUnit)
            {
                StartChaseGridPosition = Unit.GridPosition;
                shouldStopChasing = false;
            }

            base.SetTargetEnemyUnit(target);
        }

        public bool TryFindNearbyWeapon(out LooseItem foundLooseWeapon, out float distanceToWeapon)
        {
            LooseItem closestLooseWeapon = Unit.Vision.GetClosestWeapon(out float distanceToClosestWeapon);
            if (closestLooseWeapon == null)
            {
                foundLooseWeapon = null;
                distanceToWeapon = distanceToClosestWeapon;
                return false;
            }

            if (closestLooseWeapon.ItemData.Item is RangedWeapon)
            {
                if (Unit.UnitEquipment.HasValidAmmunitionEquipped(closestLooseWeapon.ItemData.Item.RangedWeapon)) 
                {
                    foundLooseWeapon = closestLooseWeapon;
                    distanceToWeapon = distanceToClosestWeapon;
                    return true;
                }
                else
                {
                    LooseItem closestLooseMeleeWeapon = Unit.Vision.GetClosestMeleeWeapon(out float distanceToClosestMeleeWeapon);
                    if (closestLooseMeleeWeapon != null)
                    {
                        foundLooseWeapon = closestLooseMeleeWeapon;
                        distanceToWeapon = distanceToClosestMeleeWeapon;
                        return true;
                    }
                }

            }

            foundLooseWeapon = closestLooseWeapon;
            distanceToWeapon = distanceToClosestWeapon;
            return true;
        }
        #endregion

        #region Flee
        void Flee()
        {
            // If there's no Unit to flee from or if the Unit to flee from died
            if (unitToFleeFrom == null || unitToFleeFrom.Health.IsDead)
            {
                Unit.StateController.SetToDefaultState(); // Variables are reset in this method
                DetermineAction();
                return;
            }

            float distanceFromUnitToFleeFrom = Vector3.Distance(unitToFleeFrom.WorldPosition, Unit.WorldPosition);

            // If the Unit has fled far enough
            if (distanceFromUnitToFleeFrom >= fleeDistance)
            {
                Unit.StateController.SetToDefaultState(); // Variables are also reset in this method
                DetermineAction();
                return;
            }

            // The enemy this Unit is fleeing from has moved closer or they have arrived at their flee destination, but are still too close to the enemy, so get a new flee destination
            if (Unit.GridPosition == MoveAction.TargetGridPosition || (unitToFleeFrom.GridPosition != unitToFleeFrom_PreviousGridPosition && (unitToFleeFrom_PreviousDistance == 0f || distanceFromUnitToFleeFrom + 2f <= unitToFleeFrom_PreviousDistance)))
                needsNewFleeDestination = true;

            GridPosition targetGridPosition = MoveAction.TargetGridPosition;
            if (needsNewFleeDestination)
            {
                needsNewFleeDestination = false;
                unitToFleeFrom_PreviousDistance = Vector3.Distance(unitToFleeFrom.WorldPosition, Unit.WorldPosition);
                targetGridPosition = GetFleeDestination();
            }

            // If there was no valid flee position, just grab a random position within range
            if (MoveAction.TargetGridPosition == Unit.GridPosition)
                targetGridPosition = LevelGrid.Instance.GetRandomGridPositionInRange(unitToFleeFrom.GridPosition, Unit, fleeDistance, fleeDistance + 15);

            MoveAction.QueueAction(targetGridPosition);
        }

        public void StartFlee(Unit unitToFleeFrom, int fleeDistance)
        {
            Unit.StateController.SetCurrentState(GoalState.Flee);
            this.unitToFleeFrom = unitToFleeFrom;
            this.fleeDistance = fleeDistance;
            ClearActionQueue(false);
        }

        GridPosition GetFleeDestination() => LevelGrid.Instance.GetRandomFleeGridPosition(Unit, unitToFleeFrom, fleeDistance, fleeDistance + 15);

        public int DefaultFleeDistance => defaultFleeDistance;

        public void SetUnitToFleeFrom(Unit unitToFleeFrom) => this.unitToFleeFrom = unitToFleeFrom;

        public bool ShouldAlwaysFleeCombat => shouldAlwaysFleeCombat;
        #endregion

        #region Follow
        void Follow()
        {
            if (leader == null || leader.Health.IsDead)
            {
                Debug.LogWarning("Leader for " + Unit.name + " is null or dead, but they are in the Follow state.");
                shouldFollowLeader = false;
                Unit.StateController.SetToDefaultState();
                DetermineAction();
                return;
            }

            if (Vector3.Distance(Unit.WorldPosition, leader.WorldPosition) <= stopFollowDistance)
                TurnManager.Instance.FinishTurn(Unit);
            else if (MoveAction.IsMoving == false)
                MoveAction.QueueAction(leader.UnitActionHandler.TurnAction.GetGridPositionBehindUnit());
        }

        public Unit Leader => leader;

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
                    Unit.StateController.SetToDefaultState();
                    DetermineAction();
                    return;
                }

                needsNewSoundInspectPosition = false;

                inspectSoundGridPosition = LevelGrid.Instance.GetRandomGridPositionInRange(SoundGridPosition, Unit, 0 + inspectSoundIterations, 2 + inspectSoundIterations, true);
                MoveAction.QueueAction(inspectSoundGridPosition);
            }
            else if (Vector3.Distance(inspectSoundGridPosition.WorldPosition, transform.position) <= 0.1f)
            {
                // Get a new Inspect Sound Position when the current one is reached
                inspectSoundIterations++;
                needsNewSoundInspectPosition = true;
                InspectSound();
            }
            else if (MoveAction.IsMoving == false)
            {
                // Get a new Inspect Sound Position if there's now another Unit or obstruction there
                if (LevelGrid.GridPositionObstructed(inspectSoundGridPosition))
                    inspectSoundGridPosition = LevelGrid.GetNearestSurroundingGridPosition(inspectSoundGridPosition, Unit.GridPosition, LevelGrid.diaganolDistance, true);

                MoveAction.QueueAction(inspectSoundGridPosition);
            }
        }

        public void SetSoundGridPosition(Vector3 soundPosition)
        {
            SoundGridPosition = LevelGrid.GetGridPosition(soundPosition);
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
                TurnManager.Instance.FinishTurn(Unit);
                return;
            }
            else if (patrolPoints.Length > 0)
            {
                if (initialPatrolPointSet == false)
                {
                    // Get the closest Patrol Point to the Unit as the first Patrol Point to move to
                    CurrentPatrolPointIndex = GetNearestPatrolPointIndex();
                    initialPatrolPointSet = true;
                }

                GridPosition patrolPointGridPosition = LevelGrid.GetGridPosition(patrolPoints[CurrentPatrolPointIndex]);

                // If the Patrol Point is set to an invalid Grid Position
                if (LevelGrid.IsValidGridPosition(patrolPointGridPosition) == false)
                {
                    // Debug.LogWarning(patrolPointGridPosition + " is not a valid grid position...");
                    IncreasePatrolPointIndex();
                    return;
                }
                // If there's another Unit currently on the Patrol Point or Alternative Patrol Point
                else if ((hasAlternativePatrolPoint == false && LevelGrid.GridPositionObstructed(patrolPointGridPosition) && LevelGrid.GetUnitAtGridPosition(patrolPointGridPosition) != Unit)
                    || (hasAlternativePatrolPoint && LevelGrid.GridPositionObstructed(MoveAction.TargetGridPosition) && LevelGrid.GetUnitAtGridPosition(MoveAction.TargetGridPosition) != Unit))
                {
                    // Increase the iteration count just in case we had to look for an Alternative Patrol Point due to something obstructing the current Target Grid Position
                    patrolIterationCount++;

                    // Find the nearest Grid Position to the Patrol Point
                    GridPosition nearestGridPositionToPatrolPoint = LevelGrid.FindNearestValidGridPosition(patrolPointGridPosition, Unit, 5);
                    if (patrolPointGridPosition == nearestGridPositionToPatrolPoint)
                        IncreasePatrolPointIndex();

                    hasAlternativePatrolPoint = true;
                    MoveAction.SetTargetGridPosition(nearestGridPositionToPatrolPoint);

                    if (nearestGridPositionToPatrolPoint != patrolPointGridPosition && LevelGrid.GridPositionObstructed(nearestGridPositionToPatrolPoint) == false)
                        patrolIterationCount = 0;
                }

                // If the Unit has arrived at their current Patrol Point or Alternative Patrol Point position
                if (Vector3.Distance(MoveAction.TargetGridPosition.WorldPosition, transform.position) <= 0.1f)
                {
                    if (hasAlternativePatrolPoint)
                        hasAlternativePatrolPoint = false;

                    // Set the Unit's Target Grid Position as the next Patrol Point
                    IncreasePatrolPointIndex();
                    patrolPointGridPosition = LevelGrid.GetGridPosition(patrolPoints[CurrentPatrolPointIndex]);
                    MoveAction.SetTargetGridPosition(patrolPointGridPosition);
                }
                // Otherwise, assign their target position to the Patrol Point if it's not already set
                else if (hasAlternativePatrolPoint == false && MoveAction.TargetGridPosition.WorldPosition != patrolPoints[CurrentPatrolPointIndex])
                {
                    MoveAction.SetTargetGridPosition(patrolPointGridPosition);

                    // Don't reset the patrol iteration count if the next target position is the Unit's current position, because we'll need to iterate through Patrol again
                    if (MoveAction.TargetGridPosition != Unit.GridPosition)
                        patrolIterationCount = 0;
                }

                // Queue the Move Action if the Unit isn't already moving
                if (MoveAction.IsMoving == false)
                    MoveAction.QueueAction(MoveAction.TargetGridPosition);
            }
            else // If no Patrol Points set
            {
                Debug.LogWarning("No patrol points set for " + name);
                patrolIterationCount = 0;

                if (Unit.StateController.DefaultState == GoalState.Patrol)
                    Unit.StateController.ChangeDefaultState(GoalState.Idle);

                Unit.StateController.SetCurrentState(GoalState.Idle);
                DetermineAction();
            }
        }

        public void IncreasePatrolPointIndex()
        {
            if (CurrentPatrolPointIndex == patrolPoints.Length - 1)
                CurrentPatrolPointIndex = 0;
            else
                CurrentPatrolPointIndex++;
        }

        public void AssignNextPatrolTargetPosition()
        {
            IncreasePatrolPointIndex();
            GridPosition patrolPointGridPosition = LevelGrid.GetGridPosition(patrolPoints[CurrentPatrolPointIndex]);
            MoveAction.SetTargetGridPosition(patrolPointGridPosition);
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
                if (wanderGridPosition == Unit.GridPosition)
                    TurnManager.Instance.FinishTurn(Unit);
                else
                {
                    wanderPositionSet = true;
                    MoveAction.SetTargetGridPosition(wanderGridPosition);
                }

                // Queue the Move Action if the Unit isn't already moving
                if (MoveAction.IsMoving == false)
                    MoveAction.QueueAction(wanderGridPosition);
            }
            // If the NPC has arrived at their destination
            else if (Vector3.Distance(wanderGridPosition.WorldPosition, transform.position) <= 0.1f)
            {
                // Get a new Wander Position when the current one is reached
                wanderPositionSet = false;
                Wander();
            }
            else if (MoveAction.IsMoving == false)
            {
                // Get a new Wander Position if there's now another Unit or obstruction there
                if (LevelGrid.GridPositionObstructed(wanderGridPosition))
                    wanderGridPosition = GetNewWanderPosition();

                MoveAction.QueueAction(wanderGridPosition);
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
        */
    }
}
