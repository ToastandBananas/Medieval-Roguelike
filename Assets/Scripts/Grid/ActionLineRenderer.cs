using Pathfinding;
using System.Collections;
using UnityEngine;
using UnitSystem.ActionSystem;
using GeneralUI;
using UnitSystem;
using Utilities;
using System.Net;
using InventorySystem;
using UnitSystem.ActionSystem.Actions;

namespace GridSystem
{
    public class ActionLineRenderer : MonoBehaviour
    {
        public static ActionLineRenderer Instance { get; private set; }

        [SerializeField] LineRenderer mainLineRenderer;
        [SerializeField] LineRenderer arrowHeadLineRenderer;

        static Unit player;
        static Unit targetUnit;

        Vector3 lineRendererOffset = new(0f, 0.1f, 0f);
        static GridPosition currentMouseGridPosition;
        static GridPosition currentInteractableGridPosition;
        static GridPosition currentUnitGridPosition;
        static GridPosition currentPlayerPosition;

        static readonly GridPosition defaultGridPosition = new(100000, 100000, 100000);

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

        public void DrawParabola(Vector3 startPoint, Vector3 endPoint)
        {
            ResetLineRenderers();

            float arcHeight = MathParabola.CalculateParabolaArcHeight(player.GridPosition, LevelGrid.GetGridPosition(endPoint), GetArcMultiplier());
            float distance = Vector3.Distance(startPoint, endPoint);
            int resolution = Mathf.RoundToInt(distance * 5);
            if (resolution < 20)
                resolution = 20;

            Vector3[] points = new Vector3[resolution + 1];
            for (int i = 0; i <= resolution; i++)
            {
                float t = (float)i / resolution;
                points[i] = MathParabola.SampleParabola(startPoint, endPoint, arcHeight, t);
            }

            mainLineRenderer.enabled = true;
            mainLineRenderer.positionCount = points.Length;
            mainLineRenderer.SetPositions(points);
        }

        float GetArcMultiplier()
        {
            float arcMultiplier = 1f;
            if (player.UnitActionHandler.PlayerActionHandler.SelectedAction is Action_Throw)
                arcMultiplier = Projectile.defaultThrowArcMultiplier;
            else if (player.UnitEquipment.RangedWeaponEquipped && player.UnitEquipment.HumanoidEquipment.HasValidAmmunitionEquipped())
            {
                HeldRangedWeapon heldRangedWeapon = player.UnitMeshManager.GetHeldRangedWeapon();
                if (heldRangedWeapon.LoadedProjectile != null)
                    arcMultiplier = heldRangedWeapon.LoadedProjectile.ItemData.Item.Ammunition.ArcMultiplier;
                else
                    arcMultiplier = player.UnitEquipment.HumanoidEquipment.GetEquippedProjectile(heldRangedWeapon.ItemData.Item.RangedWeapon.ProjectileType).Item.Ammunition.ArcMultiplier;
            }
            return arcMultiplier;
        }

        public IEnumerator DrawMovePath()
        {
            mainLineRenderer.enabled = true;
            GridPosition targetGridPosition = player.GridPosition;

            if ((PlayerInput.Instance.HighlightedInteractable != null && PlayerInput.Instance.HighlightedInteractable.GridPosition() != currentInteractableGridPosition)
                || (PlayerInput.Instance.HighlightedUnit != null && PlayerInput.Instance.HighlightedUnit.GridPosition != currentUnitGridPosition)
                || WorldMouse.CurrentGridPosition() != currentMouseGridPosition || player.GridPosition != currentPlayerPosition)
            {
                currentMouseGridPosition = WorldMouse.CurrentGridPosition();
                currentPlayerPosition = player.GridPosition;

                if (PlayerInput.Instance.HighlightedUnit == player || currentMouseGridPosition == currentPlayerPosition)
                {
                    currentMouseGridPosition = player.GridPosition;
                    ResetLineRenderers();
                    HideLineRenderers();
                    yield break;
                }

                // First, setup the targetGridPosition
                if (PlayerInput.Instance.HighlightedInteractable != null)
                {
                    targetUnit = null;
                    currentInteractableGridPosition = PlayerInput.Instance.HighlightedInteractable.GridPosition();
                    if (Vector3.Distance(player.WorldPosition, PlayerInput.Instance.HighlightedInteractable.GridPosition().WorldPosition) > LevelGrid.diaganolDistance)
                        targetGridPosition = LevelGrid.GetNearestSurroundingGridPosition(PlayerInput.Instance.HighlightedInteractable.GridPosition(), player.GridPosition, LevelGrid.diaganolDistance, PlayerInput.Instance.HighlightedInteractable.CanInteractAtMyGridPosition());
                    else
                    {
                        HideLineRenderers();
                        yield break;
                    }
                }
                else if (PlayerInput.Instance.HighlightedUnit != null)
                {
                    Action_Base selectedAction = player.UnitActionHandler.PlayerActionHandler.SelectedActionType.GetAction(player);
                    targetUnit = PlayerInput.Instance.HighlightedUnit;
                    currentUnitGridPosition = targetUnit.GridPosition;

                    if (!targetUnit.HealthSystem.IsDead && (player.Alliance.IsEnemy(targetUnit) || selectedAction.IsDefaultAttackAction))
                    {
                        if (player.Vision.IsVisible(targetUnit))
                        {
                            // If the enemy Unit is in attack range or if they're out of range and the player has a non-default attack action selected, no need to show the line renderer
                            if (selectedAction is Action_BaseAttack
                                || (selectedAction is Action_Move && player.UnitActionHandler.IsInAttackRange(targetUnit, true))
                                || (selectedAction is Action_BaseAttack == false && selectedAction is Action_Move == false))
                            {
                                if (player.UnitActionHandler.PlayerActionHandler.SelectedAction is Action_BaseAttack && player.UnitActionHandler.PlayerActionHandler.SelectedAction.BaseAttackAction.IsInAttackRange(null, player.GridPosition, currentMouseGridPosition))
                                {
                                    DrawParabola(player.WorldPosition + (player.ShoulderHeight * Vector3.up) + (0.33f * player.transform.forward), currentMouseGridPosition.WorldPosition);
                                    yield break;
                                }
                                else if (PlayerInput.Instance.HighlightedUnit != null)
                                {
                                    Action_Shoot shootAction = player.UnitActionHandler.GetAction<Action_Shoot>();
                                    if (shootAction != null && shootAction.IsInAttackRange(PlayerInput.Instance.HighlightedUnit, player.GridPosition, PlayerInput.Instance.HighlightedUnit.GridPosition))
                                    {
                                        DrawParabola(player.WorldPosition + (player.ShoulderHeight * Vector3.up) + (0.33f * player.transform.forward), PlayerInput.Instance.HighlightedUnit.WorldPosition + (PlayerInput.Instance.HighlightedUnit.ShoulderHeight * Vector3.up));
                                        yield break;
                                    }
                                }

                                ResetLineRenderers();
                                HideLineRenderers();
                                yield break;
                            }

                            targetGridPosition = GetTargetAttackGridPosition(targetUnit);
                        }
                        else
                        {
                            targetUnit.UnblockCurrentPosition();
                            targetGridPosition = targetUnit.GridPosition;
                        }
                    }
                    else if (targetUnit.HealthSystem.IsDead)
                    {
                        if (Vector3.Distance(targetUnit.WorldPosition, player.WorldPosition) > LevelGrid.diaganolDistance)
                            targetGridPosition = LevelGrid.GetNearestSurroundingGridPosition(targetUnit.GridPosition, player.GridPosition, LevelGrid.diaganolDistance, false);
                        else
                        {
                            HideLineRenderers();
                            yield break;
                        }
                    }
                    else if (player.Vision.IsVisible(targetUnit) == false)
                    {
                        targetUnit.UnblockCurrentPosition();
                        targetGridPosition = targetUnit.GridPosition;
                    }
                    else
                    {
                        HideLineRenderers();
                        yield break;
                    }
                }
                else
                {
                    targetUnit = LevelGrid.GetUnitAtGridPosition(currentMouseGridPosition);
                    Action_Base selectedAction = player.UnitActionHandler.PlayerActionHandler.SelectedActionType.GetAction(player);

                    if (targetUnit != null && player.Vision.IsVisible(targetUnit))
                    {
                        if (player.Alliance.IsEnemy(targetUnit) || selectedAction.IsDefaultAttackAction)
                        {
                            // If the enemy Unit is in attack range or if they're out of range and the player has a non-default attack action selected, no need to show the line renderer
                            if (selectedAction is Action_BaseAttack 
                                || (selectedAction is Action_Move && player.UnitActionHandler.IsInAttackRange(targetUnit, true)) 
                                || (selectedAction is Action_BaseAttack == false && selectedAction is Action_Move == false))
                            {
                                if (player.UnitActionHandler.PlayerActionHandler.SelectedAction is Action_BaseAttack && player.UnitActionHandler.PlayerActionHandler.SelectedAction.BaseAttackAction.IsInAttackRange(null, player.GridPosition, currentMouseGridPosition))
                                {
                                    DrawParabola(player.WorldPosition + (player.ShoulderHeight * Vector3.up) + (0.33f * player.transform.forward), currentMouseGridPosition.WorldPosition);
                                    yield break;
                                }
                                else if (PlayerInput.Instance.HighlightedUnit != null)
                                {
                                    Action_Shoot shootAction = player.UnitActionHandler.GetAction<Action_Shoot>();
                                    if (shootAction != null && shootAction.IsInAttackRange(PlayerInput.Instance.HighlightedUnit, player.GridPosition, PlayerInput.Instance.HighlightedUnit.GridPosition))
                                    {
                                        DrawParabola(player.WorldPosition + (player.ShoulderHeight * Vector3.up) + (0.33f * player.transform.forward), PlayerInput.Instance.HighlightedUnit.WorldPosition + (PlayerInput.Instance.HighlightedUnit.ShoulderHeight * Vector3.up));
                                        yield break;
                                    }
                                }

                                ResetLineRenderers();
                                HideLineRenderers();
                                yield break;
                            }

                            targetGridPosition = GetTargetAttackGridPosition(targetUnit);
                        }
                    }
                    else
                    {
                        if (targetUnit != null) // If the unit at the mouse position isn't visible, unblock their position so we can draw a line to it (we will re-block it after the line is drawn)
                        {
                            targetUnit.UnblockCurrentPosition();
                            targetGridPosition = targetUnit.GridPosition;
                        }
                        else
                            targetGridPosition = currentMouseGridPosition;
                    }
                }

                player.UnblockCurrentPosition();

                ABPath path = ABPath.Construct(LevelGrid.GetWorldPosition(player.GridPosition), LevelGrid.GetWorldPosition(targetGridPosition));
                path.traversalProvider = LevelGrid.DefaultTraversalProvider;

                // Schedule the path for calculation
                player.Seeker.StartPath(path);

                // Wait for the path calculation to complete
                yield return StartCoroutine(path.WaitForPath());

                player.BlockCurrentPosition();

                ResetLineRenderers();

                if (path.error || path == null)
                    yield break;

                if (!LevelGrid.IsValidGridPosition(targetGridPosition) || !AstarPath.active.GetNearest(targetGridPosition.WorldPosition).node.Walkable)
                    yield break;

                // Re-block the unit's position, in case it was unblocked
                if (targetUnit != null && !targetUnit.HealthSystem.IsDead)
                    targetUnit.BlockCurrentPosition();
                
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
            if (player.UnitEquipment.RangedWeaponEquipped)
            {
                if (player.UnitEquipment.HumanoidEquipment.HasValidAmmunitionEquipped() && player.UnitActionHandler.PlayerActionHandler.SelectedAction is Action_Melee == false)
                {
                    if (player.UnitActionHandler.PlayerActionHandler.SelectedAction is Action_BaseAttack)
                        return player.UnitActionHandler.PlayerActionHandler.SelectedAction.BaseAttackAction.GetNearestAttackPosition(player.GridPosition, targetUnit);
                    return player.UnitActionHandler.GetAction<Action_Shoot>().GetNearestAttackPosition(player.GridPosition, targetUnit);
                }

                return player.UnitActionHandler.GetAction<Action_Melee>().GetNearestAttackPosition(player.GridPosition, targetUnit);
            }
            
            if (player.UnitActionHandler.PlayerActionHandler.SelectedAction is Action_BaseAttack)
                return player.UnitActionHandler.PlayerActionHandler.SelectedAction.BaseAttackAction.GetNearestAttackPosition(player.GridPosition, targetUnit);

            return player.UnitActionHandler.GetAction<Action_Melee>().GetNearestAttackPosition(player.GridPosition, targetUnit);
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
            Direction turnDirection = player.UnitActionHandler.TurnAction.DetermineTargetTurnDirection(LevelGrid.GetGridPosition(WorldMouse.GetPosition()));
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

        public static void ResetCurrentPositions()
        {
            currentMouseGridPosition = defaultGridPosition;
            currentPlayerPosition = defaultGridPosition;
            currentInteractableGridPosition = defaultGridPosition;
            currentUnitGridPosition = defaultGridPosition;
        }
    }
}