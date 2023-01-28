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

    public Dictionary<Unit, Transform> visibleUnits = new Dictionary<Unit, Transform>();

    Unit unit;
    Vector3 yOffset = new Vector3(0, 0.15f, 0);

    void Awake()
    {
        unit = parentTransform.GetComponent<Unit>();
    }

    public bool IsVisible(Unit unitToCheck)
    {
        if (visibleUnits.ContainsKey(unitToCheck))
            return true;
        return false;
    }

    public void FindVisibleUnits()
    {
        List<Unit> unitsToRemove = new List<Unit>();

        // Check if Units that were in sight are now out of sight
        foreach (KeyValuePair<Unit, Transform> keyValuePair in visibleUnits)
        {
            Transform target = keyValuePair.Value;
            Vector3 dirToTarget = (target.position + yOffset - transform.position).normalized;

            // If target Unit is in the view angle
            if (Vector3.Angle(transform.forward, dirToTarget) < viewAngle / 2)
            {
                float distToTarget = Vector3.Distance(transform.position, target.position);

                // If an obstacle is in the way, remove the target Unit from the visibleUnits dictionary
                if (Physics.Raycast(transform.position, dirToTarget, distToTarget, obstacleMask))
                    unitsToRemove.Add(keyValuePair.Key);
            }
            else
                unitsToRemove.Add(keyValuePair.Key);
        }

        for (int i = 0; i < unitsToRemove.Count; i++)
        {
            // Debug.Log("Removing " + unitsToRemove[i].name + " from the dictionary.");
            visibleUnits.Remove(unitsToRemove[i]);
            if (unit.IsPlayer())
                unitsToRemove[i].HideMeshRenderers();
        }

        Collider[] unitsInViewRadius = Physics.OverlapSphere(transform.position, viewRadius, unitsMask);
        for (int i = 0; i < unitsInViewRadius.Length; i++)
        {
            Transform target = unitsInViewRadius[i].transform;
            if (visibleUnits.ContainsValue(target) == false)
            {
                Vector3 dirToTarget = (target.position + yOffset - transform.position).normalized;

                // If target Unit is in the view angle
                if (Vector3.Angle(transform.forward, dirToTarget) < viewAngle / 2)
                {
                    float distToTarget = Vector3.Distance(transform.position, target.position);

                    // If no obstacles are in the way, add the Unit to the visibleUnits dictionary
                    if (Physics.Raycast(transform.position, dirToTarget, distToTarget, obstacleMask) == false)
                    {
                        // Debug.Log("Adding " + target.name + " to the dictionary.");
                        Unit targetUnit = target.GetComponent<Unit>();
                        visibleUnits.Add(targetUnit, target);

                        // We don't want Units to see themselves
                        if (visibleUnits.ContainsKey(unit))
                        {
                            Debug.LogWarning(unit.name + " can see themselves. Fix their Vision Transform.");
                            visibleUnits.Remove(unit);
                            continue;
                        }

                        if (unit.IsNPC())
                        {
                            if (unit.alliance.IsEnemy(targetUnit.alliance.CurrentFaction()))
                            {
                                NPCActionHandler npcActionHandler = unit.unitActionHandler as NPCActionHandler;
                                if (npcActionHandler.ShouldAlwaysFleeCombat())
                                    npcActionHandler.StartFlee(targetUnit);
                                else
                                    npcActionHandler.StartFight(targetUnit);
                            }
                        }
                        else
                        {
                            targetUnit.ShowMeshRenderers();
                        }
                    }
                }
            }
        }
    }

    public Vector3 DirectionFromAngle(float angleInDegrees, bool angleIsGlobal)
    {
        if (angleIsGlobal == false) angleInDegrees += transform.eulerAngles.y;
        return new Vector3(Mathf.Sin(angleInDegrees * Mathf.Deg2Rad), 0, Mathf.Cos(angleInDegrees * Mathf.Deg2Rad));
    }
}
