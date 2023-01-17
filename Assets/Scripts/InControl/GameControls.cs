﻿using UnityEngine;
using InControl;

public class GameControls : MonoBehaviour
{
    public static GamePlayActions gamePlayActions;

    void Start()
    {
        gamePlayActions = new GamePlayActions();
        BindDefaultControls();
    }

    void BindDefaultControls()
    {
        gamePlayActions.select.AddDefaultBinding(Mouse.LeftButton);
        gamePlayActions.select.AddDefaultBinding(InputControlType.Action1); // X(PS4) / A(Xbox)

        gamePlayActions.context.AddDefaultBinding(Mouse.RightButton);
        gamePlayActions.context.AddDefaultBinding(InputControlType.Action3); // Square(PS4) / X(Xbox)

        gamePlayActions.turnMode.AddDefaultBinding(Key.LeftShift);
        gamePlayActions.turnMode.AddDefaultBinding(InputControlType.LeftBumper);

        gamePlayActions.skipTurn.AddDefaultBinding(Key.Space);
        gamePlayActions.skipTurn.AddDefaultBinding(InputControlType.Action2); // Cirle(PS4) / B(Xbox)

        // Mouse Buttons
        gamePlayActions.leftMouseClick.AddDefaultBinding(Mouse.LeftButton);
        gamePlayActions.rightMouseClick.AddDefaultBinding(Mouse.RightButton);
        gamePlayActions.mouseScrollWheelClick.AddDefaultBinding(Mouse.MiddleButton);

        // Move Camera
        gamePlayActions.moveUp.AddDefaultBinding(Key.W);
        gamePlayActions.moveUp.AddDefaultBinding(InputControlType.LeftStickUp);
        gamePlayActions.moveDown.AddDefaultBinding(Key.S);
        gamePlayActions.moveDown.AddDefaultBinding(InputControlType.LeftStickDown);
        gamePlayActions.moveLeft.AddDefaultBinding(Key.A);
        gamePlayActions.moveLeft.AddDefaultBinding(InputControlType.LeftStickLeft);
        gamePlayActions.moveRight.AddDefaultBinding(Key.D);
        gamePlayActions.moveRight.AddDefaultBinding(InputControlType.LeftStickRight);

        // Rotate Camera
        gamePlayActions.cameraRotateLeft.AddDefaultBinding(Key.Q);
        gamePlayActions.cameraRotateLeft.AddDefaultBinding(InputControlType.RightStickLeft);
        gamePlayActions.cameraRotateRight.AddDefaultBinding(Key.E);
        gamePlayActions.cameraRotateRight.AddDefaultBinding(InputControlType.RightStickRight);

        // Zoom Camera
        gamePlayActions.cameraZoomIn.AddDefaultBinding(Mouse.PositiveScrollWheel);
        gamePlayActions.cameraZoomOut.AddDefaultBinding(Mouse.NegativeScrollWheel);

        // UI Actions
        gamePlayActions.menuPause.AddDefaultBinding(Key.Escape);
        gamePlayActions.menuPause.AddDefaultBinding(InputControlType.RightCommand);

        gamePlayActions.menuSelect.AddDefaultBinding(Mouse.LeftButton);
        gamePlayActions.menuSelect.AddDefaultBinding(InputControlType.Action1);

        gamePlayActions.menuUp.AddDefaultBinding(Key.UpArrow);
        gamePlayActions.menuUp.AddDefaultBinding(InputControlType.DPadUp);
        gamePlayActions.menuUp.AddDefaultBinding(InputControlType.LeftStickUp);

        gamePlayActions.menuDown.AddDefaultBinding(Key.DownArrow);
        gamePlayActions.menuDown.AddDefaultBinding(InputControlType.DPadDown);
        gamePlayActions.menuUp.AddDefaultBinding(InputControlType.LeftStickDown);

        gamePlayActions.menuLeft.AddDefaultBinding(Key.LeftArrow);
        gamePlayActions.menuLeft.AddDefaultBinding(InputControlType.DPadLeft);
        gamePlayActions.menuUp.AddDefaultBinding(InputControlType.LeftStickLeft);

        gamePlayActions.menuRight.AddDefaultBinding(Key.RightArrow);
        gamePlayActions.menuRight.AddDefaultBinding(InputControlType.DPadRight);
        gamePlayActions.menuUp.AddDefaultBinding(InputControlType.LeftStickRight);
    }
}
