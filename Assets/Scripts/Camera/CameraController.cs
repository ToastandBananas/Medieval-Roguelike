using UnityEngine;
using Cinemachine;
using System.Collections;
using Controls;
using UnitSystem;

namespace CameraSystem
{
    public class CameraController : MonoBehaviour
    {
        public static CameraController Instance { get; private set; }

        [SerializeField] CinemachineVirtualCamera cinemachineVirtualCamera;
        CinemachineTransposer cinemachineTransposer;

        Vector3 targetFollowOffset;
        Vector2Int screen;

        Vector3 dragCurrentPosition, dragStartPosition, newPosition;
        Vector3 mouseRotateStartPosition, mouseRotateCurrentPosition;
        Quaternion newRotation;

        bool animatingCameraMovement;
        bool animatingCameraZoom;
        bool doingEdgeMovement;
        bool doingMouseDragMovement;
        bool doingMouseDragRotation;

        public const float MIN_FOLLOW_Y_OFFSET = 2f;
        public const float MAX_FOLLOW_Y_OFFSET = 12f;
        const float MIN_FOLLOW_Z_OFFSET = -12f;
        const float MAX_FOLLOW_Z_OFFSET = -2f;

        void Awake()
        {
            if (Instance != null)
            {
                Debug.LogError("There's more than one CameraController! " + transform + " - " + Instance);
                Destroy(gameObject);
                return;
            }
            Instance = this;

            screen = new Vector2Int(Screen.width, Screen.height);
        }

        void Start()
        {
            newRotation = transform.rotation;

            cinemachineTransposer = cinemachineVirtualCamera.GetCinemachineComponent<CinemachineTransposer>();
            targetFollowOffset = cinemachineTransposer.m_FollowOffset;

            transform.position = UnitManager.player.transform.position;

            /*UnitActionSystem.Instance.OnSelectedUnitChanged += UnitActionSystem_OnSelectedUnitChanged;
            UnitActionSystem.Instance.OnActiveAIUnitChanged += UnitActionSystem_OnActiveAIUnitChanged;
            BaseAction.OnAnyActionStarted += BaseAction_OnAnyActionStarted;
            BaseAction.OnAnyActionCompleted += BaseAction_OnAnyActionCompleted;*/
        }

        void Update()
        {
            if (animatingCameraMovement == false && animatingCameraZoom == false)
            {
                //if (doingMouseDragRotation == false && doingMouseDragMovement == false)
                //HandleEdgeOfScreenMovement();

                if (doingEdgeMovement == false)
                {
                    HandleMouseDragMovement();

                    if (doingMouseDragMovement == false)
                        HandleCameraMovement();
                }

                HandleCameraZoom();
            }

            HandleMouseDragRotation();

            if (doingMouseDragRotation == false)
                HandleCameraRotation();
        }

        public IEnumerator FollowTarget(Transform target, bool zoomInOnTarget = false, float followSpeed = 5f, float targetZoom = 4f)
        {
            while (animatingCameraMovement)
            {
                // Wait for previous movement to finish before starting a new one
                yield return null;
            }

            animatingCameraMovement = true;

            if (zoomInOnTarget)
                StartCoroutine(ZoomIn(target, targetZoom));

            while (animatingCameraMovement)
            {
                Vector3 targetPosition = new Vector3(target.position.x, 0f, target.position.z);
                transform.position = Vector3.Lerp(new Vector3(transform.position.x, 0f, transform.position.z), targetPosition, Time.deltaTime * followSpeed);
                yield return null;
            }
        }

        public void StopFollowingTarget()
        {
            animatingCameraMovement = false;
        }

        IEnumerator MoveCameraToTarget(Transform target, bool zoomInOnTarget, float targetZoom = 4f)
        {
            while (animatingCameraMovement)
            {
                // Wait for previous movement to finish before starting a new one
                yield return null;
            }

            animatingCameraMovement = true;

            if (zoomInOnTarget)
                StartCoroutine(ZoomIn(target, targetZoom));

            float moveSpeed = 3f;
            Vector3 targetPosition = new Vector3(target.position.x, 0f, target.position.z);
            while (Vector3.Distance(transform.position, targetPosition) >= 0.1f)
            {
                transform.position = Vector3.Lerp(new Vector3(transform.position.x, 0f, transform.position.z), targetPosition, Time.deltaTime * moveSpeed);
                targetPosition = new Vector3(target.position.x, 0f, target.position.z);
                yield return null;
            }

            animatingCameraMovement = false;
        }

        IEnumerator ZoomIn(Transform target, float targetZoom)
        {
            while (animatingCameraZoom)
            {
                // Wait for previous zoom to finish before starting a new one
                yield return null;
            }

            targetFollowOffset.y = targetZoom + target.position.y;
            targetFollowOffset.z = -targetZoom - target.position.y;

            targetFollowOffset.y = Mathf.Clamp(targetFollowOffset.y, MIN_FOLLOW_Y_OFFSET, MAX_FOLLOW_Y_OFFSET);
            targetFollowOffset.z = Mathf.Clamp(targetFollowOffset.z, MIN_FOLLOW_Z_OFFSET, MAX_FOLLOW_Z_OFFSET);

            float zoomSpeed = 3.5f;
            while (Mathf.Abs(targetZoom - cinemachineTransposer.m_FollowOffset.y) > 0.05f)
            {
                animatingCameraZoom = true;
                cinemachineTransposer.m_FollowOffset = Vector3.Lerp(cinemachineTransposer.m_FollowOffset, targetFollowOffset, Time.deltaTime * zoomSpeed);

                yield return null;
            }

            targetFollowOffset = cinemachineTransposer.m_FollowOffset;
            animatingCameraZoom = false;
        }

        void HandleEdgeOfScreenMovement()
        {
            newPosition = transform.position;

            Vector3 mousePosition = Input.mousePosition;

            // Give a 5% buffer around the edges of the screen where anything beyond won't affect the camera
            bool mouseValid = (mousePosition.y <= screen.y * 1.05f && mousePosition.y >= screen.y * -0.05f
                            && mousePosition.x <= screen.x * 1.05f && mousePosition.x >= screen.x * -0.05f);

            if (mouseValid == false)
            {
                doingEdgeMovement = false;
                return;
            }

            Vector3 moveDir = Vector3.zero;

            // Mouse is at the top edge of the screen
            if (mousePosition.y > screen.y * 0.95f)
                moveDir.z = 1f;
            // Mouse is at the bottom edge of the screen
            else if (mousePosition.y < screen.y * 0.05f)
                moveDir.z = -1f;
            // Mouse is at the right edge of the screen
            if (mousePosition.x > screen.x * 0.95f)
                moveDir.x = 1f;
            // Mouse is at the left edge of the screen
            else if (mousePosition.x < screen.x * 0.05f)
                moveDir.x = -1f;

            if (moveDir == Vector3.zero)
                doingEdgeMovement = false;
            else
            {
                // Movement
                float moveSpeed = 10f;
                Vector3 moveVector = transform.forward * moveDir.z + transform.right * moveDir.x;
                transform.position += moveSpeed * Time.deltaTime * moveVector;
                doingEdgeMovement = true;
            }
        }

        void HandleMouseDragMovement()
        {
            newPosition = transform.position;
            if (GameControls.gamePlayActions.mouseScrollWheelClick.WasPressed)
            {
                Plane plane = new Plane(Vector3.up, Vector3.zero);

                Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);

                if (plane.Raycast(ray, out float entry))
                    dragStartPosition = ray.GetPoint(entry);
            }

            if (GameControls.gamePlayActions.mouseScrollWheelClick.IsPressed)
            {
                Plane plane = new Plane(Vector3.up, Vector3.zero);

                Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);

                if (plane.Raycast(ray, out float entry))
                {
                    dragCurrentPosition = ray.GetPoint(entry);
                    newPosition = transform.position + dragStartPosition - dragCurrentPosition;
                }
            }

            if (Vector3.Distance(transform.position, newPosition) < 0.1f)
                doingMouseDragMovement = false;
            else
                doingMouseDragMovement = true;

            if (doingMouseDragMovement)
            {
                float dragMovementSpeed = 8f;
                transform.position = Vector3.Lerp(transform.position, newPosition, Time.deltaTime * dragMovementSpeed);
            }
        }

        void HandleCameraMovement()
        {
            newPosition = transform.position;

            Vector3 inputMoveDir = Vector3.zero;
            if (GameControls.gamePlayActions.moveUp.IsPressed)
                inputMoveDir.z = 1f;
            if (GameControls.gamePlayActions.moveDown.IsPressed)
                inputMoveDir.z = -1f;
            if (GameControls.gamePlayActions.moveLeft.IsPressed)
                inputMoveDir.x = -1f;
            if (GameControls.gamePlayActions.moveRight.IsPressed)
                inputMoveDir.x = 1f;

            float moveSpeed = 10f;
            Vector3 moveVector = transform.forward * inputMoveDir.z + transform.right * inputMoveDir.x;
            transform.position += moveSpeed * Time.deltaTime * moveVector;
        }

        void HandleMouseDragRotation()
        {
            if (GameControls.gamePlayActions.rightMouseClick.WasPressed)
                mouseRotateStartPosition = Input.mousePosition;

            if (GameControls.gamePlayActions.rightMouseClick.IsPressed)
            {
                mouseRotateCurrentPosition = Input.mousePosition;

                Vector3 difference = mouseRotateStartPosition - mouseRotateCurrentPosition;
                mouseRotateStartPosition = mouseRotateCurrentPosition;

                newRotation *= Quaternion.Euler(Vector3.up * (-difference.x / 5f));
            }

            if (newRotation == transform.rotation)
                doingMouseDragRotation = false;
            else
                doingMouseDragRotation = true;

            if (doingMouseDragRotation)
            {
                float dragRotateSpeed = 6f;
                transform.rotation = Quaternion.Lerp(transform.rotation, newRotation, Time.deltaTime * dragRotateSpeed);
            }
        }

        void HandleCameraRotation()
        {
            newRotation = transform.rotation;

            Vector3 rotationVector = Vector3.zero;
            if (GameControls.gamePlayActions.cameraRotateLeft.IsPressed)
                rotationVector.y = 1f;
            if (GameControls.gamePlayActions.cameraRotateRight.IsPressed)
                rotationVector.y = -1f;

            float rotationSpeed = 100f;
            transform.eulerAngles += rotationVector * rotationSpeed * Time.deltaTime;
            newRotation = transform.rotation;
        }

        void HandleCameraZoom()
        {
            float zoomAmount = 1f;
            if (GameControls.gamePlayActions.cameraZoomIn.WasPressed)
            {
                targetFollowOffset.y -= zoomAmount;
                targetFollowOffset.z += zoomAmount;
            }
            else if (GameControls.gamePlayActions.cameraZoomOut.WasPressed)
            {
                targetFollowOffset.y += zoomAmount;
                targetFollowOffset.z -= zoomAmount;
            }

            targetFollowOffset.y = Mathf.Clamp(targetFollowOffset.y, MIN_FOLLOW_Y_OFFSET, MAX_FOLLOW_Y_OFFSET);
            targetFollowOffset.z = Mathf.Clamp(targetFollowOffset.z, MIN_FOLLOW_Z_OFFSET, MAX_FOLLOW_Z_OFFSET);

            float zoomSpeed = 5f;
            cinemachineTransposer.m_FollowOffset = Vector3.Lerp(cinemachineTransposer.m_FollowOffset, targetFollowOffset, Time.deltaTime * zoomSpeed);
        }

        public static float CurrentZoom => Instance.cinemachineTransposer.m_FollowOffset.y;

        //void UnitActionSystem_OnSelectedUnitChanged(object sender, EventArgs e) => StartCoroutine(MoveCameraToTarget(UnitActionSystem.Instance.SelectedUnit().transform, false));

        //void UnitActionSystem_OnActiveAIUnitChanged(object sender, EventArgs e) => StartCoroutine(MoveCameraToTarget(UnitActionSystem.Instance.ActiveAIUnit().transform, false));

        /*public void BaseAction_OnAnyActionStarted(object sender, EventArgs e)
        {
            Unit unitToFocusOn;
            if (TurnSystem.Instance.IsPlayerTurn())
                unitToFocusOn = UnitActionSystem.Instance.SelectedUnit();
            else
                unitToFocusOn = UnitActionSystem.Instance.ActiveAIUnit();

            if (unitToFocusOn == null)
                return;

            switch (sender)
            {
                case MoveAction moveAction:
                    StartCoroutine(FollowTarget(unitToFocusOn.transform));
                    break;
                case TurnAction spinAction:
                    StartCoroutine(MoveCameraToTarget(unitToFocusOn.transform, false));
                    break;
                case ShootAction shootAction:
                    StartCoroutine(MoveCameraToTarget(unitToFocusOn.transform, false));
                    break;
                case ReloadAction reloadAction:
                    StartCoroutine(MoveCameraToTarget(unitToFocusOn.transform, false));
                    break;
            }
        }

        public void BaseAction_OnAnyActionCompleted(object sender, EventArgs e)
        {
            switch (sender)
            {
                case MoveAction moveAction:
                    StopFollowingTarget();
                    break;
                default:
                    break;
            }
        }*/
    }
}
