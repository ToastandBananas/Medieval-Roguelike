using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class UnitManager : MonoBehaviour
{
    public static UnitManager Instance { get; private set; }

    public static Unit player { get; private set; }

    public static List<Unit> livingNPCs { get; private set; }
    public static List<Unit> deadNPCs { get; private set; }

    void Awake()
    {
        if (Instance != null)
        {
            if (Instance != this)
            {
                Debug.LogWarning("More than one Instance of UnitManager. Fix me!");
                Destroy(gameObject);
                return;
            }
        }
        else
            Instance = this;

        player = GameObject.FindGameObjectWithTag("Player").GetComponent<Unit>();

        livingNPCs = new List<Unit>();
        deadNPCs = new List<Unit>();

        livingNPCs = FindObjectsOfType<Unit>().ToList();
        livingNPCs.Remove(player);
    }

    void Start()
    {
        for (int i = 0; i < livingNPCs.Count; i++)
        {
            if (livingNPCs[i].health.IsDead())
            {
                deadNPCs.Add(livingNPCs[i]);
                livingNPCs.Remove(livingNPCs[i]);
            }
        }
    }

    public static void AddUnitToNPCList(Unit unit) => livingNPCs.Add(unit);

    public static void RemoveUnitFromNPCList(Unit unit) => livingNPCs.Remove(unit);
}
