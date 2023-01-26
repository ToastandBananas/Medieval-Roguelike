using UnityEngine;

public class WorldMouse : MonoBehaviour
{
    public static WorldMouse Instance;
    [SerializeField] LayerMask mousePlaneLayerMask;

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
}
