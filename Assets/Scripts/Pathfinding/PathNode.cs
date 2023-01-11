public class PathNode
{
    GridPosition gridPosition;
    PathNode cameFromPathNode;
    int gCost, hCost, fCost;
    bool isWalkable = true;

    public PathNode(GridPosition gridPosition) => this.gridPosition = gridPosition;

    public int GCost() => gCost;

    public int HCost() => hCost;

    public int FCost() => fCost;

    public void SetGCost(int gCost) => this.gCost = gCost;

    public void SetHCost(int hCost) => this.hCost = hCost;

    public void CalculateFCost() => fCost = gCost + hCost;

    public void ResetCameFromPathNode() => cameFromPathNode = null;

    public PathNode GetCameFromPathNode() => cameFromPathNode;

    public void SetCameFromPathNode(PathNode pathNode) => cameFromPathNode = pathNode;

    public GridPosition GridPosition() => gridPosition;

    public bool IsWalkable() => isWalkable;

    public bool SetIsWalkable(bool isWalkable) => this.isWalkable = isWalkable;

    public override string ToString() => gridPosition.ToString();
}
