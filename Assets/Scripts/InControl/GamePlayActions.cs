﻿using InControl;

public class GamePlayActions : PlayerActionSet
{
    public PlayerAction select, turnMode, skipTurn, cancelAction, swapWeapons;

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
    public PlayerAction menuPause, menuSelect, menuContext, menuQuickUse;
    public PlayerAction menuLeft, menuRight, menuUp, menuDown;
    public PlayerAction toggleInventory, splitStackEnter, splitStackDelete;

    public GamePlayActions()
    {
        select = CreatePlayerAction("Select");
        turnMode = CreatePlayerAction("TurnMode");
        skipTurn = CreatePlayerAction("SkipTurn");
        cancelAction = CreatePlayerAction("CancelAction");
        swapWeapons = CreatePlayerAction("SwapWeapons");

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
        menuContext = CreatePlayerAction("MenuContext");
        menuQuickUse = CreatePlayerAction("MenuQuickUse");

        menuLeft = CreatePlayerAction("MenuLeft");
        menuRight = CreatePlayerAction("MenuRight");
        menuUp = CreatePlayerAction("MenuUp");
        menuDown = CreatePlayerAction("MenuDown");

        toggleInventory = CreatePlayerAction("ToggleInventory");
        splitStackEnter = CreatePlayerAction("SplitStackEnter");
        splitStackDelete = CreatePlayerAction("SplitStackDelete");
    }
}
