using UnityEngine;
using TMPro;

namespace GridSystem
{
    public class GridDebugObject : MonoBehaviour
    {
        [SerializeField] TextMeshPro gridPositionText;

        object gridObject;

        public virtual void SetGridObject(object gridObject)
        {
            this.gridObject = gridObject;
        }

        protected virtual void Update()
        {
            gridPositionText.text = gridObject.ToString();
        }
    }
}