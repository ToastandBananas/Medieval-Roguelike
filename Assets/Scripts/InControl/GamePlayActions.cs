using InControl;

public class GamePlayActions : PlayerActionSet
{
    public InControl.PlayerAction select, context;

    // Mouse Buttons
    public InControl.PlayerAction leftMouseClick, rightMouseClick, mouseScrollWheelClick;

    // Move Camera
    public InControl.PlayerAction moveUp, moveDown, moveLeft, moveRight;
    public PlayerTwoAxisAction movementAxis;

    // Rotate Camera
    public InControl.PlayerAction cameraRotateLeft, cameraRotateRight;
    public PlayerOneAxisAction cameraRotateAxis;

    // Zoom Camera
    public InControl.PlayerAction cameraZoomIn, cameraZoomOut;

    // UI Actions
    public InControl.PlayerAction menuPause, menuSelect;
    public InControl.PlayerAction menuLeft, menuRight, menuUp, menuDown;

    public GamePlayActions()
    {
        select = CreatePlayerAction("Select");
        context = CreatePlayerAction("Context");

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
