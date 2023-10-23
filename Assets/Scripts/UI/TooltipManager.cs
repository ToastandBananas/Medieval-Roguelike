using InventorySystem;
using UnityEngine;

namespace GeneralUI 
{
    public class TooltipManager : MonoBehaviour
    {
        public static TooltipManager Instance;

        [SerializeField] Tooltip[] tooltips;

        public static Slot currentSlot { get; private set; }

        void Awake()
        {
            if (Instance != null)
            {
                Debug.LogError("There's more than one TooltipManager! " + transform + " - " + Instance);
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        public static void ClearTooltips()
        {
            for (int i = 0; i < Instance.tooltips.Length; i++)
            {
                Instance.tooltips[i].ClearTooltip();
            }

            currentSlot = null;
        }

        public static Tooltip GetTooltip()
        {
            for (int i = 0; i < Instance.tooltips.Length; i++)
            {
                if (Instance.tooltips[i].gameObject.activeSelf == false)
                    return Instance.tooltips[i];
            }

            Debug.LogWarning("Not enough tooltips...");
            return null;
        }

        public static void SetCurrentSlot(Slot slot) => currentSlot = slot;
    }
}
