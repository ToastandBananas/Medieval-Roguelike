using UnityEngine;

namespace InventorySystem
{
    public abstract class Wearable : Equipment
    {
        [Header("Female Equipped Mesh")]
        [SerializeField] Mesh[] meshes_Female;
        [SerializeField] Material[] meshRendererMaterials_Female;

        public Mesh[] Meshes_Female => meshes_Female;
        public Material[] MeshRendererMaterials_Female => meshRendererMaterials_Female;
    }
}
