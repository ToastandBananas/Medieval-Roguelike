using UnityEngine;
using TMPro;
using GridSystem;

public class PathfindingGridDebugObject : GridDebugObject
{
    [SerializeField] TextMeshPro gCostText;
    [SerializeField] TextMeshPro hCostText;
    [SerializeField] TextMeshPro fCostText;

    PathNode pathNode;

    protected override void Update()
    {
        base.Update();

        if (pathNode.GCost() == int.MaxValue)
            gCostText.text = "g: N/A";
        else
            gCostText.text = "g: " + pathNode.GCost().ToString();

        if (pathNode.HCost() == int.MaxValue)
            gCostText.text = "g: N/A";
        else
            hCostText.text = "h: " + pathNode.HCost().ToString();

        if (pathNode.FCost() == int.MaxValue)
            gCostText.text = "g: N/A";
        else
            fCostText.text = "f: " + pathNode.FCost().ToString();
    }

    public override void SetGridObject(object gridObject)
    {
        base.SetGridObject(gridObject);

        pathNode = (PathNode)gridObject;
    }
}
