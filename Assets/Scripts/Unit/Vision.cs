/// <summary>
/// Derived from Sebastian Lague's Tutorial "Field of View Visualisation" (https://www.youtube.com/watch?v=rQG9aUWarwE)
/// </summary>

using System.Collections.Concurrent;
using System.Collections.Generic;
using UnityEngine;
using InteractableObjects;
using ActionSystem;
using GridSystem;

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

        public Collider[] looseItemsInViewRadius;
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

        public bool IsVisible(Unit unitToCheck)
        {
            if (knownUnits.ContainsKey(unitToCheck) == false)
                return false;

            if (unitToCheck.unitMeshManager.meshesHidden)
                return false;

            if (IsInLineOfSight_Raycast(unitToCheck) == false)
                return false;

            return true;
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
            Vector3 offset = Vector3.up * unitToCheck.ShoulderHeight * 2f;
            Vector3 shootDir = (unitToCheck.WorldPosition + offset - transform.position).normalized;
            float distToTarget = Vector3.Distance(transform.position, unitToCheck.WorldPosition + offset);
            if (Physics.SphereCast(transform.position, sphereCastRadius, shootDir, out RaycastHit hit, distToTarget, unit.unitActionHandler.AttackObstacleMask))
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
                            targetUnit.unitMeshManager.HideMeshRenderers();
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
                        if (knownUnit.Key.unitActionHandler.targetEnemyUnit != unit && Vector3.Distance(unit.transform.position, knownUnit.Key.transform.position) > playerPerceptionDistance)
                        {
                            knownUnit.Key.unitMeshManager.HideMeshRenderers();
                            if (unit.unitActionHandler.targetEnemyUnit == knownUnit.Key)
                                unit.unitActionHandler.SetTargetEnemyUnit(null);
                        }
                    }
                    else
                        knownUnit.Key.unitMeshManager.ShowMeshRenderers();
                }
            }

            // Flee or Fight if not already doing so, if there are now enemies visible to this NPC
            if (unit.IsNPC && unit.stateController.currentState != State.Flee && unit.stateController.currentState != State.Fight)
            {
                if (knownEnemies.Count > 0)
                {
                    NPCActionHandler npcActionHandler = unit.unitActionHandler as NPCActionHandler;
                    if (npcActionHandler.ShouldAlwaysFleeCombat())
                        npcActionHandler.StartFlee(GetClosestEnemy(true), npcActionHandler.DefaultFleeDistance());
                    else
                        npcActionHandler.StartFight();
                }
            }
        }

        void UpdateVisibleUnits()
        {
            unitsToRemove.Clear();

            // Check if Units that were in sight are now out of sight
            foreach (KeyValuePair<Unit, int> knownUnit in knownUnits)
            {
                // If the visible Unit is now dead, update the appropriate lists
                if (knownUnit.Key.health.IsDead())
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

                        if (unit.IsPlayer && knownUnit.Key.unitActionHandler.targetEnemyUnit != unit) // Hide the NPC's mesh renderers
                            knownUnit.Key.unitMeshManager.HideMeshRenderers();
                    }
                    else // We can still see the Unit, so reset their lose sight time
                    {
                        knownUnits[knownUnit.Key] = loseSightTime;

                        if (unit.IsPlayer) // Show the NPC's mesh renderers
                            knownUnit.Key.unitMeshManager.ShowMeshRenderers();
                    }
                }
                else // The target is outside of the view angle
                {
                    if (knownUnit.Value > 1)
                        knownUnits[knownUnit.Key]--; // Subtract from the visible Unit's corresponding lose sight time
                    else
                        unitsToRemove.Add(knownUnit.Key); // The Unit is no longer visible

                    if (unit.IsPlayer && knownUnit.Key.unitActionHandler.targetEnemyUnit != unit && Vector3.Distance(unit.transform.position, targetTransform.position) > playerPerceptionDistance) // Hide the NPC's mesh renderers
                        knownUnit.Key.unitMeshManager.HideMeshRenderers();
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

                if (unitToAdd.health.IsDead())
                    knownDeadUnits.Add(unitToAdd);
                else if (unit.alliance.IsEnemy(unitToAdd))
                {
                    knownEnemies.Add(unitToAdd);

                    // The Player should cancel any action (other than attacks) when becoming aware of a new enemy
                    if (unit.IsPlayer && unit.unitActionHandler.queuedActions.Count > 0 && unit.unitActionHandler.AttackQueuedNext() == false)
                        unit.unitActionHandler.CancelActions();
                }
                else if (unit.alliance.IsAlly(unitToAdd))
                    knownAllies.Add(unitToAdd);
            }
            else // Restart the lose sight countdown
                knownUnits[unitToAdd] = loseSightTime;

            // If this is the Player's Vision, show the newly visible NPC Unit
            if (unit.IsPlayer)
                unitToAdd.unitMeshManager.ShowMeshRenderers();
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

                if (unit.unitActionHandler.targetEnemyUnit == unitToRemove)
                    unit.unitActionHandler.SetTargetEnemyUnit(null);

                // If they are no longer visible to the player, hide them
                if (unit.IsPlayer)
                    unitToRemove.unitMeshManager.HideMeshRenderers();
            }
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

        public Unit GetClosestEnemy(bool includeTargetEnemy)
        {
            Unit closestEnemy = unit.unitActionHandler.targetEnemyUnit;
            float closestEnemyDist = 1000000;
            if (includeTargetEnemy && unit.unitActionHandler.targetEnemyUnit != null)
                closestEnemyDist = Vector3.Distance(unit.WorldPosition, unit.unitActionHandler.targetEnemyUnit.WorldPosition);
            for (int i = 0; i < knownEnemies.Count; i++)
            {
                if (unit.unitActionHandler.targetEnemyUnit != null)
                {
                    if (unit.unitActionHandler.targetEnemyUnit.health.IsDead() || (includeTargetEnemy == false && knownEnemies[i] == unit.unitActionHandler.targetEnemyUnit))
                        continue;
                }

                float distToEnemy = Vector3.Distance(transform.position, knownEnemies[i].transform.position);
                if (distToEnemy < closestEnemyDist)
                {
                    closestEnemy = knownEnemies[i];
                    closestEnemyDist = distToEnemy;
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

                looseItemsInViewRadius[i].transform.TryGetComponent(out LooseItem looseItem);

                // If the LooseItem in the view radius is already "known", skip it
                if (looseItem == null || knownLooseItems.ContainsKey(looseItem))
                    continue;

                Vector3 looseItemCenter = looseItem.transform.TransformPoint(looseItem.MeshCollider.sharedMesh.bounds.center);
                Vector3 dirToTarget = (looseItemCenter - transform.position).normalized;

                // If target LooseItem is in the view angle
                float distToTarget = Vector3.Distance(transform.position, looseItemCenter);
                if (TargetInViewAngle(dirToTarget))
                {
                    // If no obstacles are in the way, add the LooseItem to the knownLooseItems dictionary
                    if (Physics.Raycast(transform.position, dirToTarget, distToTarget, looseItemVisionObstacleMask) == false)
                        AddVisibleLooseItem(looseItem);
                    else if (unit.IsPlayer && distToTarget > playerPerceptionDistance) // Else, hide the LooseItem's mesh renderers
                        looseItem.HideMeshRenderer();
                }
                else if (unit.IsPlayer && distToTarget > playerPerceptionDistance)
                    looseItem.HideMeshRenderer();
            }

            if (unit.IsPlayer)
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
            }
        }

        void UpdateVisibleLooseItems()
        {
            looseItemsToRemove.Clear();

            // Check if LooseItems that were in sight are now out of sight
            foreach (KeyValuePair<LooseItem, int> looseItem in knownLooseItems)
            {
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

                        if (unit.IsPlayer) // Hide the LooseItem's mesh renderers
                            looseItem.Key.HideMeshRenderer();
                    }
                    else // We can still see the LooseItem, so reset their lose sight time
                    {
                        knownLooseItems[looseItem.Key] = loseSightTime;

                        if (unit.IsPlayer) // Show the LooseItem's mesh renderers
                            looseItem.Key.ShowMeshRenderer();
                    }
                }
                else // The target is outside of the view angle
                {
                    if (looseItem.Value > 1)
                        knownLooseItems[looseItem.Key]--; // Subtract from the visible LooseItem's corresponding lose sight time
                    else
                        looseItemsToRemove.Add(looseItem.Key); // The LooseItem is no longer visible

                    if (unit.IsPlayer && Vector3.Distance(unit.transform.position, looseItemCenter) > playerPerceptionDistance) // Hide the LooseItem's mesh renderers
                        looseItem.Key.HideMeshRenderer();
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

        void RemoveVisibleLooseItem(LooseItem looseItemToRemove)
        {
            if (knownLooseItems.ContainsKey(looseItemToRemove))
            {
                knownLooseItems.Remove(looseItemToRemove, out int value);

                // If they are no longer visible to the player, hide them
                if (unit.IsPlayer)
                    looseItemToRemove.HideMeshRenderer();
            }
        }

        public LooseItem GetClosestLooseItem()
        {
            LooseItem closestLooseItem = null;
            float closestLooseItemDistance = 1000000;
            foreach (KeyValuePair<LooseItem, int> knownLooseItem in knownLooseItems)
            {
                float distToLooseItem = Vector3.Distance(transform.position, knownLooseItem.Key.transform.TransformPoint(knownLooseItem.Key.MeshCollider.sharedMesh.bounds.center));
                if (distToLooseItem < closestLooseItemDistance)
                {
                    closestLooseItem = knownLooseItem.Key;
                    closestLooseItemDistance = distToLooseItem;
                }
            }
            return closestLooseItem;
        }
        #endregion

        public Vector3 DirectionFromAngle(float angleInDegrees, bool angleIsGlobal)
        {
            if (angleIsGlobal == false) angleInDegrees += transform.eulerAngles.y;
            return new Vector3(Mathf.Sin(angleInDegrees * Mathf.Deg2Rad), 0, Mathf.Cos(angleInDegrees * Mathf.Deg2Rad));
        }

        public LayerMask UnitsMask => unitsMask;

        public float ViewRadius => viewRadius;

        public float ViewAngle => viewAngle;

        public Unit Unit => unit;
    }
}
