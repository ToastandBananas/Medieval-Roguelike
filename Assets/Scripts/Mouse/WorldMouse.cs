using UnityEngine;

public class WorldMouse : MonoBehaviour
{
    public static WorldMouse Instance;
    [SerializeField] LayerMask mousePlaneLayerMask;

    [Header("Cursor")]
    public Texture2D defaultCursorTexture;
    public Sprite defaultCursorSprite;
    public Vector2 hotSpot;

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

    public static Vector3 GetPosition()
    {
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        Physics.Raycast(ray, out RaycastHit hit, float.MaxValue, Instance.mousePlaneLayerMask);
        return hit.point;
    }

    public LayerMask MousePlaneLayerMask() => mousePlaneLayerMask;

    public void SetCursor(Texture2D cursorTexture, Sprite cursorSprite) => Cursor.SetCursor(cursorTexture, cursorSprite.pivot, CursorMode.Auto); 
}
