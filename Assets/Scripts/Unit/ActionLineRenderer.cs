using Pathfinding;
using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;

public class ActionLineRenderer : MonoBehaviour
{
    public static ActionLineRenderer Instance { get; private set; }

    [SerializeField] LineRenderer mainLineRenderer;
    [SerializeField] LineRenderer arrowHeadLineRenderer;

    Vector3 lineRendererOffset = new Vector3(0f, 0.1f, 0f);
    GridPosition currentMouseGridPosition;

    void Awake()
    {
        if (Instance != null)
        {
            Debug.LogError("There's more than one ActionLineRenderer! " + transform + " - " + Instance);
            Destroy(gameObject);
            return;
        }
        Instance = this;

        HideLineRenderers();
    }

    public IEnumerator DrawMovePath()
    {
        mainLineRenderer.enabled = true;

        GridPosition mouseGridPosition = LevelGrid.Instance.GetGridPosition(WorldMouse.GetPosition());
        if (mouseGridPosition != null && mouseGridPosition != currentMouseGridPosition)
        {
            currentMouseGridPosition = mouseGridPosition;

            ABPath path = ABPath.Construct(LevelGrid.Instance.GetWorldPosition(UnitActionSystem.Instance.SelectedUnit().GridPosition()), LevelGrid.Instance.GetWorldPosition(mouseGridPosition));
            path.traversalProvider = LevelGrid.Instance.DefaultTraversalProvider();

            // Schedule the path for calculation
            UnitActionSystem.Instance.SelectedUnit().GetAction<MoveAction>().Seeker().StartPath(path);

            // Wait for the path calculation to complete
            yield return StartCoroutine(path.WaitForPath());

            ResetLineRenderers();

            if (path.error || path == null)
                yield break;

            if (LevelGrid.Instance.IsValidGridPosition(currentMouseGridPosition) == false)
                yield break;

            // Don't draw a path if the mouse grid position is unwalkable
            Collider[] collisions = Physics.OverlapSphere(currentMouseGridPosition.WorldPosition() + new Vector3(0f, 0.025f, 0f), 0.01f, UnitActionSystem.Instance.ActionObstaclesMask());
            if (collisions.Length > 0)
                yield break;

            int verticeIndex = 0;
            for (int i = 0; i < path.vectorPath.Count - 1; i++)
            {
                mainLineRenderer.positionCount++;

                if (i == 0)
                {
                    if (path.vectorPath[1].y + lineRendererOffset.y - path.vectorPath[0].y > 0.02f)
                    {
                        // If the second point on the path is ABOVE the starting position, draw a line straight up/down before drawing a horizontal line to the next point
                        mainLineRenderer.SetPosition(0, path.vectorPath[0] + lineRendererOffset);
                        verticeIndex++;
                        mainLineRenderer.positionCount++;
                        mainLineRenderer.SetPosition(1, mainLineRenderer.GetPosition(verticeIndex - 1) + new Vector3(0f, path.vectorPath[i + 1].y - mainLineRenderer.GetPosition(verticeIndex - 1).y, 0f) + lineRendererOffset);
                        verticeIndex++;
                        mainLineRenderer.positionCount++;
                        mainLineRenderer.SetPosition(2, path.vectorPath[1] + lineRendererOffset);
                    }
                    else if (path.vectorPath[1].y + lineRendererOffset.y - path.vectorPath[0].y < 0.02f)
                    {
                        // If the second point on the path is BELOW the starting position, draw a line straight up/down before drawing a horizontal line to the next point
                        mainLineRenderer.SetPosition(0, path.vectorPath[0] + lineRendererOffset);
                        verticeIndex++;
                        mainLineRenderer.positionCount++;
                        mainLineRenderer.SetPosition(1, mainLineRenderer.GetPosition(verticeIndex - 1) + new Vector3(path.vectorPath[i + 1].x - mainLineRenderer.GetPosition(verticeIndex - 1).x, 0f, path.vectorPath[i + 1].z - mainLineRenderer.GetPosition(verticeIndex - 1).z));
                        verticeIndex++;
                        mainLineRenderer.positionCount++;
                        mainLineRenderer.SetPosition(2, path.vectorPath[1] + lineRendererOffset);
                    }
                    else
                    {
                        mainLineRenderer.SetPosition(0, path.vectorPath[0] + lineRendererOffset);
                        verticeIndex++;
                        mainLineRenderer.positionCount++;
                        mainLineRenderer.SetPosition(1, path.vectorPath[1] + lineRendererOffset);
                    }
                }
                else if (path.vectorPath[i + 1].y + lineRendererOffset.y - mainLineRenderer.GetPosition(verticeIndex - 1).y > 0.02f)
                {
                    // If the next point on the path is ABOVE the last vertex position assigned to the line renderer, draw a line straight up/down before drawing a horizontal line to the next point
                    mainLineRenderer.SetPosition(verticeIndex, mainLineRenderer.GetPosition(verticeIndex - 1) + new Vector3(0f, path.vectorPath[i + 1].y - mainLineRenderer.GetPosition(verticeIndex - 1).y, 0f) + lineRendererOffset);
                    verticeIndex++;
                    mainLineRenderer.positionCount++;
                    mainLineRenderer.SetPosition(verticeIndex, path.vectorPath[i + 1] + lineRendererOffset);
                }
                else if (path.vectorPath[i + 1].y + lineRendererOffset.y - mainLineRenderer.GetPosition(verticeIndex - 1).y < 0.02f)
                {
                    // If the next point on the path is BELOW the last vertex position assigned to the line renderer, draw a line horizontally before drawing a vertical line to the next point
                    mainLineRenderer.SetPosition(verticeIndex, mainLineRenderer.GetPosition(verticeIndex - 1) + new Vector3(path.vectorPath[i + 1].x - mainLineRenderer.GetPosition(verticeIndex - 1).x, 0f, path.vectorPath[i + 1].z - mainLineRenderer.GetPosition(verticeIndex - 1).z));
                    verticeIndex++;
                    mainLineRenderer.positionCount++;
                    mainLineRenderer.SetPosition(verticeIndex, path.vectorPath[i + 1] + lineRendererOffset);
                }
                else // Otherwise, simply draw a line to the next point
                    mainLineRenderer.SetPosition(verticeIndex, path.vectorPath[i + 1] + lineRendererOffset);

                verticeIndex++;
            }
        }
    }

    public void DrawTurnArrow(Vector3 targetPosition)
    {
        if (targetPosition == Vector3.zero)
        {
            HideLineRenderers();
            return;
        }

        ResetLineRenderers();
        mainLineRenderer.enabled = true;
        mainLineRenderer.positionCount = 2;

        mainLineRenderer.SetPosition(0, UnitActionSystem.Instance.SelectedUnit().WorldPosition() + lineRendererOffset);
        mainLineRenderer.SetPosition(1, targetPosition + lineRendererOffset);

        float finalTargetPositionY = targetPosition.y + lineRendererOffset.y;
        Direction turnDirection = UnitActionSystem.Instance.SelectedUnit().GetAction<TurnAction>().DetermineTurnDirection();
        arrowHeadLineRenderer.enabled = true;
        arrowHeadLineRenderer.positionCount = 3;

        switch (turnDirection)
        {
            case Direction.North:
                arrowHeadLineRenderer.SetPosition(0, new Vector3(targetPosition.x + -0.2f, finalTargetPositionY, targetPosition.z + -0.2f));
                arrowHeadLineRenderer.SetPosition(1, new Vector3(targetPosition.x, finalTargetPositionY, targetPosition.z));
                arrowHeadLineRenderer.SetPosition(2, new Vector3(targetPosition.x + 0.2f, finalTargetPositionY, targetPosition.z + -0.2f));
                break;
            case Direction.East:
                arrowHeadLineRenderer.SetPosition(0, new Vector3(targetPosition.x + -0.2f, finalTargetPositionY, targetPosition.z + 0.2f));
                arrowHeadLineRenderer.SetPosition(1, new Vector3(targetPosition.x, finalTargetPositionY, targetPosition.z));
                arrowHeadLineRenderer.SetPosition(2, new Vector3(targetPosition.x + -0.2f, finalTargetPositionY, targetPosition.z + -0.2f));
                break;
            case Direction.South:
                arrowHeadLineRenderer.SetPosition(0, new Vector3(targetPosition.x + 0.2f, finalTargetPositionY, targetPosition.z + 0.2f));
                arrowHeadLineRenderer.SetPosition(1, new Vector3(targetPosition.x, finalTargetPositionY, targetPosition.z));
                arrowHeadLineRenderer.SetPosition(2, new Vector3(targetPosition.x + -0.2f, finalTargetPositionY, targetPosition.z + 0.2f));
                break;
            case Direction.West:
                arrowHeadLineRenderer.SetPosition(0, new Vector3(targetPosition.x + 0.2f, finalTargetPositionY, targetPosition.z + 0.2f));
                arrowHeadLineRenderer.SetPosition(1, new Vector3(targetPosition.x, finalTargetPositionY, targetPosition.z));
                arrowHeadLineRenderer.SetPosition(2, new Vector3(targetPosition.x + 0.2f, finalTargetPositionY, targetPosition.z + -0.2f));
                break;
            case Direction.NorthWest:
                arrowHeadLineRenderer.SetPosition(0, new Vector3(targetPosition.x + 0.3f, finalTargetPositionY, targetPosition.z + -0.05f));
                arrowHeadLineRenderer.SetPosition(1, new Vector3(targetPosition.x, finalTargetPositionY, targetPosition.z));
                arrowHeadLineRenderer.SetPosition(2, new Vector3(targetPosition.x + 0.05f, finalTargetPositionY, targetPosition.z + -0.3f));
                break;
            case Direction.NorthEast:
                arrowHeadLineRenderer.SetPosition(0, new Vector3(targetPosition.x + -0.05f, finalTargetPositionY, targetPosition.z + -0.3f));
                arrowHeadLineRenderer.SetPosition(1, new Vector3(targetPosition.x, finalTargetPositionY, targetPosition.z));
                arrowHeadLineRenderer.SetPosition(2, new Vector3(targetPosition.x + -0.3f, finalTargetPositionY, targetPosition.z + -0.05f));
                break;
            case Direction.SouthWest:
                arrowHeadLineRenderer.SetPosition(0, new Vector3(targetPosition.x + 0.05f, finalTargetPositionY, targetPosition.z + 0.3f));
                arrowHeadLineRenderer.SetPosition(1, new Vector3(targetPosition.x, finalTargetPositionY, targetPosition.z));
                arrowHeadLineRenderer.SetPosition(2, new Vector3(targetPosition.x + 0.3f, finalTargetPositionY, targetPosition.z + 0.05f));
                break;
            case Direction.SouthEast:
                arrowHeadLineRenderer.SetPosition(0, new Vector3(targetPosition.x + -0.3f, finalTargetPositionY, targetPosition.z + 0.05f));
                arrowHeadLineRenderer.SetPosition(1, new Vector3(targetPosition.x, finalTargetPositionY, targetPosition.z));
                arrowHeadLineRenderer.SetPosition(2, new Vector3(targetPosition.x + -0.05f, finalTargetPositionY, targetPosition.z + 0.3f));
                break;
        }
    }

    public IEnumerator DelayHideLineRenderer()
    {
        yield return new WaitForSeconds(0.1f);
        HideLineRenderers();
    }

    public void HideLineRenderers()
    {
        mainLineRenderer.enabled = false;
        arrowHeadLineRenderer.enabled = false;
    }

    void ResetLineRenderers()
    {
        mainLineRenderer.positionCount = 0;
        arrowHeadLineRenderer.positionCount = 0;
    }
}
