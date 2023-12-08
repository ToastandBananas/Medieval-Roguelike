using GridSystem;
using InventorySystem;
using Pathfinding.Util;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnitSystem.ActionSystem.Actions;
using UnitSystem.ActionSystem.GOAP.Goals;
using UnityEngine;
using Random = UnityEngine.Random;

namespace UnitSystem.ActionSystem.GOAP.GoalActions
{
    public class GoalAction_Fight : GoalAction_Base
    {
        [SerializeField] float maxChaseDistance = 25f;
        public float MaxChaseDistance => maxChaseDistance;
        public GridPosition StartChaseGridPosition { get; private set; }
        public bool ShouldStopChasing { get; private set; }

        readonly List<Type> supportedGoals = new(new Type[] { typeof(Goal_Fight) });
        List<NPCAIAction> npcAIActions = new();

        public GoalAction_Flee FleeAction { get; private set; }

        void Start()
        {
            FleeAction = npcActionHandler.GoalPlanner.GetGoalAction(typeof(GoalAction_Flee)) as GoalAction_Flee;
        }

        public override List<Type> SupportedGoals() => supportedGoals;

        public override float Cost() => 10;

        public override void OnTick()
        {
            Fight();
        }

        #region Fight
        void Fight()
        {
            if (unit.UnitEquipment.RangedWeaponEquipped)
            {
                Unit closestEnemy = unit.Vision.GetClosestEnemy(true);
                float minShootRange = unit.UnitMeshManager.GetHeldRangedWeapon().ItemData.Item.Weapon.MinRange;

                // If the closest enemy is too close and this Unit doesn't have a melee weapon, retreat back a few spaces or switch to a melee weapon
                if (closestEnemy != null && Vector3.Distance(unit.WorldPosition, closestEnemy.WorldPosition) < minShootRange + LevelGrid.diaganolDistance)
                {
                    // If the Unit has a melee weapon, switch to it
                    if (unit.UnitEquipment.OtherWeaponSet_IsMelee())
                    {
                        npcActionHandler.GetAction<SwapWeaponSetAction>().QueueAction();
                        return;
                    }
                    else if (unit.UnitInventoryManager.ContainsMeleeWeaponInAnyInventory(out ItemData weaponItemData))
                    {
                        npcActionHandler.GetAction<SwapWeaponSetAction>().QueueAction();
                        npcActionHandler.GetAction<EquipAction>().QueueAction(weaponItemData, weaponItemData.Item.Equipment.EquipSlot, null);
                        return;
                    }
                    else if (FleeAction != null)
                    {
                        // Else flee somewhere
                        FleeAction.StartFlee(closestEnemy, Mathf.RoundToInt(minShootRange + Random.Range(2, unit.UnitMeshManager.GetHeldRangedWeapon().ItemData.Item.Weapon.MaxRange - 2)));
                        npcActionHandler.DetermineAction();
                        return;
                    }
                }
            }

            if (ShouldStopChasing)
            {
                if (unit.GridPosition == LevelGrid.GetGridPosition(npcActionHandler.DefaultPosition))
                {
                    SetTargetEnemyUnit(null);
                    unit.StateController.SetToDefaultState();
                    npcActionHandler.DetermineAction();
                }
                else
                    npcActionHandler.MoveAction.QueueAction(LevelGrid.GetGridPosition(npcActionHandler.DefaultPosition));
                return;
            }
            else
            {
                if (Vector3.Distance(StartChaseGridPosition.WorldPosition, unit.WorldPosition) >= maxChaseDistance)
                {
                    ShouldStopChasing = true;
                    npcActionHandler.MoveAction.QueueAction(LevelGrid.GetGridPosition(npcActionHandler.DefaultPosition));
                    return;
                }
            }

            FindBestTargetEnemy();

            if (unit.StateController.CurrentState == GoalState.Flee)
                return;

            // If there's no target enemy Unit, try to find one, else switch States
            if (npcActionHandler.TargetEnemyUnit == null || npcActionHandler.TargetEnemyUnit.Health.IsDead)
            {
                SetTargetEnemyUnit(null);
                npcActionHandler.DetermineAction();
                return;
            }

            if (npcActionHandler.IsInAttackRange(npcActionHandler.TargetEnemyUnit, false))
                ChooseCombatAction();
            else
                PursueTargetEnemy();
        }

        public void SetStartChaseGridPosition(GridPosition newGridPosition)
        {
            StartChaseGridPosition = newGridPosition;
            ShouldStopChasing = false;
        }

        public void ChooseCombatAction() => StartCoroutine(ChooseCombatAction_Coroutine());

        public IEnumerator ChooseCombatAction_Coroutine()
        {
            BaseAttackAction chosenCombatAction = null;
            npcAIActions.Clear();

            // Loop through all combat actions
            for (int i = 0; i < npcActionHandler.AvailableCombatActions.Count; i++)
            {
                if (npcActionHandler.AvailableCombatActions[i].IsValidAction() == false)
                    continue;

                if (unit.Stats.HasEnoughEnergy(npcActionHandler.AvailableCombatActions[i].InitialEnergyCost()) == false)
                    continue;

                // Loop through every grid position in range of the combat action
                foreach (GridPosition gridPositionInRange in npcActionHandler.AvailableCombatActions[i].GetActionGridPositionsInRange(unit.GridPosition))
                {
                    // For each of these grid positions, get the best one for this combat action
                    npcAIActions.Add(npcActionHandler.AvailableCombatActions[i].GetNPCAIAction_ActionGridPosition(gridPositionInRange));
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

                        if (chosenCombatAction.IsInAttackRange(unitAtActionGridPosition, unit.GridPosition, unitAtActionGridPosition.GridPosition) == false) // If the target Unit moved out of range
                        {
                            npcActionHandler.SetTargetEnemyUnit(unitAtActionGridPosition);
                            PursueTargetEnemy();

                            ListPool<NPCAIAction>.Release(filteredNPCAIActions);
                            ListPool<int>.Release(accumulatedWeights);
                            yield break;
                        }
                    }

                    // Set the unit's target attack position to the one corresponding to the NPCAIAction that was chosen
                    npcActionHandler.SetQueuedAttack(chosenCombatAction, filteredNPCAIActions[selectedIndex].actionGridPosition);
                    chosenCombatAction.QueueAction(filteredNPCAIActions[selectedIndex].actionGridPosition);
                }
                else // If no combat action was found, just move towards the target enemy
                    PursueTargetEnemy();

                ListPool<NPCAIAction>.Release(filteredNPCAIActions);
                ListPool<int>.Release(accumulatedWeights);
            }
        }

        void PursueTargetEnemy()
        {
            if (npcActionHandler.TargetEnemyUnit == null)
            {
                npcActionHandler.DetermineAction();
                return;
            }

            // If there's no space around the enemy unit, try to find another enemy to attack
            if (npcActionHandler.TargetEnemyUnit.IsCompletelySurrounded(unit.GetAttackRange()))
                SwitchTargetEnemies(out Unit oldEnemy, out Unit newEnemy);

            npcActionHandler.MoveAction.QueueAction(LevelGrid.FindNearestValidGridPosition(npcActionHandler.TargetEnemyUnit.GridPosition, unit, 10));
        }

        void FindBestTargetEnemy()
        {
            if (unit.Health.IsDead)
                return;

            if (unit.Vision.knownEnemies.Count > 0)
            {
                // If there's only one visible enemy, then there's no need to figure out the best enemy AI action
                if (unit.Vision.knownEnemies.Count == 1)
                    SetTargetEnemyUnit(unit.Vision.knownEnemies[0]);
                else
                {
                    npcAIActions.Clear();
                    if (unit.UnitEquipment.RangedWeaponEquipped && unit.UnitEquipment.HasValidAmmunitionEquipped())
                    {
                        ShootAction shootAction = npcActionHandler.GetAction<ShootAction>();
                        for (int i = 0; i < unit.Vision.knownEnemies.Count; i++)
                        {
                            npcAIActions.Add(shootAction.GetNPCAIAction_Unit(unit.Vision.knownEnemies[i]));
                        }

                        if (npcAIActions.Count == 1 && npcAIActions[0].actionValue <= -1)
                        {
                            TurnManager.Instance.FinishTurn(unit);
                            return;
                        }

                        SetTargetEnemyUnit(LevelGrid.GetUnitAtGridPosition(shootAction.GetBestNPCAIActionFromList(npcAIActions).actionGridPosition));

                    }
                    else if (unit.UnitEquipment.MeleeWeaponEquipped || unit.Stats.CanFightUnarmed)
                    {
                        MeleeAction meleeAction = npcActionHandler.GetAction<MeleeAction>();
                        for (int i = 0; i < unit.Vision.knownEnemies.Count; i++)
                        {
                            npcAIActions.Add(meleeAction.GetNPCAIAction_Unit(unit.Vision.knownEnemies[i]));
                        }

                        if (npcAIActions.Count == 1 && npcAIActions[0].actionValue <= -1)
                        {
                            TurnManager.Instance.FinishTurn(unit);
                            return;
                        }

                        SetTargetEnemyUnit(LevelGrid.GetUnitAtGridPosition(meleeAction.GetBestNPCAIActionFromList(npcAIActions).actionGridPosition));
                    }
                    else
                    {
                        npcActionHandler.GoalPlanner.FleeAction.StartFlee(unit.Vision.GetClosestEnemy(true), npcActionHandler.GoalPlanner.FleeAction.DefaultFleeDistance);
                        npcActionHandler.DetermineAction();
                    }
                }
            }
        }

        void GetRandomTargetEnemy()
        {
            if (unit.Vision.knownEnemies.Count > 0)
                npcActionHandler.SetTargetEnemyUnit(unit.Vision.knownEnemies[Random.Range(0, unit.Vision.knownEnemies.Count)]);
            else
                npcActionHandler.SetTargetEnemyUnit(null);
        }

        void SwitchTargetEnemies(out Unit oldEnemy, out Unit newEnemy)
        {
            oldEnemy = npcActionHandler.TargetEnemyUnit;
            Unit closestEnemy = unit.Vision.GetClosestEnemy(false);

            // Debug.Log(unit + " new enemy: " + closestEnemy + " old enemy: " + oldEnemy);
            newEnemy = closestEnemy;
            SetTargetEnemyUnit(closestEnemy);
        }

        public void SetTargetEnemyUnit(Unit target)
        {
            if (target != null && target != npcActionHandler.TargetEnemyUnit)
            {
                StartChaseGridPosition = unit.GridPosition;
                ShouldStopChasing = false;
            }

            npcActionHandler.SetTargetEnemyUnit(target);
        }
        #endregion
    }
}
