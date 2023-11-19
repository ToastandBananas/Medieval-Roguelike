using UnityEngine;
using GridSystem;

namespace GeneralUI
{
    public enum CursorState { Default, MeleeAttack, RangedAttack, UseDoor, PickupItem, LootBag, LootContainer, Speak }

    public class WorldMouse : MonoBehaviour
    {
        public static WorldMouse Instance;

        [SerializeField] LayerMask mousePlaneLayerMask;

        [Header("Cursors")]
        public Texture2D defaultCursor;
        public Texture2D meleeAttackCursor;
        public Texture2D rangedAttackCursor;
        public Texture2D useDoorCursor;
        public Texture2D pickupItemCursor;
        public Texture2D lootBagCursor;
        public Texture2D lootContainerCursor;
        public Texture2D speakCursor;

        public static GridPosition currentGridPosition;

        static Vector2 hotSpot = new Vector2(0.04f, 0.04f);

        void Awake()
        {
            if (Instance != null)
            {
                Debug.LogError("There's more than one WorldMouse! " + transform + " - " + Instance);
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        void Start()
        {
            SetCursor(defaultCursor);
        }

        public static Vector3 GetPosition()
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            Physics.Raycast(ray, out RaycastHit hit, float.MaxValue, Instance.mousePlaneLayerMask);

            hit.point = LevelGrid.SnapPosition(hit.point);
            if (hit.point != currentGridPosition)
                currentGridPosition.Set(hit.point);

            return hit.point;
        }

        public static GridPosition CurrentGridPosition()
        {
            currentGridPosition.Set(GetPosition());
            return currentGridPosition;
        }

        public static LayerMask MousePlaneLayerMask => Instance.mousePlaneLayerMask;

        public static void ChangeCursor(CursorState cursorState)
        {
            switch (cursorState)
            {
                case CursorState.Default:
                    SetCursor(Instance.defaultCursor);
                    break;
                case CursorState.MeleeAttack:
                    SetCursor(Instance.meleeAttackCursor);
                    break;
                case CursorState.RangedAttack:
                    SetCursor(Instance.rangedAttackCursor);
                    break;
                case CursorState.UseDoor:
                    SetCursor(Instance.useDoorCursor);
                    break;
                case CursorState.PickupItem:
                    SetCursor(Instance.pickupItemCursor);
                    break;
                case CursorState.LootBag:
                    SetCursor(Instance.lootBagCursor);
                    break;
                case CursorState.LootContainer:
                    SetCursor(Instance.lootContainerCursor);
                    break;
                case CursorState.Speak:
                    SetCursor(Instance.speakCursor);
                    break;
                default:
                    SetCursor(Instance.defaultCursor);
                    break;
            }
        }

        static void SetCursor(Texture2D cursorTexture) => Cursor.SetCursor(cursorTexture, hotSpot, CursorMode.Auto);
    }
}
