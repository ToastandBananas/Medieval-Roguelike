using UnityEngine;
using UnityEngine.EventSystems;

namespace InventorySystem
{
    public class ContainerUIDragHandle : MonoBehaviour, IBeginDragHandler, IDragHandler
    {
        [SerializeField] RectTransform rectTransform;
        [SerializeField] Transform parentTransform;
        Transform containerInventoryUITransform;

        readonly int defaultDragSiblingIndex = 2;

        Vector3 offset;
        float yOffset;

        void Awake()
        {
            // The difference between the center of the ContainerUI and this drag handle
            yOffset = transform.position.y - parentTransform.position.y - (rectTransform.rect.height / 2f);
            containerInventoryUITransform = parentTransform.parent;
        }

        public void Reset()
        {
            parentTransform.SetParent(containerInventoryUITransform);
        }

        public void SetParent()
        {
            parentTransform.SetParent(InventoryUI.Instance.transform, true);
            parentTransform.SetSiblingIndex(defaultDragSiblingIndex + Mathf.Abs(containerInventoryUITransform.childCount - 1));
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            SetParent();
            offset.Set(Input.mousePosition.x - rectTransform.position.x, yOffset, 0);
        }

        public void OnDrag(PointerEventData eventData)
        {
            parentTransform.position = Input.mousePosition - offset;
        }
    }
}
