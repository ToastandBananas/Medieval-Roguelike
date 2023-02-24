using Pathfinding;
using System.Collections;
using UnityEngine;

public class ActionLineRenderer : MonoBehaviour
{
    public static ActionLineRenderer Instance { get; private set; }

    [SerializeField] LineRenderer mainLineRenderer;
    [SerializeField] LineRenderer arrowHeadLineRenderer;

    Unit player;

    Vector3 lineRendererOffset = new Vector3(0f, 0.1f, 0f);
    GridPosition currentMouseGridPosition;
    GridPosition currentPlayerPosition;

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

    void Start()
    {
        player = UnitManager.Instance.player;
    }

    public IEnumerator DrawMovePath()
    {
        mainLineRenderer.enabled = true;
        GridPosition mouseGridPosition = WorldMouse.currentGridPosition;
        GridPosition targetGridPosition = mouseGridPosition;

        if (PlayerInput.Instance.highlightedInteractable != null || mouseGridPosition != currentMouseGridPosition || player.gridPosition != currentPlayerPosition)
        {
            currentMouseGridPosition = mouseGridPosition;
            currentPlayerPosition = player.gridPosition;
            Unit unitAtMousePosition = LevelGrid.Instance.GetUnitAtGridPosition(mouseGridPosition);

            if (unitAtMousePosition == player || (unitAtMousePosition != null && unitAtMousePosition.health.IsDead()))
            {
                HideLineRenderers();
                yield break;
            }

            if (PlayerInput.Instance.highlightedInteractable != null)
            {
                if (TacticsPathfindingUtilities.CalculateWorldSpaceDistance_XYZ(player.gridPosition, PlayerInput.Instance.highlightedInteractable.gridPosition) > LevelGrid.Instance.GridSize())
                    targetGridPosition = LevelGrid.Instance.GetNearestSurroundingGridPosition(PlayerInput.Instance.highlightedInteractable.gridPosition, player.gridPosition, LevelGrid.Instance.GridSize());
                else
                {
                    HideLineRenderers();
                    yield break;
                }
            }
            else if (unitAtMousePosition != null && player.vision.IsVisible(unitAtMousePosition))
            {
                if (player.alliance.IsEnemy(unitAtMousePosition))
                {
                    // If the enemy Unit is in attack range, no need to show the line renderer
                    if (player.unitActionHandler.IsInAttackRange(unitAtMousePosition))
                    {
                        HideLineRenderers();
                        yield break;
                    }

                    if (player.RangedWeaponEquipped())
                        targetGridPosition = player.unitActionHandler.GetAction<ShootAction>().GetNearestShootPosition(player.gridPosition, unitAtMousePosition);
                    else
                        targetGridPosition = player.unitActionHandler.GetAction<MeleeAction>().GetNearestMeleePosition(player.gridPosition, unitAtMousePosition);
                }
                else
                    targetGridPosition = mouseGridPosition;
            }
            else
            {
                if (unitAtMousePosition != null)
                    unitAtMousePosition.UnblockCurrentPosition();
            }

            ABPath path = ABPath.Construct(LevelGrid.GetWorldPosition(player.gridPosition), LevelGrid.GetWorldPosition(targetGridPosition));
            path.traversalProvider = LevelGrid.Instance.DefaultTraversalProvider();

            // Schedule the path for calculation
            player.unitActionHandler.GetAction<MoveAction>().seeker.StartPath(path);

            // Wait for the path calculation to complete
            yield return StartCoroutine(path.WaitForPath());

            ResetLineRenderers();

            if (path.error || path == null)
                yield break;

            if (LevelGrid.IsValidGridPosition(currentMouseGridPosition) == false || AstarPath.active.GetNearest(currentMouseGridPosition.WorldPosition()).node.Walkable == false)
                yield break;

            if (unitAtMousePosition != null && player.vision.IsVisible(unitAtMousePosition) == false)
                unitAtMousePosition.BlockCurrentPosition();

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

        mainLineRenderer.SetPosition(0, player.WorldPosition() + lineRendererOffset);
        mainLineRenderer.SetPosition(1, targetPosition + lineRendererOffset);

        float finalTargetPositionY = targetPosition.y + lineRendererOffset.y;
        Direction turnDirection = player.unitActionHandler.GetAction<TurnAction>().DetermineTargetTurnDirection(LevelGrid.GetGridPosition(WorldMouse.GetPosition()));
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
        ResetCurrentPositions();
    }

    void ResetLineRenderers()
    {
        mainLineRenderer.positionCount = 0;
        arrowHeadLineRenderer.positionCount = 0;
    }

    public void ResetCurrentPositions() 
    {
        currentMouseGridPosition = new GridPosition(10000, 10000, 10000);
        currentPlayerPosition = new GridPosition(10001, 10001, 10001);
    }
}
