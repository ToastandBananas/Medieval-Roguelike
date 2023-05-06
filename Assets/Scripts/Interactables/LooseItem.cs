using UnityEngine;

public class LooseItem : MonoBehaviour
{
    [SerializeField] MeshFilter meshFilter;
    [SerializeField] MeshRenderer meshRenderer;
    [SerializeField] MeshCollider meshCollider;
    [SerializeField] Rigidbody rigidBody;

    ItemData itemData;

    public void PickUp(Unit unitPickingUpItem)
    {
        Debug.Log("Picking up item");
    }

    public void SetupMesh(Mesh mesh, Material material)
    {
        meshFilter.mesh = mesh;
        meshRenderer.material = material;
        meshCollider.sharedMesh = mesh;
    }

    public ItemData ItemData => itemData;

    public Rigidbody RigidBody() => rigidBody;
}
