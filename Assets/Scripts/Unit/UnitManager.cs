using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class UnitManager : MonoBehaviour
{
    public static UnitManager Instance { get; private set; }

    Unit player;
    List<Unit> units = new List<Unit>();
    List<Unit> friendlyUnits = new List<Unit>();
    List<Unit> enemyUnits = new List<Unit>();

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

        units = FindObjectsOfType<Unit>().ToList();
    }

    void Start()
    {
        
    }

    public void AddUnitToUnitsList(Unit unit)
    {
        units.Add(unit);
    }

    public void RemoveUnitFromUnitsList(Unit unit)
    {
        units.Remove(unit);
    }

    public Unit Player() => player;

    public List<Unit> UnitsList() => units;

    public List<Unit> FriendlyUnitsList() => friendlyUnits;

    public List<Unit> EnemyUnitsList() => enemyUnits;
}
