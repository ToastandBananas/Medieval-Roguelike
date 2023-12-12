using UnitSystem.ActionSystem.Actions;
using UnityEngine;

namespace UnitSystem.ActionSystem.UI
{
    public class MoveModeButtonManager : MonoBehaviour
    {
        public static MoveModeButtonManager Instance;

        [SerializeField] MoveModeButton[] moveModeButtons;

        MoveModeButton activeMoveModeButton;

        void Awake()
        {
            if (Instance != null)
            {
                Debug.LogError("There's more than one MoveModeButtonManager! " + transform + " - " + Instance);
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        static void SetActiveMoveModeButton(MoveModeButton moveModeButton)
        {
            if (Instance.activeMoveModeButton != null)
                Instance.activeMoveModeButton.Deselect();

            Instance.activeMoveModeButton = moveModeButton;
            moveModeButton.Select();
        }

        public static void SetActiveMoveModeButton(MoveMode moveMode)
        {
            for (int i = 0; i < Instance.moveModeButtons.Length; i++)
            {
                if (moveMode == Instance.moveModeButtons[i].MoveMode)
                {
                    SetActiveMoveModeButton(Instance.moveModeButtons[i]);
                    return;
                }
            }
        }
    }
}
