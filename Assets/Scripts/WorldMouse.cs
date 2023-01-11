using UnityEngine;

public class WorldMouse : MonoBehaviour
{
    private static WorldMouse instance;
    [SerializeField] LayerMask mousePlaneLayerMask;

    void Awake()
    {
        if (instance != null)
        {
            Debug.LogError("There's more than one WorldMouse! " + transform + " - " + instance);
            Destroy(gameObject);
            return;
        }
        instance = this;
    }

    public static Vector3 GetPosition()
    {
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        Physics.Raycast(ray, out RaycastHit hit, float.MaxValue, instance.mousePlaneLayerMask);
        return hit.point;
    }
}
