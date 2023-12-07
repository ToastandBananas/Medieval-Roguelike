/// <summary>
/// Derived from Sebastian Lague's Tutorial "Field of View Visualisation" (https://www.youtube.com/watch?v=rQG9aUWarwE)
/// </summary>

using System.Collections.Concurrent;
using System.Collections.Generic;
using UnityEngine;
using InteractableObjects;
using UnitSystem.ActionSystem;
using GridSystem;
using InventorySystem;
using Utilities;

namespace UnitSystem
{
    public class Vision : MonoBehaviour
    {
        [SerializeField] Unit unit;

        [Header("Field of View")]
        [SerializeField] Transform parentTransform;
        [SerializeField] float viewRadius = 20f;
        [Range(0, 360)][SerializeField] float viewAngle = 160f;
        [Range(0, 360)][SerializeField] float opportunityAttackViewAngle = 220f;
        Vector3 yOffset; // Height offset for where vision starts (the eyes)

        [Header("Layer Masks")]
        [SerializeField] LayerMask unitsMask;
        [SerializeField] LayerMask obstacleMask;
        [SerializeField] LayerMask looseItemVisionObstacleMask;
        [SerializeField] LayerMask interactableMask;

        public ConcurrentDictionary<LooseItem, int> knownLooseItems { get; private set; }
        public ConcurrentDictionary<Unit, int> knownUnits { get; private set; }
        public List<Unit> knownDeadUnits { get; private set; }
        public List<Unit> knownEnemies { get; private set; }
        public List<Unit> knownAllies { get; private set; }

        List<LooseItem> looseItemsToRemove = new List<LooseItem>();
        List<Unit> unitsToRemove = new List<Unit>();

        Collider[] looseItemsInViewRadius;
        Collider[] unitsInViewRadius;

        readonly int loseSightTime = 60; // The amount of turns it takes to lose sight of a Unit, when out of their direct vision

        // The distance at which the player loses sight of something outside of their view radius, even if it is contained in one of the "known" lists
        readonly float playerPerceptionDistance = 5f;

        void Awake()
        {
            knownLooseItems = new ConcurrentDictionary<LooseItem, int>();
            knownUnits = new ConcurrentDictionary<Unit, int>();
            knownDeadUnits = new List<Unit>();
            knownEnemies = new List<Unit>();
            knownAllies = new List<Unit>();
            unit = parentTransform.GetComponent<Unit>();

            yOffset.Set(0, transform.localPosition.y, 0);
        }

        public bool IsDirectlyVisible(Unit unitToCheck)
        {
            if (knownUnits.ContainsKey(unitToCheck) == false)
                return false;

            if (unitToCheck.UnitMeshManager.meshesHidden)
                return false;

            if (IsInLineOfSight_Raycast(unitToCheck) == false)
                return false;

            return true;
        }

        public bool IsVisible(Unit unitToCheck)
        {
            if (knownUnits.ContainsKey(unitToCheck) == false)
                return false;

            if (unitToCheck.UnitMeshManager.meshesHidden)
                return false;

            return true;
        }

        public bool IsDirectlyVisible(GameObject unitGameObject)
        {
            Unit targetUnit = LevelGrid.GetUnitAtGridPosition(LevelGrid.GetGridPosition(unitGameObject.transform.position));
            if (targetUnit == null)
                return false;

            return IsDirectlyVisible(targetUnit);
        }

        public bool IsVisible(GameObject unitGameObject)
        {
            Unit targetUnit = LevelGrid.GetUnitAtGridPosition(LevelGrid.GetGridPosition(unitGameObject.transform.position));
            if (targetUnit == null)
                return false;

            return IsVisible(targetUnit);
        }

        public bool IsVisible(LooseItem looseItemToCheck)
        {
            if (knownLooseItems.ContainsKey(looseItemToCheck) == false)
                return false;

            if (looseItemToCheck.CanSeeMeshRenderer == false)
                return false;

            if (IsInLineOfSight_Raycast(looseItemToCheck) == false)
                return false;

            return true;
        }

        public bool IsKnown(Unit unitToCheck)
        {
            if (knownUnits.ContainsKey(unitToCheck))
                return true;
            return false;
        }

        public bool IsKnown(LooseItem looseItemToCheck)
        {
            if (knownLooseItems.ContainsKey(looseItemToCheck))
                return true;
            return false;
        }

        public bool IsInLineOfSight_SphereCast(Unit unitToCheck)
        {
            float sphereCastRadius = 0.1f;
            Vector3 offset = 2f * unitToCheck.ShoulderHeight * Vector3.up;
            Vector3 shootDir = (unitToCheck.WorldPosition + offset - transform.position).normalized;
            float distToTarget = Vector3.Distance(transform.position, unitToCheck.WorldPosition + offset);
            if (Physics.SphereCast(transform.position, sphereCastRadius, shootDir, out _, distToTarget, unit.UnitActionHandler.AttackObstacleMask))
                return false; // Blocked by an obstacle
            return true;
        }

        public bool IsInLineOfSight_SphereCast(GridPosition targetGridPosition)
        {
            float sphereCastRadius = 0.1f;
            Vector3 offset = 2f * unit.ShoulderHeight * Vector3.up;
            Vector3 shootDir = (targetGridPosition.WorldPosition + offset - transform.position).normalized;
            float distToTarget = Vector3.Distance(transform.position, targetGridPosition.WorldPosition + offset);
            if (Physics.SphereCast(transform.position, sphereCastRadius, shootDir, out _, distToTarget, unit.UnitActionHandler.AttackObstacleMask))
                return false; // Blocked by an obstacle
            return true;
        }

        public bool IsInLineOfSight_Raycast(Unit unitToCheck)
        {
            Vector3 dirToTarget = (unitToCheck.WorldPosition + yOffset - transform.position).normalized;
            float distToTarget = Vector3.Distance(transform.position, unitToCheck.WorldPosition + yOffset);

            // If target Unit is in the view angle and no obstacles are in the way
            if (Physics.Raycast(transform.position, dirToTarget, distToTarget, obstacleMask) == false)
                return true;
            return false;
        }

        bool IsInLineOfSight_Raycast(LooseItem looseItem)
        {
            Vector3 looseItemCenter = looseItem.transform.TransformPoint(looseItem.MeshCollider.sharedMesh.bounds.center);
            Vector3 dirToTarget = (looseItemCenter - transform.position).normalized;
            float distToTarget = Vector3.Distance(transform.position, looseItemCenter);

            // If target Unit is in the view angle and no obstacles are in the way
            if (Physics.Raycast(transform.position, dirToTarget, distToTarget, obstacleMask) == false)
                return true;
            return false;
        }

        public void FindVisibleUnitsAndObjects()
        {
            FindVisibleUnits();
            FindVisibleLooseItems();

            ActionLineRenderer.ResetCurrentPositions();
        }

        public void UpdateVision()
        {
            UpdateVisibleUnits();
            UpdateVisibleLooseItems();

            ActionLineRenderer.ResetCurrentPositions();
        }

        public bool TargetInViewAngle(Vector3 directionToTarget)
        {
            if (Vector3.Angle(unit.transform.forward, directionToTarget) < viewAngle / 2)
                return true;
            return false;
        }

        public bool TargetInOpportunityAttackViewAngle(Transform targetTransform)
        {
            Vector3 directionToTarget = (targetTransform.position + yOffset - transform.position).normalized;
            if (Vector3.Angle(unit.transform.forward, directionToTarget) < opportunityAttackViewAngle / 2)
                return true;
            return false;
        }

        #region Units
        void FindVisibleUnits()
        {
            unitsInViewRadius = Physics.OverlapSphere(transform.position, viewRadius, unitsMask);
            for (int i = 0; i < unitsInViewRadius.Length; i++)
            {
                Transform targetTransform = unitsInViewRadius[i].transform;
                targetTransform.TryGetComponent(out Unit targetUnit);
                if (targetUnit != null && targetUnit != unit)
                {
                    // If the Unit in the view radius is already "known", skip them
                    if (knownUnits.ContainsKey(targetUnit)) continue;

                    Vector3 dirToTarget = (targetTransform.position + yOffset - transform.position).normalized;

                    // If target Unit is in the view angle
                    if (TargetInViewAngle(dirToTarget))
                    {
                        float distToTarget = Vector3.Distance(transform.position, targetTransform.position);

                        // If no obstacles are in the way, add the Unit to the knownUnits dictionary
                        if (Physics.Raycast(transform.position, dirToTarget, distToTarget, obstacleMask) == false)
                            AddVisibleUnit(targetUnit);
                        else if (unit.IsPlayer && distToTarget > playerPerceptionDistance) // Else, hide the NPC's mesh renderers
                            targetUnit.UnitMeshManager.HideMeshRenderers();
                    }
                }
            }

            if (unit.IsPlayer)
            {
                // Check if a "visible NPC Unit" is outside of the Player's view radius. If so, hide their mesh renderers
                foreach (KeyValuePair<Unit, int> knownUnit in knownUnits)
                {
                    bool shouldHide = true;
                    for (int j = 0; j < unitsInViewRadius.Length; j++)
                    {
                        if (knownUnit.Key.transform == unitsInViewRadius[j].transform && unitsInViewRadius[j].transform != unit.transform)
                        {
                            // Skip if they're in the view radius and there's no obstacles in the way
                            Vector3 dirToTarget = (unitsInViewRadius[j].transform.position + yOffset - transform.position).normalized;
                            float distToTarget = Vector3.Distance(transform.position, unitsInViewRadius[j].transform.position);

                            // If target Unit is in the view angle and no obstacles are in the way
                            if (TargetInViewAngle(dirToTarget) && Physics.Raycast(transform.position, dirToTarget, distToTarget, obstacleMask) == false)
                                shouldHide = false;

                            break;
                        }
                    }

                    if (shouldHide && knownUnit.Key != unit)
                    {
                        if (knownUnit.Key.UnitActionHandler.TargetEnemyUnit != unit && Vector3.Distance(unit.transform.position, knownUnit.Key.transform.position) > playerPerceptionDistance)
                        {
                            knownUnit.Key.UnitMeshManager.HideMeshRenderers();
                            if (unit.UnitActionHandler.TargetEnemyUnit == knownUnit.Key)
                                unit.UnitActionHandler.SetTargetEnemyUnit(null);
                        }
                    }
                    else
                        knownUnit.Key.UnitMeshManager.ShowMeshRenderers();
                }
            }

            // Flee or Fight if not already doing so, if there are now enemies visible to this NPC
            /*if (knownEnemies.Count > 0 && unit.IsNPC && unit.StateController.CurrentState != GoalState.Flee && unit.StateController.CurrentState != GoalState.Fight)
            {
                NPCActionHandler npcActionHandler = unit.UnitActionHandler as NPCActionHandler;
                if (npcActionHandler.ShouldAlwaysFleeCombat)
                    npcActionHandler.StartFlee(GetClosestEnemy(true), npcActionHandler.DefaultFleeDistance);
                else
                    npcActionHandler.StartFight();
            }*/
        }

        void UpdateVisibleUnits()
        {
            unitsToRemove.Clear();

            // Check if Units that were in sight are now out of sight
            foreach (KeyValuePair<Unit, int> knownUnit in knownUnits)
            {
                // If the visible Unit is now dead, update the appropriate lists
                if (knownUnit.Key.Health.IsDead)
                    UpdateDeadUnit(knownUnit.Key);

                Transform targetTransform = knownUnit.Key.transform;
                Vector3 dirToTarget = (targetTransform.position + yOffset - transform.position).normalized;

                // If target Unit is in the view angle
                if (TargetInViewAngle(dirToTarget))
                {
                    float distToTarget = Vector3.Distance(transform.position, targetTransform.position);

                    // If an obstacle is in the way, lower the lose sight time
                    if (Physics.Raycast(transform.position, dirToTarget, distToTarget, obstacleMask))
                    {
                        if (knownUnit.Value > 1)
                            knownUnits[knownUnit.Key]--; // Subtract from the visible Unit's corresponding lose sight time
                        else
                            unitsToRemove.Add(knownUnit.Key); // The Unit is no longer visible

                        if (unit.IsPlayer && knownUnit.Key.UnitActionHandler.TargetEnemyUnit != unit) // Hide the NPC's mesh renderers
                            knownUnit.Key.UnitMeshManager.HideMeshRenderers();
                    }
                    else // We can still see the Unit, so reset their lose sight time
                    {
                        knownUnits[knownUnit.Key] = loseSightTime;

                        if (unit.IsPlayer) // Show the NPC's mesh renderers
                            knownUnit.Key.UnitMeshManager.ShowMeshRenderers();
                    }
                }
                else // The target is outside of the view angle
                {
                    if (knownUnit.Value > 1)
                        knownUnits[knownUnit.Key]--; // Subtract from the visible Unit's corresponding lose sight time
                    else
                        unitsToRemove.Add(knownUnit.Key); // The Unit is no longer visible

                    if (unit.IsPlayer && knownUnit.Key.UnitActionHandler.TargetEnemyUnit != unit && Vector3.Distance(unit.transform.position, targetTransform.position) > playerPerceptionDistance) // Hide the NPC's mesh renderers
                        knownUnit.Key.UnitMeshManager.HideMeshRenderers();
                }
            }

            // Remove Units from their corresponding lists if they were found to no longer be visible
            for (int i = 0; i < unitsToRemove.Count; i++)
            {
                RemoveVisibleUnit(unitsToRemove[i]);
            }
        }

        public void AddVisibleUnit(Unit unitToAdd)
        {
            // We don't want Units to see themselves
            if (unitToAdd == unit)
                return;

            if (knownUnits.ContainsKey(unitToAdd) == false)
            {
                // Add the unit to the dictionary
                knownUnits.TryAdd(unitToAdd, loseSightTime);

                if (unitToAdd.Health.IsDead)
                    knownDeadUnits.Add(unitToAdd);
                else if (unit.Alliance.IsEnemy(unitToAdd))
                {
                    knownEnemies.Add(unitToAdd);

                    // The Player should cancel any action (other than attacks) when becoming aware of a new enemy
                    if (unit.IsPlayer && unit.UnitActionHandler.QueuedActions.Count > 0 && unit.UnitActionHandler.AttackQueuedNext() == false)
                        unit.UnitActionHandler.CancelActions();
                }
                else if (unit.Alliance.IsAlly(unitToAdd))
                    knownAllies.Add(unitToAdd);
            }
            else // Restart the lose sight countdown
                knownUnits[unitToAdd] = loseSightTime;

            // If this is the Player's Vision, show the newly visible NPC Unit
            if (unit.IsPlayer)
                unitToAdd.UnitMeshManager.ShowMeshRenderers();
        }

        public void RemoveVisibleUnit(Unit unitToRemove)
        {
            if (knownUnits.ContainsKey(unitToRemove))
            {
                knownUnits.Remove(unitToRemove, out int value);

                if (knownDeadUnits.Contains(unitToRemove))
                    knownDeadUnits.Remove(unitToRemove);

                if (knownEnemies.Contains(unitToRemove))
                    knownEnemies.Remove(unitToRemove);

                if (knownAllies.Contains(unitToRemove))
                    knownAllies.Remove(unitToRemove);

                if (unit.UnitActionHandler.TargetEnemyUnit == unitToRemove)
                    unit.UnitActionHandler.SetTargetEnemyUnit(null);

                // If they are no longer visible to the player, hide them
                if (unit.IsPlayer)
                    unitToRemove.UnitMeshManager.HideMeshRenderers();
            }
        }

        public void BecomeVisibleUnitOfTarget(Unit targetUnit, bool becomeEnemy)
        {
            if (targetUnit == null)
                return;

            if (becomeEnemy)
                BecomeVisibleEnemyOfTarget(targetUnit);
            else
            {
                // Otherwise, just become visible
                if (targetUnit.Alliance.IsEnemy(unit))
                    BecomeVisibleEnemyOfTarget(targetUnit);
                else if (targetUnit.Alliance.IsAlly(unit))
                    BecomeVisibleAllyOfTarget(targetUnit);
                else
                    targetUnit.Vision.AddVisibleUnit(unit);
            }
        }

        void BecomeVisibleEnemyOfTarget(Unit targetUnit)
        {
            // The target Unit becomes an enemy of this Unit's faction if they weren't already
            if (unit.Alliance.IsEnemy(targetUnit) == false)
            {
                targetUnit.Vision.RemoveVisibleUnit(unit);
                RemoveVisibleUnit(targetUnit);

                targetUnit.Alliance.AddEnemy(unit);
                AddVisibleUnit(targetUnit);
            }

            targetUnit.Vision.AddVisibleUnit(unit); // The target Unit becomes aware of this Unit if they weren't already
        }

        void BecomeVisibleAllyOfTarget(Unit targetUnit)
        {
            // The target Unit becomes an enemy of this Unit's faction if they weren't already
            if (unit.Alliance.IsAlly(targetUnit) == false)
            {
                targetUnit.Vision.RemoveVisibleUnit(unit);
                RemoveVisibleUnit(targetUnit);

                targetUnit.Alliance.AddAlly(unit);
                AddVisibleUnit(targetUnit);
            }

            targetUnit.Vision.AddVisibleUnit(unit); // The target Unit becomes aware of this Unit if they weren't already
        }

        void UpdateDeadUnit(Unit deadUnit)
        {
            if (knownDeadUnits.Contains(deadUnit) == false)
                knownDeadUnits.Add(deadUnit);

            if (knownEnemies.Contains(deadUnit))
                knownEnemies.Remove(deadUnit);

            if (knownAllies.Contains(deadUnit))
                knownAllies.Remove(deadUnit);
        }

        public Unit GetClosestEnemy(bool includeTargetEnemy, float maxDistance = Mathf.Infinity)
        {
            Unit closestEnemy = null;
            float closestEnemyDistance = 1000000;
            if (includeTargetEnemy && unit.UnitActionHandler.TargetEnemyUnit != null && !unit.UnitActionHandler.TargetEnemyUnit.Health.IsDead)
            {
                float distToTargetEnemy = Vector3.Distance(unit.WorldPosition, unit.UnitActionHandler.TargetEnemyUnit.WorldPosition);
                if (distToTargetEnemy <= maxDistance)
                {
                    closestEnemy = unit.UnitActionHandler.TargetEnemyUnit;
                    closestEnemyDistance = distToTargetEnemy;
                }
            }

            for (int i = 0; i < knownEnemies.Count; i++)
            {
                if (unit.UnitActionHandler.TargetEnemyUnit != null && (unit.UnitActionHandler.TargetEnemyUnit.Health.IsDead || (!includeTargetEnemy && knownEnemies[i] == unit.UnitActionHandler.TargetEnemyUnit)))
                    continue;

                float distToEnemy = Vector3.Distance(unit.WorldPosition, knownEnemies[i].WorldPosition);
                if (distToEnemy < closestEnemyDistance && distToEnemy <= maxDistance)
                {
                    closestEnemy = knownEnemies[i];
                    closestEnemyDistance = distToEnemy;
                }
            }

            return closestEnemy;
        }
        #endregion

        #region Loose Items
        void FindVisibleLooseItems()
        {
            looseItemsInViewRadius = Physics.OverlapSphere(transform.position, viewRadius, interactableMask);
            for (int i = 0; i < looseItemsInViewRadius.Length; i++)
            {
                if (looseItemsInViewRadius[i].CompareTag("Loose Item") == false)
                    continue;

                // If the LooseItem in the view radius is already "known", skip it
                if (looseItemsInViewRadius[i].transform.TryGetComponent(out LooseItem looseItem) == false || knownLooseItems.ContainsKey(looseItem))
                    continue;

                Vector3 looseItemCenter = looseItem.transform.TransformPoint(looseItem.MeshCollider.sharedMesh.bounds.center);
                Vector3 dirToTarget = (looseItemCenter - transform.position).normalized;

                // If target LooseItem is in the view angle
                float distToTarget = Vector3.Distance(transform.position, looseItemCenter);
                if (TargetInViewAngle(dirToTarget))
                {
                    // If no obstacles are in the way, add the LooseItem to the knownLooseItems dictionary
                    if (Physics.Raycast(transform.position, dirToTarget, distToTarget, looseItemVisionObstacleMask) == false || distToTarget <= playerPerceptionDistance)
                        AddVisibleLooseItem(looseItem);
                    else if (unit.IsPlayer && !knownLooseItems.ContainsKey(looseItem) && distToTarget > playerPerceptionDistance) // Else, hide the LooseItem's mesh renderers
                        looseItem.HideMeshRenderer();
                }
                else if (unit.IsPlayer && !knownLooseItems.ContainsKey(looseItem) && distToTarget > playerPerceptionDistance)
                    looseItem.HideMeshRenderer();
            }

            /*if (unit.IsPlayer)
            {
                // Check if a "visible LooseItem" is outside of the Player's view radius. If so, hide their mesh renderers
                foreach (KeyValuePair<LooseItem, int> looseItem in knownLooseItems)
                {
                    bool shouldHide = true;
                    Vector3 looseItemCenter = looseItem.Key.transform.TransformPoint(looseItem.Key.MeshCollider.sharedMesh.bounds.center);
                    for (int j = 0; j < looseItemsInViewRadius.Length; j++)
                    {
                        if (looseItem.Key.transform == looseItemsInViewRadius[j].transform)
                        {
                            // Skip if it's in the view radius and there's no obstacles in the way
                            Vector3 dirToTarget = (looseItemCenter - transform.position).normalized;
                            float distToTarget = Vector3.Distance(transform.position, looseItemCenter);

                            // If target LooseItem is in the view angle and no obstacles are in the way
                            if (TargetInViewAngle(dirToTarget) && Physics.Raycast(transform.position, dirToTarget, distToTarget, looseItemVisionObstacleMask) == false)
                                shouldHide = false;
                            break;
                        }
                    }

                    // Only hide it if it's outside of the player's perception distance
                    if (shouldHide && Vector3.Distance(transform.position, looseItemCenter) > playerPerceptionDistance)
                        looseItem.Key.HideMeshRenderer();
                    else
                        looseItem.Key.ShowMeshRenderer();
                }
            }*/
        }

        void UpdateVisibleLooseItems()
        {
            looseItemsToRemove.Clear();

            // Check if LooseItems that were in sight are now out of sight
            foreach (KeyValuePair<LooseItem, int> looseItem in knownLooseItems)
            {
                if (looseItem.Key == null || looseItem.Key.gameObject.activeSelf == false)
                {
                    looseItemsToRemove.Add(looseItem.Key);
                    continue;
                }

                Vector3 looseItemCenter = looseItem.Key.transform.TransformPoint(looseItem.Key.MeshCollider.sharedMesh.bounds.center);
                Vector3 dirToTarget = (looseItemCenter - transform.position).normalized;

                // If target LooseItem is in the view angle
                if (TargetInViewAngle(dirToTarget))
                {
                    float distToTarget = Vector3.Distance(transform.position, looseItemCenter);

                    // If an obstacle is in the way, lower the lose sight time
                    if (Physics.Raycast(transform.position, dirToTarget, distToTarget, looseItemVisionObstacleMask))
                    {
                        if (looseItem.Value > 1)
                            knownLooseItems[looseItem.Key]--; // Subtract from the visible LooseItem's corresponding lose sight time
                        else
                            looseItemsToRemove.Add(looseItem.Key); // The LooseItem is no longer visible

                        //if (unit.IsPlayer) // Hide the LooseItem's mesh renderers
                            //looseItem.Key.HideMeshRenderer();
                    }
                    else // We can still see the LooseItem, so reset their lose sight time
                    {
                        knownLooseItems[looseItem.Key] = loseSightTime;

                        //if (unit.IsPlayer) // Show the LooseItem's mesh renderers
                            //looseItem.Key.ShowMeshRenderer();
                    }
                }
                else // The target is outside of the view angle
                {
                    if (looseItem.Value > 1)
                        knownLooseItems[looseItem.Key]--; // Subtract from the visible LooseItem's corresponding lose sight time
                    else
                        looseItemsToRemove.Add(looseItem.Key); // The LooseItem is no longer visible

                    //if (unit.IsPlayer && Vector3.Distance(unit.transform.position, looseItemCenter) > playerPerceptionDistance) // Hide the LooseItem's mesh renderers
                        //looseItem.Key.HideMeshRenderer();
                }
            }

            // Remove LooseItems from their corresponding list if they were found to no longer be visible
            for (int i = 0; i < looseItemsToRemove.Count; i++)
            {
                RemoveVisibleLooseItem(looseItemsToRemove[i]);
            }
        }

        public void AddVisibleLooseItem(LooseItem looseItemToAdd)
        {
            if (knownLooseItems.ContainsKey(looseItemToAdd) == false)
            {
                // Add the LooseItem to the dictionary
                knownLooseItems.TryAdd(looseItemToAdd, loseSightTime);
            }
            else // Restart the lose sight countdown
                knownLooseItems[looseItemToAdd] = loseSightTime;

            // If this is the Player's Vision, show the newly visible LooseItem
            if (unit.IsPlayer && looseItemToAdd.CanSeeMeshRenderer == false)
                looseItemToAdd.ShowMeshRenderer();
        }

        public void RemoveVisibleLooseItem(LooseItem looseItemToRemove)
        {
            if (knownLooseItems.ContainsKey(looseItemToRemove))
            {
                knownLooseItems.Remove(looseItemToRemove, out _);

                // If they are no longer visible to the player, hide them
                if (looseItemToRemove.gameObject.activeSelf && unit.IsPlayer)
                    looseItemToRemove.HideMeshRenderer();
            }
        }

        public LooseItem GetClosestLooseItem(out float distanceToLooseItem)
        {
            LooseItem closestLooseItem = null;
            float closestLooseItemDistance = 1000000;
            foreach (KeyValuePair<LooseItem, int> knownLooseItem in knownLooseItems)
            {
                if (knownLooseItem.Key.ItemData == null || knownLooseItem.Key.ItemData.Item == null)
                    continue;

                float distToLooseItem = Vector3.Distance(unit.WorldPosition, knownLooseItem.Key.GridPosition().WorldPosition);
                if (distToLooseItem < closestLooseItemDistance)
                {
                    closestLooseItem = knownLooseItem.Key;
                    closestLooseItemDistance = distToLooseItem;
                }
            }
            distanceToLooseItem = closestLooseItemDistance;
            return closestLooseItem;
        }

        public LooseItem GetClosestWeapon(out float distanceToWeapon)
        {
            LooseItem closestLooseMeleeWeapon = null;
            float closestWeaponDistance = 1000000;
            foreach (KeyValuePair<LooseItem, int> knownLooseItem in knownLooseItems)
            {
                if (knownLooseItem.Key.ItemData == null || knownLooseItem.Key.ItemData.Item == null || knownLooseItem.Key.ItemData.Item is Weapon == false)
                    continue;

                float distToLooseItem = Vector3.Distance(unit.WorldPosition, knownLooseItem.Key.GridPosition().WorldPosition);
                if (distToLooseItem < closestWeaponDistance)
                {
                    closestLooseMeleeWeapon = knownLooseItem.Key;
                    closestWeaponDistance = distToLooseItem;
                }
            }

            distanceToWeapon = closestWeaponDistance;
            return closestLooseMeleeWeapon;
        }

        public LooseItem GetClosestMeleeWeapon(out float distanceToWeapon)
        {
            LooseItem closestLooseMeleeWeapon = null;
            float closestMeleeWeaponDistance = 1000000;
            foreach (KeyValuePair<LooseItem, int> knownLooseItem in knownLooseItems)
            {
                if (knownLooseItem.Key.ItemData == null || knownLooseItem.Key.ItemData.Item == null || knownLooseItem.Key.ItemData.Item is MeleeWeapon == false)
                    continue;

                float distToLooseItem = Vector3.Distance(unit.WorldPosition, knownLooseItem.Key.GridPosition().WorldPosition);
                if (distToLooseItem < closestMeleeWeaponDistance)
                {
                    closestLooseMeleeWeapon = knownLooseItem.Key;
                    closestMeleeWeaponDistance = distToLooseItem;
                }
            }

            distanceToWeapon = closestMeleeWeaponDistance;
            return closestLooseMeleeWeapon;
        }

        public LooseItem GetClosestRangedWeapon(out float distanceToWeapon)
        {
            LooseItem closestLooseRangedWeapon = null;
            float closestRangedWeaponDistance = 1000000;
            foreach (KeyValuePair<LooseItem, int> knownLooseItem in knownLooseItems)
            {
                if (knownLooseItem.Key.ItemData == null || knownLooseItem.Key.ItemData.Item == null || knownLooseItem.Key.ItemData.Item is RangedWeapon == false)
                    continue;

                float distToLooseItem = Vector3.Distance(unit.WorldPosition, knownLooseItem.Key.GridPosition().WorldPosition);
                if (distToLooseItem < closestRangedWeaponDistance)
                {
                    closestLooseRangedWeapon = knownLooseItem.Key;
                    closestRangedWeaponDistance = distToLooseItem;
                }
            }

            distanceToWeapon = closestRangedWeaponDistance;
            return closestLooseRangedWeapon;
        }
        #endregion

        public Vector3 DirectionFromAngle(float angleInDegrees, bool angleIsGlobal)
        {
            if (angleIsGlobal == false) angleInDegrees += transform.eulerAngles.y;
            return new Vector3(Mathf.Sin(angleInDegrees * Mathf.Deg2Rad), 0, Mathf.Cos(angleInDegrees * Mathf.Deg2Rad));
        }

        public LayerMask UnitsMask => unitsMask;

        public Collider[] LooseItemsInViewRadius => looseItemsInViewRadius;

        public float ViewRadius => viewRadius;
        public float ViewAngle => viewAngle;

        public Unit Unit => unit;
    }
}
