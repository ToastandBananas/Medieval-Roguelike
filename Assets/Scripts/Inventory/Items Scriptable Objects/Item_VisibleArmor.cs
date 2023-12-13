using UnityEngine;

namespace InventorySystem
{
    public class Item_VisibleArmor : Item_Armor
    {
        [Header("Male Equipped Mesh")]
        [SerializeField] Mesh[] meshes;
        [SerializeField] Material[] meshRendererMaterials;

        [Header("Female Equipped Mesh")]
        [SerializeField] Mesh[] meshes_Female;
        [SerializeField] Material[] meshRendererMaterials_Female;

        public Mesh[] Meshes_Male => meshes;
        public Material[] MeshRendererMaterials_Male => meshRendererMaterials;

        public Mesh[] Meshes_Female => meshes_Female;
        public Material[] MeshRendererMaterials_Female => meshRendererMaterials_Female;
    }
}
