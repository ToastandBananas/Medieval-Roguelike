using Pathfinding;
using System.Collections;
using UnityEngine;

namespace GridSystem
{
    public class ActionLineRenderer : MonoBehaviour
    {
        public static ActionLineRenderer Instance { get; private set; }

        [SerializeField] LineRenderer mainLineRenderer;
        [SerializeField] LineRenderer arrowHeadLineRenderer;

        static Unit player;

        Vector3 lineRendererOffset = new Vector3(0f, 0.1f, 0f);
        static GridPosition currentMouseGridPosition;
        static GridPosition currentInteractableGridPosition;
        static GridPosition currentUnitGridPosition;
        static GridPosition currentPlayerPosition;

        static readonly GridPosition defaultGridPosition = new GridPosition(100000, 100000, 100000);

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
            player = UnitManager.player;
        }

        public IEnumerator DrawMovePath()
        {
            mainLineRenderer.enabled = true;
            GridPosition targetGridPosition;

            if ((PlayerInput.Instance.highlightedInteractable != null && PlayerInput.Instance.highlightedInteractable.GridPosition() != currentInteractableGridPosition)
                || (PlayerInput.Instance.highlightedUnit != null && PlayerInput.Instance.highlightedUnit.GridPosition() != currentUnitGridPosition)
                || WorldMouse.GetCurrentGridPosition() != currentMouseGridPosition || player.GridPosition() != currentPlayerPosition)
            {
                currentMouseGridPosition = WorldMouse.GetCurrentGridPosition();
                currentPlayerPosition = player.GridPosition();

                if (PlayerInput.Instance.highlightedUnit == player || currentMouseGridPosition == currentPlayerPosition)
                {
                    HideLineRenderers();
                    yield break;
                }

                if (PlayerInput.Instance.highlightedInteractable != null)
                {
                    currentInteractableGridPosition = PlayerInput.Instance.highlightedInteractable.GridPosition();
                    if (TacticsPathfindingUtilities.CalculateWorldSpaceDistance_XYZ(player.GridPosition(), PlayerInput.Instance.highlightedInteractable.GridPosition()) > LevelGrid.diaganolDistance)
                        targetGridPosition = LevelGrid.Instance.GetNearestSurroundingGridPosition(PlayerInput.Instance.highlightedInteractable.GridPosition(), player.GridPosition(), LevelGrid.diaganolDistance, PlayerInput.Instance.highlightedInteractable.CanInteractAtMyGridPosition());
                    else
                    {
                        HideLineRenderers();
                        yield break;
                    }
                }
                else if (PlayerInput.Instance.highlightedUnit != null && player.vision.IsVisible(PlayerInput.Instance.highlightedUnit))
                {
                    BaseAction selectedAction = player.unitActionHandler.selectedActionType.GetAction(player);
                    currentUnitGridPosition = PlayerInput.Instance.highlightedUnit.GridPosition();

                    if (PlayerInput.Instance.highlightedUnit.health.IsDead() == false && (player.alliance.IsEnemy(PlayerInput.Instance.highlightedUnit) || selectedAction.IsDefaultAttackAction()))
                    {
                        // If the enemy Unit is in attack range or if they're out of range and the player has a non-default attack action selected, no need to show the line renderer
                        if (player.unitActionHandler.IsInAttackRange(PlayerInput.Instance.highlightedUnit, true) || (selectedAction.IsDefaultAttackAction() == false && selectedAction is MoveAction == false))
                        {
                            HideLineRenderers();
                            yield break;
                        }

                        targetGridPosition = GetTargetAttackGridPosition(PlayerInput.Instance.highlightedUnit);
                    }
                    else if (PlayerInput.Instance.highlightedUnit.health.IsDead())
                    {
                        if (TacticsPathfindingUtilities.CalculateWorldSpaceDistance_XYZ(PlayerInput.Instance.highlightedUnit.GridPosition(), player.GridPosition()) > LevelGrid.diaganolDistance)
                            targetGridPosition = LevelGrid.Instance.GetNearestSurroundingGridPosition(PlayerInput.Instance.highlightedUnit.GridPosition(), player.GridPosition(), LevelGrid.diaganolDistance, false);
                        else
                        {
                            HideLineRenderers();
                            yield break;
                        }
                    }
                    else
                    {
                        HideLineRenderers();
                        yield break;
                    }
                }
                else
                {
                    Unit unitAtGridPosition = LevelGrid.Instance.GetUnitAtGridPosition(currentMouseGridPosition);
                    BaseAction selectedAction = player.unitActionHandler.selectedActionType.GetAction(player);

                    if (unitAtGridPosition != null && player.vision.IsVisible(unitAtGridPosition) && (player.alliance.IsEnemy(unitAtGridPosition) || selectedAction.IsDefaultAttackAction()))
                    {
                        // If the enemy Unit is in attack range or if they're out of range and the player has a non-default attack action selected, no need to show the line renderer
                        if (player.unitActionHandler.IsInAttackRange(unitAtGridPosition, true) || (selectedAction.IsDefaultAttackAction() == false && selectedAction is MoveAction == false))
                        {
                            HideLineRenderers();
                            yield break;
                        }

                        targetGridPosition = GetTargetAttackGridPosition(unitAtGridPosition);
                    }
                    else
                    {
                        if (PlayerInput.Instance.highlightedUnit != null) // If the unit at the mouse position isn't visible, unblock their position so we can draw a line to it (we will re-block it after the line is drawn)
                            PlayerInput.Instance.highlightedUnit.UnblockCurrentPosition();

                        targetGridPosition = currentMouseGridPosition;

                        currentInteractableGridPosition = defaultGridPosition;
                        currentUnitGridPosition = defaultGridPosition;
                    }
                }

                player.UnblockCurrentPosition();

                ABPath path = ABPath.Construct(LevelGrid.GetWorldPosition(player.GridPosition()), LevelGrid.GetWorldPosition(targetGridPosition));
                path.traversalProvider = LevelGrid.Instance.DefaultTraversalProvider();

                // Schedule the path for calculation
                player.seeker.StartPath(path);

                // Wait for the path calculation to complete
                yield return StartCoroutine(path.WaitForPath());

                player.BlockCurrentPosition();

                ResetLineRenderers();

                if (path.error || path == null)
                    yield break;

                if (LevelGrid.IsValidGridPosition(targetGridPosition) == false || AstarPath.active.GetNearest(targetGridPosition.WorldPosition()).node.Walkable == false)
                    yield break;

                // Re-block the unit's position, in case it was unblocked
                if (PlayerInput.Instance.highlightedUnit != null && PlayerInput.Instance.highlightedUnit.health.IsDead() == false && player.vision.IsVisible(PlayerInput.Instance.highlightedUnit) == false)
                    PlayerInput.Instance.highlightedUnit.BlockCurrentPosition();

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

        GridPosition GetTargetAttackGridPosition(Unit targetUnit)
        {
            if (player.CharacterEquipment.RangedWeaponEquipped())
            {
                if (player.CharacterEquipment.HasValidAmmunitionEquipped() && player.unitActionHandler.SelectedAction is MeleeAction == false)
                    return player.unitActionHandler.GetAction<ShootAction>().GetNearestAttackPosition(player.GridPosition(), targetUnit);
                else
                    return player.unitActionHandler.GetAction<MeleeAction>().GetNearestAttackPosition(player.GridPosition(), targetUnit);
            }
            else
                return player.unitActionHandler.GetAction<MeleeAction>().GetNearestAttackPosition(player.GridPosition(), targetUnit);
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

            mainLineRenderer.SetPosition(0, player.WorldPosition + lineRendererOffset);
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

        public static void ResetCurrentPositions()
        {
            currentMouseGridPosition = defaultGridPosition;
            currentPlayerPosition = defaultGridPosition;
            currentInteractableGridPosition = defaultGridPosition;
            currentUnitGridPosition = defaultGridPosition;
        }
    }
}