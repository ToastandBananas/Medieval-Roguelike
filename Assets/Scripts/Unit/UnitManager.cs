using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class UnitManager : MonoBehaviour
{
    public static UnitManager Instance { get; private set; }

    List<Unit> units = new List<Unit>();
    List<Unit> playerUnits = new List<Unit>();
    List<Unit> friendlyUnits = new List<Unit>();
    List<Unit> enemyUnits = new List<Unit>();

    void Awake()
    {
        if (Instance != null)
        {
            Debug.LogError("There's more than one UnitManager! " + transform + " - " + Instance);
            Destroy(gameObject);
            return;
        }
        Instance = this;

        units = FindObjectsOfType<Unit>().ToList();
    }

    void Start()
    {
        Unit.OnAnyUnitSpawned += Unit_OnAnyUnitSpawned;
        Unit.OnAnyUnitDead += Unit_OnAnyUnitDead;
    }

    public void AddUnitToUnitsList(Unit unit)
    {
        units.Add(unit);
    }

    public void RemoveUnitFromUnitsList(Unit unit)
    {
        units.Remove(unit);
    }

    public List<Unit> UnitsList() => units;

    public List<Unit> PlayerUnitsList() => playerUnits;

    public List<Unit> FriendlyUnitsList() => friendlyUnits;

    public List<Unit> EnemyUnitsList() => enemyUnits;

    void Unit_OnAnyUnitSpawned(object sender, EventArgs e)
    {
        Unit unit = (Unit)sender;
        units.Add(unit);
        if (unit.IsEnemy(Faction.Player))
            enemyUnits.Add(unit);
        else if (unit.IsPlayer())
            playerUnits.Add(unit);
        else
            friendlyUnits.Add(unit);
    }

    void Unit_OnAnyUnitDead(object sender, EventArgs e)
    {
        Unit unit = (Unit)sender;
        units.Remove(unit);
        if (unit.IsEnemy(Faction.Player))
            enemyUnits.Remove(unit);
        else if (unit.IsPlayer())
            playerUnits.Remove(unit);
        else
            friendlyUnits.Remove(unit);
    }
}
