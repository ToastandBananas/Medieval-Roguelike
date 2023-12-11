using UnityEngine;
using InControl;

namespace Controls
{
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
            // Action Buttons
            gamePlayActions.select.AddDefaultBinding(Mouse.LeftButton);
            gamePlayActions.select.AddDefaultBinding(InputControlType.Action1); // X(PS4) / A(Xbox)

            gamePlayActions.turnMode.AddDefaultBinding(Key.LeftShift);
            gamePlayActions.turnMode.AddDefaultBinding(InputControlType.LeftBumper);

            gamePlayActions.skipTurn.AddDefaultBinding(Key.Space);
            gamePlayActions.skipTurn.AddDefaultBinding(InputControlType.Action2); // Circle(PS4) / B(Xbox)

            gamePlayActions.cancelAction.AddDefaultBinding(Key.Space);
            gamePlayActions.cancelAction.AddDefaultBinding(InputControlType.Action2); // Circle(PS4) / B(Xbox)

            gamePlayActions.swapWeapons.AddDefaultBinding(Key.Tab);
            gamePlayActions.swapWeapons.AddDefaultBinding(InputControlType.Action4); // Triangle(PS4) / Y(Xbox)

            gamePlayActions.switchVersatileStance.AddDefaultBinding(Key.F);
            gamePlayActions.switchVersatileStance.AddDefaultBinding(InputControlType.Action3);

            // Move Mode Buttons
            gamePlayActions.sneak.AddDefaultBinding(Key.Z);
            gamePlayActions.walk.AddDefaultBinding(Key.X);
            gamePlayActions.run.AddDefaultBinding(Key.C);
            gamePlayActions.sprint.AddDefaultBinding(Key.V);

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
            gamePlayActions.menuSelect.AddDefaultBinding(InputControlType.Action1); // X(PS4) / A(Xbox)

            gamePlayActions.menuContext.AddDefaultBinding(Mouse.RightButton);
            gamePlayActions.menuContext.AddDefaultBinding(InputControlType.Action3); // Square(PS4) / X(Xbox)

            gamePlayActions.menuQuickUse.AddDefaultBinding(Mouse.MiddleButton);
            gamePlayActions.menuQuickUse.AddDefaultBinding(InputControlType.RightBumper);

            gamePlayActions.menuUp.AddDefaultBinding(Key.UpArrow);
            gamePlayActions.menuUp.AddDefaultBinding(InputControlType.DPadUp);
            gamePlayActions.menuUp.AddDefaultBinding(InputControlType.LeftStickUp);

            gamePlayActions.menuDown.AddDefaultBinding(Key.DownArrow);
            gamePlayActions.menuDown.AddDefaultBinding(InputControlType.DPadDown);
            gamePlayActions.menuDown.AddDefaultBinding(InputControlType.LeftStickDown);

            gamePlayActions.menuLeft.AddDefaultBinding(Key.LeftArrow);
            gamePlayActions.menuLeft.AddDefaultBinding(InputControlType.DPadLeft);
            gamePlayActions.menuLeft.AddDefaultBinding(InputControlType.LeftStickLeft);

            gamePlayActions.menuRight.AddDefaultBinding(Key.RightArrow);
            gamePlayActions.menuRight.AddDefaultBinding(InputControlType.DPadRight);
            gamePlayActions.menuRight.AddDefaultBinding(InputControlType.LeftStickRight);

            gamePlayActions.toggleInventory.AddDefaultBinding(Key.I);
            gamePlayActions.toggleInventory.AddDefaultBinding(InputControlType.Select);

            gamePlayActions.splitStackEnter.AddDefaultBinding(Key.Return);
            gamePlayActions.splitStackEnter.AddDefaultBinding(Key.PadEnter);
            gamePlayActions.splitStackEnter.AddDefaultBinding(InputControlType.Action3); // Square(PS4) / X(Xbox)

            gamePlayActions.splitStackEnter.AddDefaultBinding(Key.Backspace);
            gamePlayActions.splitStackEnter.AddDefaultBinding(Key.Delete);
            gamePlayActions.splitStackDelete.AddDefaultBinding(InputControlType.Action2); // Circle(PS4) / B(Xbox)

            gamePlayActions.showLooseItemTooltips.AddDefaultBinding(Key.LeftAlt);
            gamePlayActions.showLooseItemTooltips.AddDefaultBinding(Key.RightAlt);
            gamePlayActions.showLooseItemTooltips.AddDefaultBinding(InputControlType.RightStickButton);
        }
    }
}
