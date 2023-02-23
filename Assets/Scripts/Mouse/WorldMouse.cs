using UnityEngine;

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
    public static Unit currentUnit;
    // public static LooseItem currentLooseItem;

    Vector2 hotSpot = new Vector2(0.04f, 0.04f);

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

        GridPosition mouseGridPosition = LevelGrid.GetGridPosition(hit.point);
        if (mouseGridPosition != currentGridPosition)
        {
            currentGridPosition = mouseGridPosition;
            currentUnit = LevelGrid.Instance.GetUnitAtGridPosition(mouseGridPosition);
        }
        
        /*Physics.Raycast(ray, out RaycastHit looseItemHit, float.MaxValue, Instance.looseItemLayerMask);
        if (looseItemHit.collider != null)
        {
            // Get item
        }*/

        return hit.point;
    }

    public static GridPosition GetCurrentGridPosition() => LevelGrid.GetGridPosition(GetPosition());

    public LayerMask MousePlaneLayerMask() => mousePlaneLayerMask;

    public static void ChangeCursor(CursorState cursorState)
    {
        switch (cursorState)
        {
            case CursorState.Default:
                Instance.SetCursor(Instance.defaultCursor);
                break;
            case CursorState.MeleeAttack:
                Instance.SetCursor(Instance.meleeAttackCursor);
                break;
            case CursorState.RangedAttack:
                Instance.SetCursor(Instance.rangedAttackCursor);
                break;
            case CursorState.UseDoor:
                Instance.SetCursor(Instance.useDoorCursor);
                break;
            case CursorState.PickupItem:
                Instance.SetCursor(Instance.pickupItemCursor);
                break;
            case CursorState.LootBag:
                Instance.SetCursor(Instance.lootBagCursor);
                break;
            case CursorState.LootContainer:
                Instance.SetCursor(Instance.lootContainerCursor);
                break;
            case CursorState.Speak:
                Instance.SetCursor(Instance.speakCursor);
                break;
            default:
                Instance.SetCursor(Instance.defaultCursor);
                break;
        }
    }

    void SetCursor(Texture2D cursorTexture) => Cursor.SetCursor(cursorTexture, hotSpot, CursorMode.Auto); 
}
