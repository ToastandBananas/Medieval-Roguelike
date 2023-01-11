using System;
using UnityEngine;

public struct GridPosition : IEquatable<GridPosition>
{
    public int x;
    public float y;
    public int z;

    public GridPosition(int x, float y, int z)
    {
        this.x = x;
        this.y = y;
        this.z = z;
    }

    public GridPosition(Vector3 position)
    {
        x = Mathf.RoundToInt(position.x);
        y = Mathf.RoundToInt(position.y * 100f) / 100f;
        z = Mathf.RoundToInt(position.z);
    }

    public Vector3 WorldPosition() => LevelGrid.Instance.GetWorldPosition(this);

    public override string ToString()
    {
        return "(" + x + ", " + y + " ," + z + ")";
    }

    public static bool operator ==(GridPosition a, GridPosition b)
    {
        return a.x == b.x && a.y == b.y && a.z == b.z;
    }

    public static bool operator !=(GridPosition a, GridPosition b)
    {
        return !(a == b);
    }

    public static GridPosition operator +(GridPosition a, GridPosition b)
    {
        return new GridPosition(a.x + b.x, a.y + b.y, a.z + b.z);
    }

    public static GridPosition operator -(GridPosition a, GridPosition b)
    {
        return new GridPosition(a.x - b.x, a.y - b.y, a.z - b.z);
    }

    public override bool Equals(object obj)
    {
        return obj is GridPosition position &&
               x == position.x &&
               y == position.y &&
               z == position.z;
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(x, y, z);
    }

    public bool Equals(GridPosition other)
    {
        return this == other;
    }
}