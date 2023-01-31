using InControl;

public class GamePlayActions : PlayerActionSet
{
    public PlayerAction select, context, turnMode, skipTurn, cancelAction;

    // Mouse Buttons
    public PlayerAction leftMouseClick, rightMouseClick, mouseScrollWheelClick;

    // Move Camera
    public PlayerAction moveUp, moveDown, moveLeft, moveRight;
    public PlayerTwoAxisAction movementAxis;

    // Rotate Camera
    public PlayerAction cameraRotateLeft, cameraRotateRight;
    public PlayerOneAxisAction cameraRotateAxis;

    // Zoom Camera
    public PlayerAction cameraZoomIn, cameraZoomOut;

    // UI Actions
    public PlayerAction menuPause, menuSelect;
    public PlayerAction menuLeft, menuRight, menuUp, menuDown;

    public GamePlayActions()
    {
        select = CreatePlayerAction("Select");
        context = CreatePlayerAction("Context");
        turnMode = CreatePlayerAction("TurnMode");
        skipTurn = CreatePlayerAction("SkipTurn");
        cancelAction = CreatePlayerAction("CancelAction");

        leftMouseClick = CreatePlayerAction("LeftMouseClick");
        rightMouseClick = CreatePlayerAction("RightMouseClick");
        mouseScrollWheelClick = CreatePlayerAction("MouseScrollWheelClick");

        moveUp = CreatePlayerAction("MoveUp");
        moveDown = CreatePlayerAction("MoveDown");
        moveLeft = CreatePlayerAction("MoveLeft");
        moveRight = CreatePlayerAction("MoveRight");
        movementAxis = CreateTwoAxisPlayerAction(moveLeft, moveRight, moveDown, moveUp);

        cameraRotateLeft = CreatePlayerAction("PlayerLookLeft");
        cameraRotateRight = CreatePlayerAction("PlayerLookRight");
        cameraRotateAxis = CreateOneAxisPlayerAction(cameraRotateLeft, cameraRotateRight);

        cameraZoomIn = CreatePlayerAction("CameraZoomIn");
        cameraZoomOut = CreatePlayerAction("CameraZoomOut");

        // UI Actions
        menuPause = CreatePlayerAction("MenuPause");
        menuSelect = CreatePlayerAction("MenuSelect");

        menuLeft = CreatePlayerAction("MenuLeft");
        menuRight = CreatePlayerAction("MenuRight");
        menuUp = CreatePlayerAction("MenuUp");
        menuDown = CreatePlayerAction("MenuDown");
    }
}
