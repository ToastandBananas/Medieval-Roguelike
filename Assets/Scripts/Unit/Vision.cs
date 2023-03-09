/// <summary>
/// Derived from Sebastian Lague's Tutorial "Field of View Visualisation" (https://www.youtube.com/watch?v=rQG9aUWarwE)
/// </summary>

using System.Collections.Generic;
using UnityEngine;

public class Vision : MonoBehaviour
{
    [Header("Field of View")]
    [SerializeField] Transform parentTransform;
    public float viewRadius = 20f;
    [Range(0, 360)] public float viewAngle = 160f;

    [Header("Layer Masks")]
    public LayerMask unitsMask;
    public LayerMask obstacleMask;

    public List<Unit> visibleUnits { get; private set; }
    public List<Unit> visibleDeadUnits { get; private set; }
    public List<Unit> visibleEnemies { get; private set; }
    public List<Unit> visibleAllies { get; private set; }
    List<int> loseSightTimes = new List<int>();
    List<Unit> unitsToRemove = new List<Unit>();
    Collider[] unitsInViewRadius;

    Unit unit;
    readonly int loseSightTime = 60; // The amount of turns it takes to lose sight of a Unit, when out of their direct vision
    Vector3 yOffset = new Vector3(0, 0.15f, 0); // Height offset for where vision starts (the eyes)

    readonly float playerPerceptionDistance = 1.45f;

    void Awake()
    {
        visibleUnits = new List<Unit>();
        visibleDeadUnits = new List<Unit>();
        visibleEnemies = new List<Unit>();
        visibleAllies = new List<Unit>();
        unit = parentTransform.GetComponent<Unit>();
    }

    public bool IsVisible(Unit unitToCheck)
    {
        if (visibleUnits.Contains(unitToCheck))
            return true;
        return false;
    }

    public bool IsInLineOfSight(Unit unitToCheck)
    {
        float sphereCastRadius = 0.1f;
        Vector3 shootDir = ((unit.WorldPosition() + (Vector3.up * unit.ShoulderHeight() * 2f)) - (unitToCheck.WorldPosition() + (Vector3.up * unitToCheck.ShoulderHeight() * 2f))).normalized;
        if (Physics.SphereCast(unitToCheck.WorldPosition() + (Vector3.up * unitToCheck.ShoulderHeight() * 2f), sphereCastRadius, shootDir, out RaycastHit hit, Vector3.Distance(unit.WorldPosition() + (Vector3.up * unit.ShoulderHeight() * 2f), unitToCheck.WorldPosition() + (Vector3.up * unitToCheck.ShoulderHeight() * 2f)), unit.unitActionHandler.AttackObstacleMask()))
            return false; // Blocked by an obstacle
        return true;
    }

    public void FindVisibleUnits()
    {
        unitsInViewRadius = Physics.OverlapSphere(transform.position, viewRadius, unitsMask);
        for (int i = 0; i < unitsInViewRadius.Length; i++)
        {
            Transform targetTransform = unitsInViewRadius[i].transform;
            Unit targetUnit = LevelGrid.Instance.GetUnitAtGridPosition(LevelGrid.GetGridPosition(targetTransform.position));
            if (targetUnit != null && targetUnit != unit)
            {
                // If the Unit in the view radius is not already "visible"
                if (visibleUnits.Contains(targetUnit) == false)
                {
                    Vector3 dirToTarget = ((targetTransform.position + yOffset) - transform.position).normalized;

                    // If target Unit is in the view angle
                    if (Vector3.Angle(transform.forward, dirToTarget) < viewAngle / 2)
                    {
                        float distToTarget = Vector3.Distance(transform.position, targetTransform.position);

                        // If no obstacles are in the way, add the Unit to the visibleUnits dictionary
                        if (Physics.Raycast(transform.position, dirToTarget, distToTarget, obstacleMask) == false)
                            AddVisibleUnit(targetUnit);
                        else if (unit.IsPlayer() && distToTarget > playerPerceptionDistance) // Else, hide the NPC's mesh renderers
                            targetUnit.HideMeshRenderers();
                    }
                }
            }
        }

        if (unit.IsPlayer())
        {
            // Check if a "visible NPC Unit" is outside of the Player's view radius. If so, hide their mesh renderers
            for (int i = 0; i < visibleUnits.Count; i++)
            {
                bool shouldHide = true;
                for (int j = 0; j < unitsInViewRadius.Length; j++)
                {
                    if (visibleUnits[i].transform == unitsInViewRadius[j].transform && unitsInViewRadius[j].transform != unit.transform) // Skip if they're in the view radius
                    {
                        Transform targetTransform = unitsInViewRadius[j].transform;
                        Vector3 dirToTarget = ((targetTransform.position + yOffset) - transform.position).normalized;
                        float distToTarget = Vector3.Distance(transform.position, targetTransform.position);

                        // If target Unit is in the view angle and no obstacles are in the way
                        if (Vector3.Angle(transform.forward, dirToTarget) < viewAngle / 2 && Physics.Raycast(transform.position, dirToTarget, distToTarget, obstacleMask) == false)
                            shouldHide = false;
                    }
                }
                
                if (shouldHide && visibleUnits[i] != unit)
                {
                    if (Vector3.Distance(unit.transform.position, visibleUnits[i].transform.position) > playerPerceptionDistance)
                        visibleUnits[i].HideMeshRenderers();
                }
                else
                    visibleUnits[i].ShowMeshRenderers();
            }
        }

        // Flee or Fight if not already doing so, if there are now enemies visible to this NPC
        if (unit.IsNPC() && unit.stateController.currentState != State.Flee && unit.stateController.currentState != State.Fight)
        {
            if (visibleEnemies.Count > 0)
            {
                NPCActionHandler npcActionHandler = unit.unitActionHandler as NPCActionHandler;
                if (npcActionHandler.ShouldAlwaysFleeCombat())
                    npcActionHandler.StartFlee(GetClosestEnemy(true), npcActionHandler.DefaultFleeDistance());
                else
                    npcActionHandler.StartFight();
            }
        }
    }

    public void UpdateVisibleUnits()
    {
        unitsToRemove.Clear();

        // Check if Units that were in sight are now out of sight
        for (int i = 0; i < visibleUnits.Count; i++)
        {
            // If the visible Unit is now dead, update the appropriate lists
            if (visibleUnits[i].health.IsDead())
                UpdateDeadUnit(visibleUnits[i]);

            Transform targetTransform = visibleUnits[i].transform;
            Vector3 dirToTarget = ((targetTransform.position + yOffset) - transform.position).normalized;

            // If target Unit is in the view angle
            if (Vector3.Angle(transform.forward, dirToTarget) < viewAngle / 2)
            {
                float distToTarget = Vector3.Distance(transform.position, targetTransform.position);

                // If an obstacle is in the way, lower the lose sight time
                if (Physics.Raycast(transform.position, dirToTarget, distToTarget, obstacleMask))
                {
                    if (loseSightTimes[i] > 1)
                        loseSightTimes[i]--; // Subtract from the visible Unit's corresponding lose sight time
                    else
                        unitsToRemove.Add(visibleUnits[i]); // The Unit is no longer visible

                    if (unit.IsPlayer()) // Hide the NPC's mesh renderers
                        visibleUnits[i].HideMeshRenderers();
                }
                else // We can still see the Unit, so reset their lose sight time
                {
                    loseSightTimes[i] = loseSightTime;

                    if (unit.IsPlayer()) // Show the NPC's mesh renderers
                        visibleUnits[i].ShowMeshRenderers();
                }
            }
            else // The target is outside of the view angle
            {
                if (loseSightTimes[i] > 1)
                    loseSightTimes[i]--; // Subtract from the visible Unit's corresponding lose sight time
                else
                    unitsToRemove.Add(visibleUnits[i]); // The Unit is no longer visible

                if (unit.IsPlayer() && Vector3.Distance(unit.transform.position, targetTransform.position) > playerPerceptionDistance) // Hide the NPC's mesh renderers
                    visibleUnits[i].HideMeshRenderers();
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
        if (visibleUnits.Contains(unitToAdd) == false)
        {
            visibleUnits.Add(unitToAdd);

            // We don't want Units to see themselves
            if (visibleUnits.Contains(unit))
            {
                visibleUnits.Remove(unit);
                return;
            }

            if (unitToAdd.health.IsDead())
                visibleDeadUnits.Add(unitToAdd);
            else if (unit.alliance.IsEnemy(unitToAdd))
            {
                visibleEnemies.Add(unitToAdd);

                // The Player should cancel any action (other than attacks) when becoming aware of a new enemy
                if (unit.IsPlayer() && unit.unitActionHandler.queuedAction != null && unit.unitActionHandler.AttackQueued() == false)
                    StartCoroutine(unit.unitActionHandler.CancelAction());
            }
            else if (unit.alliance.IsAlly(unitToAdd))
                visibleAllies.Add(unitToAdd);

            // Add a corresponding lose sight time for this newly visible Unit
            loseSightTimes.Add(loseSightTime);

            // If this is the Player's Vision, show the newly visible NPC Unit
            if (unit.IsPlayer())
                unitToAdd.ShowMeshRenderers();
        }
        else
            loseSightTimes[visibleUnits.IndexOf(unitToAdd)] = loseSightTime;
    }

    public void RemoveVisibleUnit(Unit unitToRemove)
    {
        if (visibleUnits.Contains(unitToRemove))
        {
            loseSightTimes.RemoveAt(visibleUnits.IndexOf(unitToRemove));
            visibleUnits.Remove(unitToRemove);

            if (visibleDeadUnits.Contains(unitToRemove))
                visibleDeadUnits.Remove(unitToRemove);

            if (visibleEnemies.Contains(unitToRemove))
                visibleEnemies.Remove(unitToRemove);

            if (visibleAllies.Contains(unitToRemove))
                visibleAllies.Remove(unitToRemove);

            // If they are no longer visible to the player, hide them
            if (unit.IsPlayer())
                unitToRemove.HideMeshRenderers();
        }
    }

    public void UpdateDeadUnit(Unit deadUnit)
    {
        if (visibleDeadUnits.Contains(deadUnit) == false)
            visibleDeadUnits.Add(deadUnit);

        if (visibleEnemies.Contains(deadUnit))
            visibleEnemies.Remove(deadUnit);

        if (visibleAllies.Contains(deadUnit))
            visibleAllies.Remove(deadUnit);
    }

    public Unit GetClosestEnemy(bool includeTargetEnemy)
    {
        Unit closestEnemy = unit.unitActionHandler.targetEnemyUnit;
        float closestEnemyDist = 1000000;
        if (includeTargetEnemy && unit.unitActionHandler.targetEnemyUnit != null)
            closestEnemyDist = Vector3.Distance(unit.WorldPosition(), unit.unitActionHandler.targetEnemyUnit.WorldPosition());
        for (int i = 0; i < visibleEnemies.Count; i++)
        {
            if (unit.unitActionHandler.targetEnemyUnit != null)
            {
                if (unit.unitActionHandler.targetEnemyUnit.health.IsDead() || (includeTargetEnemy == false && visibleEnemies[i] == unit.unitActionHandler.targetEnemyUnit))
                    continue;
            }

            float distToEnemy = Vector3.Distance(transform.position, visibleEnemies[i].transform.position);
            if (distToEnemy < closestEnemyDist)
            {
                closestEnemy = visibleEnemies[i];
                closestEnemyDist = distToEnemy;
            }
        }
        return closestEnemy;
    }

    public Vector3 DirectionFromAngle(float angleInDegrees, bool angleIsGlobal)
    {
        if (angleIsGlobal == false) angleInDegrees += transform.eulerAngles.y;
        return new Vector3(Mathf.Sin(angleInDegrees * Mathf.Deg2Rad), 0, Mathf.Cos(angleInDegrees * Mathf.Deg2Rad));
    }
}
