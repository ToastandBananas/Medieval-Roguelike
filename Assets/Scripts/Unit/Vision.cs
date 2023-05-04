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

    public List<Unit> knownUnits { get; private set; }
    public List<Unit> knownDeadUnits { get; private set; }
    public List<Unit> knownEnemies { get; private set; }
    public List<Unit> knownAllies { get; private set; }
    List<int> loseSightTimes = new List<int>();
    List<Unit> unitsToRemove = new List<Unit>();
    Collider[] unitsInViewRadius;

    Unit unit;
    readonly int loseSightTime = 60; // The amount of turns it takes to lose sight of a Unit, when out of their direct vision
    Vector3 yOffset = new Vector3(0, 0.15f, 0); // Height offset for where vision starts (the eyes)

    readonly float playerPerceptionDistance = 5f;

    void Awake()
    {
        knownUnits = new List<Unit>();
        knownDeadUnits = new List<Unit>();
        knownEnemies = new List<Unit>();
        knownAllies = new List<Unit>();
        unit = parentTransform.GetComponent<Unit>();
    }

    public bool IsVisible(Unit unitToCheck)
    {
        if (knownUnits.Contains(unitToCheck) == false)
            return false;

        if (unitToCheck.CanSeeMeshRenderers() == false)
            return false;

        if (IsInLineOfSight_Raycast(unitToCheck.transform) == false)
            return false;

        return true;
    }

    public bool IsKnown(Unit unitToCheck)
    {
        if (knownUnits.Contains(unitToCheck))
            return true;
        return false;
    }

    public bool IsInLineOfSight_SphereCast(Unit unitToCheck)
    {
        float sphereCastRadius = 0.1f;
        Vector3 offset = Vector3.up * unitToCheck.ShoulderHeight() * 2f;
        Vector3 shootDir = ((unit.WorldPosition() + offset) - (unitToCheck.WorldPosition() + offset)).normalized;
        if (Physics.SphereCast(unitToCheck.WorldPosition() + offset, sphereCastRadius, shootDir, out RaycastHit hit, Vector3.Distance(unit.WorldPosition() + offset, unitToCheck.WorldPosition() + offset), unit.unitActionHandler.AttackObstacleMask()))
            return false; // Blocked by an obstacle
        return true;
    }

    bool IsInLineOfSight_Raycast(Transform transformToCheck)
    {
        Vector3 dirToTarget = ((transformToCheck.position + yOffset) - transform.position).normalized;
        float distToTarget = Vector3.Distance(transform.position, transformToCheck.position);

        // If target Unit is in the view angle and no obstacles are in the way
        if (Physics.Raycast(transform.position, dirToTarget, distToTarget, obstacleMask) == false)
            return true;
        return false;
    }

    public void FindVisibleUnits()
    {
        unitsInViewRadius = Physics.OverlapSphere(transform.position, viewRadius, unitsMask);
        for (int i = 0; i < unitsInViewRadius.Length; i++)
        {
            Transform targetTransform = unitsInViewRadius[i].transform;
            targetTransform.TryGetComponent(out Unit targetUnit);
            if (targetUnit != null && targetUnit != unit)
            {
                // If the Unit in the view radius is not already "visible"
                if (knownUnits.Contains(targetUnit)) continue;

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

        if (unit.IsPlayer())
        {
            // Check if a "visible NPC Unit" is outside of the Player's view radius. If so, hide their mesh renderers
            for (int i = 0; i < knownUnits.Count; i++)
            {
                bool shouldHide = true;
                for (int j = 0; j < unitsInViewRadius.Length; j++)
                {
                    if (knownUnits[i].transform == unitsInViewRadius[j].transform && unitsInViewRadius[j].transform != unit.transform) 
                    {
                        // Skip if they're in the view radius and there's no obstacles in the way
                        Vector3 dirToTarget = ((unitsInViewRadius[j].transform.position + yOffset) - transform.position).normalized;
                        float distToTarget = Vector3.Distance(transform.position, unitsInViewRadius[j].transform.position);

                        // If target Unit is in the view angle and no obstacles are in the way
                        if (Vector3.Angle(transform.forward, dirToTarget) < viewAngle / 2 && Physics.Raycast(transform.position, dirToTarget, distToTarget, obstacleMask) == false)
                            shouldHide = false;
                    }
                }
                
                if (shouldHide && knownUnits[i] != unit)
                {
                    if (Vector3.Distance(unit.transform.position, knownUnits[i].transform.position) > playerPerceptionDistance)
                        knownUnits[i].HideMeshRenderers();
                }
                else
                    knownUnits[i].ShowMeshRenderers();
            }
        }

        // Flee or Fight if not already doing so, if there are now enemies visible to this NPC
        if (unit.IsNPC() && unit.stateController.currentState != State.Flee && unit.stateController.currentState != State.Fight)
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

    public void UpdateVisibleUnits()
    {
        unitsToRemove.Clear();

        // Check if Units that were in sight are now out of sight
        for (int i = 0; i < knownUnits.Count; i++)
        {
            // If the visible Unit is now dead, update the appropriate lists
            if (knownUnits[i].health.IsDead())
                UpdateDeadUnit(knownUnits[i]);

            Transform targetTransform = knownUnits[i].transform;
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
                        unitsToRemove.Add(knownUnits[i]); // The Unit is no longer visible

                    if (unit.IsPlayer()) // Hide the NPC's mesh renderers
                        knownUnits[i].HideMeshRenderers();
                }
                else // We can still see the Unit, so reset their lose sight time
                {
                    loseSightTimes[i] = loseSightTime;

                    if (unit.IsPlayer()) // Show the NPC's mesh renderers
                        knownUnits[i].ShowMeshRenderers();
                }
            }
            else // The target is outside of the view angle
            {
                if (loseSightTimes[i] > 1)
                    loseSightTimes[i]--; // Subtract from the visible Unit's corresponding lose sight time
                else
                    unitsToRemove.Add(knownUnits[i]); // The Unit is no longer visible

                if (unit.IsPlayer() && Vector3.Distance(unit.transform.position, targetTransform.position) > playerPerceptionDistance) // Hide the NPC's mesh renderers
                    knownUnits[i].HideMeshRenderers();
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
        if (knownUnits.Contains(unitToAdd) == false)
        {
            knownUnits.Add(unitToAdd);

            // We don't want Units to see themselves
            if (knownUnits.Contains(unit))
            {
                knownUnits.Remove(unit);
                return;
            }

            if (unitToAdd.health.IsDead())
                knownDeadUnits.Add(unitToAdd);
            else if (unit.alliance.IsEnemy(unitToAdd))
            {
                knownEnemies.Add(unitToAdd);

                // The Player should cancel any action (other than attacks) when becoming aware of a new enemy
                if (unit.IsPlayer() && unit.unitActionHandler.queuedAction != null && unit.unitActionHandler.AttackQueued() == false)
                    StartCoroutine(unit.unitActionHandler.CancelAction());
            }
            else if (unit.alliance.IsAlly(unitToAdd))
                knownAllies.Add(unitToAdd);

            // Add a corresponding lose sight time for this newly visible Unit
            loseSightTimes.Add(loseSightTime);

            // If this is the Player's Vision, show the newly visible NPC Unit
            if (unit.IsPlayer())
                unitToAdd.ShowMeshRenderers();
        }
        else
            loseSightTimes[knownUnits.IndexOf(unitToAdd)] = loseSightTime;
    }

    public void RemoveVisibleUnit(Unit unitToRemove)
    {
        if (knownUnits.Contains(unitToRemove))
        {
            loseSightTimes.RemoveAt(knownUnits.IndexOf(unitToRemove));
            knownUnits.Remove(unitToRemove);

            if (knownDeadUnits.Contains(unitToRemove))
                knownDeadUnits.Remove(unitToRemove);

            if (knownEnemies.Contains(unitToRemove))
                knownEnemies.Remove(unitToRemove);

            if (knownAllies.Contains(unitToRemove))
                knownAllies.Remove(unitToRemove);

            // If they are no longer visible to the player, hide them
            if (unit.IsPlayer())
                unitToRemove.HideMeshRenderers();
        }
    }

    public void UpdateDeadUnit(Unit deadUnit)
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
            closestEnemyDist = Vector3.Distance(unit.WorldPosition(), unit.unitActionHandler.targetEnemyUnit.WorldPosition());
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

    public Vector3 DirectionFromAngle(float angleInDegrees, bool angleIsGlobal)
    {
        if (angleIsGlobal == false) angleInDegrees += transform.eulerAngles.y;
        return new Vector3(Mathf.Sin(angleInDegrees * Mathf.Deg2Rad), 0, Mathf.Cos(angleInDegrees * Mathf.Deg2Rad));
    }
}
