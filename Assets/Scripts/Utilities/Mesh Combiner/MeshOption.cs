using UnityEngine;

namespace Utilities
{
    public class MeshOption : MonoBehaviour
    {
        public float size = 0.25f;
        public GameObject[] options;
        public int currentOption = -1;
        public GameObject instance;

        void OnDrawGizmos()
        {
            Gizmos.color = new Color(2, 2, 0.75f, 1);
            Gizmos.DrawSphere(transform.position, size);
        }

        void OnDrawGizmosSelected()
        {
            Gizmos.DrawWireSphere(transform.position, size * 1.1f);
        }

        public void NextOption()
        {
            if (instance != null)
            {
                DestroyImmediate(instance);
                instance = null;
            }

            currentOption++;
            if (currentOption >= options.Length)
                currentOption = -1;
            else
            {
                // Put a new GameObject into the scene view
                instance = Instantiate(options[currentOption]);
                instance.transform.SetParent(transform, false);
                instance.transform.position = transform.position;
                // Debug.Log(name + " -> " + instance.name);
            }
        }
    }
}
