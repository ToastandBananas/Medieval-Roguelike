using UnityEngine;

public class NPCDistanceTrigger : MonoBehaviour
{
    public static NPCDistanceTrigger Instance;

    SphereCollider sphereCollider;

    [SerializeField] LayerMask unitsMask;

    void Awake()
    {
        if (Instance != null)
        {
            if (Instance != this)
            {
                Debug.LogWarning("More than one Instance of NPCDistanceTrigger. Fix me!");
                Destroy(gameObject);
            }
        }
        else
            Instance = this;

        sphereCollider = GetComponent<SphereCollider>();
    }

    public float TriggerRange() => sphereCollider.radius;

    void OnTriggerEnter(Collider other)
    {
        if (unitsMask == (unitsMask | (1 << other.transform.gameObject.layer)))
        {
            // Debug.Log(other.gameObject.name + " can now perform actions.");
            other.GetComponent<Unit>().unitActionHandler.SetCanPerformActions(true);
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (unitsMask == (unitsMask | (1 << other.transform.gameObject.layer)))
        {
            // Debug.Log(other.gameObject.name + " cannot perform actions.");
            other.GetComponent<Unit>().unitActionHandler.SetCanPerformActions(false);
        }
    }
}
