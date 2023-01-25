using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class UnitManager : MonoBehaviour
{
    public static UnitManager Instance { get; private set; }

    public Unit player { get; private set; }

    public List<Unit> livingNPCs { get; private set; }
    public List<Unit> deadNPCs { get; private set; }
    //public List<Unit> friendlyNPCs { get; private set; }
    //public List<Unit> enemyNPCs { get; private set; }

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
        //friendlyNPCs = new List<Unit>();
        //enemyNPCs = new List<Unit>();

        livingNPCs = FindObjectsOfType<Unit>().ToList();
        for (int i = 0; i < livingNPCs.Count; i++)
        {
            //if (livingNPCs[i].IsPlayer())
                //livingNPCs.Remove(livingNPCs[i]);

            if (livingNPCs[i].isDead)
            {
                deadNPCs.Add(livingNPCs[i]);
                livingNPCs.Remove(livingNPCs[i]);
            }
        }
    }

    public void AddUnitToUnitsList(Unit unit) => livingNPCs.Add(unit);

    public void RemoveUnitFromUnitsList(Unit unit) => livingNPCs.Remove(unit);
}
