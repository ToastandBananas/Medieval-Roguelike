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

    Unit unit;
    readonly int loseSightTime = 60; // The amount of turns it takes to lose sight of a Unit, when out of their direct vision
    Vector3 yOffset = new Vector3(0, 0.15f, 0);

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

    public void FindVisibleUnits()
    {
        Collider[] unitsInViewRadius = Physics.OverlapSphere(transform.position, viewRadius, unitsMask);
        for (int i = 0; i < unitsInViewRadius.Length; i++)
        {
            Transform targetTransform = unitsInViewRadius[i].transform;
            Unit targetUnit = LevelGrid.Instance.GetUnitAtGridPosition(LevelGrid.Instance.GetGridPosition(targetTransform.position));
            if (targetUnit != null && targetUnit.health.IsDead() == false && visibleUnits.Contains(targetUnit) == false)
            {
                Vector3 dirToTarget = (targetTransform.position + yOffset - transform.position).normalized;

                // If target Unit is in the view angle
                if (Vector3.Angle(transform.forward, dirToTarget) < viewAngle / 2)
                {
                    float distToTarget = Vector3.Distance(transform.position, targetTransform.position);

                    // If no obstacles are in the way, add the Unit to the visibleUnits dictionary
                    if (Physics.Raycast(transform.position, dirToTarget, distToTarget, obstacleMask) == false)
                        AddVisibleUnit(targetUnit);
                }
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
        List<Unit> unitsToRemove = new List<Unit>();

        // Check if Units that were in sight are now out of sight
        for (int i = 0; i < visibleUnits.Count; i++)
        {
            // If the visible Unit is now dead, update the appropriate lists
            if (visibleUnits[i].health.IsDead() && visibleDeadUnits.Contains(visibleUnits[i]) == false)
            {
                visibleDeadUnits.Add(visibleUnits[i]);
                if (visibleEnemies.Contains(visibleUnits[i]))
                    visibleEnemies.Remove(visibleUnits[i]);
            }

            Transform targetTransform = visibleUnits[i].transform;
            Vector3 dirToTarget = (targetTransform.position + yOffset - transform.position).normalized;

            // If target Unit is in the view angle
            if (Vector3.Angle(transform.forward, dirToTarget) < viewAngle / 2)
            {
                float distToTarget = Vector3.Distance(transform.position, targetTransform.position);

                // If an obstacle is in the way, remove the target Unit from the visibleUnits dictionary
                if (Physics.Raycast(transform.position, dirToTarget, distToTarget, obstacleMask))
                {
                    if (loseSightTimes[i] > 1)
                        loseSightTimes[i]--; // Subtract from the visible Unit's corresponding lose sight time
                    else
                        unitsToRemove.Add(visibleUnits[i]); // The Unit is no longer visible
                }
                else // We can still see the Unit, so reset their lose sight time
                    loseSightTimes[i] = loseSightTime;
            }
            else // The target is outside of the view angle
            {
                if (loseSightTimes[i] > 1)
                    loseSightTimes[i]--; // Subtract from the visible Unit's corresponding lose sight time
                else
                    unitsToRemove.Add(visibleUnits[i]); // The Unit is no longer visible
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
                Debug.LogWarning(unit.name + " can see themselves. Fix their Vision Transform.");
                visibleUnits.Remove(unit);
                return;
            }

            if (unitToAdd.health.IsDead())
                visibleDeadUnits.Add(unitToAdd);
            else if (unit.alliance.IsEnemy(unitToAdd.alliance.CurrentFaction()))
            {
                visibleEnemies.Add(unitToAdd);
                if (unit.IsPlayer())
                    StartCoroutine(unit.unitActionHandler.CancelAction());
            }
            else if (unit.alliance.IsAlly(unitToAdd.alliance.CurrentFaction()))
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

    void RemoveVisibleUnit(Unit unitToRemove)
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

    public Unit GetClosestEnemy(bool includeTargetEnemy)
    {
        Unit closestEnemy = unit.unitActionHandler.targetEnemyUnit;
        float closestEnemyDist = 1000000;
        if (includeTargetEnemy && unit.unitActionHandler.targetEnemyUnit != null)
            closestEnemyDist = Vector3.Distance(unit.WorldPosition(), unit.unitActionHandler.targetEnemyUnit.WorldPosition());
        for (int i = 0; i < visibleEnemies.Count; i++)
        {
            if (includeTargetEnemy == false && unit.unitActionHandler.targetEnemyUnit != null && visibleEnemies[i] == unit.unitActionHandler.targetEnemyUnit)
                continue;

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
