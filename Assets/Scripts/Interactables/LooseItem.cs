using UnityEngine;

public class LooseItem : Interactable
{
    [SerializeField] MeshFilter meshFilter;
    [SerializeField] MeshRenderer meshRenderer;
    [SerializeField] MeshCollider meshCollider;
    [SerializeField] Rigidbody rigidBody;

    ItemData itemData;

    public override void Awake()
    {
        gridPosition = LevelGrid.GetGridPosition(transform.position);
    }

    public override void Interact(Unit unitPickingUpItem)
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

    public MeshCollider MeshCollider() => meshCollider;

    public void ShowMeshRenderer() => meshRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;

    public void HideMeshRenderer() => meshRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.ShadowsOnly;

    public bool CanSeeMeshRenderer() => meshRenderer.shadowCastingMode == UnityEngine.Rendering.ShadowCastingMode.On;
}