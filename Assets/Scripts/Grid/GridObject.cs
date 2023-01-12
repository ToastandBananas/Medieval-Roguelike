using System.Collections.Generic;
using UnityEngine;

public class GridObject
{
    GridPosition gridPosition;
    List<Unit> unitList;
    //Door door;

    public GridObject(GridPosition gridPosition)
    {
        this.gridPosition = gridPosition;
        unitList = new List<Unit>();
    }

    public override string ToString()
    {
        string unitString = "";
        for (int i = 0; i < unitList.Count; i++)
        {
            unitString += unitList[i].name + "\n";
        }

        return gridPosition.ToString() + "\n" + unitString;
    }

    public void AddUnit(Unit unit)
    {
        unitList.Add(unit);
    }

    public void RemoveUnit(Unit unit)
    {
        unitList.Remove(unit);
    }

    public List<Unit> GetUnitList()
    {
        return unitList;
    }

    public bool HasAnyUnit()
    {
        return unitList.Count > 0;
    }

    public Unit GetUnit()
    {
        if (HasAnyUnit())
            return unitList[0];
        else
            return null;
    }

    //public Door GetDoor() => door;

    //public void SetDoor(Door door) => this.door = door;
}
