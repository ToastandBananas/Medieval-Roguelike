using UnitSystem.ActionSystem.Actions;
using UnityEngine;
using UnityEngine.UI;

namespace UnitSystem.ActionSystem.UI
{
    public class MoveModeButton : MonoBehaviour
    {
        [SerializeField] MoveMode moveMode;

        [Header("Image")]
        [SerializeField] Image image;
        [SerializeField] Sprite defaultBackgroundImage;
        [SerializeField] Sprite selectedBackgroundImage;

        public void OnButtonClicked()
        {
            UnitManager.player.UnitActionHandler.MoveAction.SetMoveMode(moveMode);
        }

        public void Select()
        {
            image.sprite = selectedBackgroundImage;
        }

        public void Deselect()
        {
            image.sprite = defaultBackgroundImage;
        }

        public MoveMode MoveMode => moveMode;
    }
}
