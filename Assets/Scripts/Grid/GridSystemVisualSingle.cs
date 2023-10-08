using UnityEngine;

namespace GridSystem
{
    public class GridSystemVisualSingle : MonoBehaviour
    {
        [SerializeField] MeshRenderer meshRenderer;

        public void SetMaterial(Material material)
        {
            meshRenderer.material = material;
        }
    }
}