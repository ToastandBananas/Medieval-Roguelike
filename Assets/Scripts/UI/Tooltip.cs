using UnityEngine.UI;
using UnityEngine;
using System.Text;
using InventorySystem;

namespace GeneralUI
{
    public class Tooltip : MonoBehaviour
    {
        StringBuilder stringBuilder = new StringBuilder(50);
        RectTransform rectTransform;
        Canvas canvas;

        Vector3 offset;

        // Start is called before the first frame update
        void Awake()
        {
            rectTransform = GetComponentInParent<RectTransform>();
            // canvas = GameObject.Find("Tooltip Canvas").GetComponent<Canvas>();
        }

        public void ShowItemTooltip(Slot slot)
        {


            RecalculateTooltipSize();
        }

        public void ClearTooltip()
        {
            stringBuilder.Clear();
            gameObject.SetActive(false);
        }

        void CalculateOffset(Item item, EquipSlot equipSlot)
        {
            /*if (equipSlot == null)
            {
                // For invSlots
                if (item.iconWidth == 1)
                    offset = new Vector3(0.55f, 0.55f);
                else if (item.iconWidth == 2)
                    offset = new Vector3(1.65f, 0.55f);
                else if (item.iconWidth == 3)
                    offset = new Vector3(2.75f, 0.55f);

                // If the tooltip is going to be too far to the right
                if (tooltipSlot.slotParent != invUI.containerParent && tooltipSlot.slotCoordinate.x > 3)
                    offset += new Vector3(-5.15f, 0);

                // Get our mouse position
                RectTransformUtility.ScreenPointToLocalPointInRectangle(canvas.transform as RectTransform, Input.mousePosition, canvas.worldCamera, out Vector2 pos);

                // If the tooltip is going to be too close to the bottom
                if (pos.y < -260.0f)
                    offset += new Vector3(0, 2f);
            }
            else
            {
                // For equipSlots
                offset = new Vector3(0.75f, 0.75f);

                if (equipSlot.thisEquipmentSlot == EquipmentSlot.Boots || equipSlot.thisEquipmentSlot == EquipmentSlot.Quiver)
                    offset += new Vector3(0, 2f);
            }*/
        }

        void RecalculateTooltipSize()
        {
            LayoutRebuilder.ForceRebuildLayoutImmediate(rectTransform);
        }
    }
}
