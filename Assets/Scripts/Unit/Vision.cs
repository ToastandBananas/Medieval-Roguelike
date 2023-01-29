/// <summary>
/// Derived from Sebastian Lague's Tutorial "Field of View Visualisation" (https://www.youtube.com/watch?v=rQG9aUWarwE)
/// </summary>

using System.Collections.Generic;
using System.Linq;
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

    public List<Unit> visibleUnits = new List<Unit>();
    List<int> loseSightTimes = new List<int>();
    public List<Unit> visibleEnemies = new List<Unit>();

    Unit unit;
    readonly int loseSightOfEnemyCount = 120; // In turns (seconds)
    Vector3 yOffset = new Vector3(0, 0.15f, 0);

    void Awake()
    {
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
            if (targetUnit != null && visibleUnits.Contains(targetUnit) == false)
            {
                Vector3 dirToTarget = (targetTransform.position + yOffset - transform.position).normalized;

                // If target Unit is in the view angle
                if (Vector3.Angle(transform.forward, dirToTarget) < viewAngle / 2)
                {
                    float distToTarget = Vector3.Distance(transform.position, targetTransform.position);

                    // If no obstacles are in the way, add the Unit to the visibleUnits dictionary
                    if (Physics.Raycast(transform.position, dirToTarget, distToTarget, obstacleMask) == false)
                    {
                        // Debug.Log("Adding " + targetTransform.name + " to the dictionary.");
                        visibleUnits.Add(targetUnit);

                        if (unit.alliance.IsEnemy(targetUnit.alliance.CurrentFaction()))
                            visibleEnemies.Add(targetUnit);

                        // We don't want Units to see themselves
                        if (visibleUnits.Contains(unit))
                        {
                            Debug.LogWarning(unit.name + " can see themselves. Fix their Vision Transform.");
                            visibleUnits.Remove(unit);
                            continue;
                        }

                        // Add a corresponding lose sight time for this newly visible Unit
                        loseSightTimes.Add(loseSightOfEnemyCount);

                        // If this is the Player's Vision, show the newly visible NPC Unit
                        if (unit.IsPlayer())
                            targetUnit.ShowMeshRenderers();
                    }
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
                    npcActionHandler.StartFlee(GetClosestEnemy());
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
                    {
                        // Subtract from the visible Unit's corresponding lose sight time
                        loseSightTimes[i]--;
                    }
                    else // The Unit is no longer visible
                    {
                        unitsToRemove.Add(visibleUnits[i]);

                        // If they are no longer visible to the player, hide them
                        if (unit.IsPlayer())
                            visibleUnits[i].HideMeshRenderers();
                    }
                }
                else
                {
                    // We can still see the Unit, so reset their lose sight time
                    loseSightTimes[i] = loseSightOfEnemyCount;
                }
            }
            else // The target is outside of the view angle
            {
                if (loseSightTimes[i] > 1)
                {
                    // Subtract from the visible Unit's corresponding lose sight time
                    loseSightTimes[i]--;
                }
                else // The Unit is no longer visible
                {
                    unitsToRemove.Add(visibleUnits[i]);

                    // If they are no longer visible to the player, hide them
                    if (unit.IsPlayer())
                        visibleUnits[i].HideMeshRenderers();
                }
            }
        }

        for (int i = 0; i < unitsToRemove.Count; i++)
        {
            // Debug.Log("Removing " + unitsToRemove[i].name + " from the dictionary.");
            loseSightTimes.RemoveAt(visibleUnits.IndexOf(unitsToRemove[i]));
            visibleUnits.Remove(unitsToRemove[i]);
            if (visibleEnemies.Contains(unitsToRemove[i]))
                visibleEnemies.Remove(unitsToRemove[i]);
        }
    }

    public Unit GetClosestEnemy()
    {
        Unit closestEnemy = null;
        float closestEnemyDist = 100000;
        for (int i = 0; i < visibleEnemies.Count; i++)
        {
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
