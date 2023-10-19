using UnityEngine;

namespace InventorySystem
{
    [CreateAssetMenu(fileName = "New Wearable", menuName = "Inventory/Wearable")]
    public class Wearable : Equipment
    {
        [Header("Female Equipped Mesh")]
        [SerializeField] Mesh[] meshes_Female;
        [SerializeField] Material[] meshRendererMaterials_Female;
    }
}
