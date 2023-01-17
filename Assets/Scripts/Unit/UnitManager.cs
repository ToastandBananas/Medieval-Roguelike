using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class UnitManager : MonoBehaviour
{
    public static UnitManager Instance { get; private set; }

    public Unit player { get; private set; }

    public List<Unit> units { get; private set; }
    public List<Unit> friendlyUnits { get; private set; }
    public List<Unit> enemyUnits { get; private set; }

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

        units = new List<Unit>();
        friendlyUnits = new List<Unit>();
        enemyUnits = new List<Unit>();

        units = FindObjectsOfType<Unit>().ToList();
    }

    public void AddUnitToUnitsList(Unit unit) => units.Add(unit);

    public void RemoveUnitFromUnitsList(Unit unit) => units.Remove(unit);
}
