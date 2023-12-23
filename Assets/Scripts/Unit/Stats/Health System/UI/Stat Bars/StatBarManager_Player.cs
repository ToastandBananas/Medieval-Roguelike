using InventorySystem;
using UnityEngine;
using UnityEngine.UI;

namespace UnitSystem.UI
{
    public class StatBarManager_Player : MonoBehaviour
    {
        public static StatBarManager_Player Instance;

        [SerializeField] RectTransform verticalLayoutGroupRectTransform;
        [SerializeField] RectTransform groupParent;
        [SerializeField] RectTransform expandButtonRectTransform;
        [SerializeField] RectTransform barsParent;

        [Header("Stat Bars")]
        [SerializeField] StatBar_Energy energyBar;
        [SerializeField] StatBar_Health[] healthBars;
        [SerializeField] RectTransform[] healthParents;
        [SerializeField] StatBar_Armor[] armorBars;

        readonly int expandedBarHeight = 140;
        readonly int contractedBarHeight = 60;

        void Awake()
        {
            if (Instance != null)
            {
                Debug.LogError("There's more than one PlayerStatBarManager! " + transform + " - " + Instance);
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        void Start()
        {
            energyBar.Initialize(UnitManager.player);
            for (int i = 0; i < healthBars.Length; i++)
                healthBars[i].Initialize(UnitManager.player);

            for (int i = 0; i < armorBars.Length; i++)
                armorBars[i].Initialize(UnitManager.player);
        }

        public void ToggleExpand()
        {
            Vector3 expandBarScale = expandButtonRectTransform.localScale;
            expandBarScale.y *= -1;
            expandButtonRectTransform.localScale = expandBarScale;

            Vector2 barsParentSizeDelta = barsParent.sizeDelta;
            Vector2 iconsParentSizeDelta = groupParent.sizeDelta;
            if (Mathf.Approximately(barsParent.sizeDelta.y, expandedBarHeight)) // If contracting
            {
                barsParentSizeDelta.y = contractedBarHeight;
                barsParent.sizeDelta = barsParentSizeDelta;

                iconsParentSizeDelta.y = contractedBarHeight;
                groupParent.sizeDelta = iconsParentSizeDelta;

                for (int i = 0; i < healthParents.Length; i++)
                    healthParents[i].gameObject.SetActive(false);

                for (int i = 2; i < armorBars.Length; i++)
                    armorBars[i].gameObject.SetActive(false);
            }
            else // If expanding
            {
                barsParentSizeDelta.y = expandedBarHeight;
                barsParent.sizeDelta = barsParentSizeDelta;

                iconsParentSizeDelta.y = expandedBarHeight;
                groupParent.sizeDelta = iconsParentSizeDelta;

                for (int i = 0; i < healthParents.Length; i++)
                    healthParents[i].gameObject.SetActive(true);

                for (int i = 2; i < armorBars.Length; i++)
                    armorBars[i].gameObject.SetActive(true);
            }

            LayoutRebuilder.ForceRebuildLayoutImmediate(verticalLayoutGroupRectTransform);
        }

        public static void UpdateHealthBar(BodyPartType bodyPartType, BodyPartSide bodyPartSide)
        {
            for (int i = 0; i < Instance.healthBars.Length; i++)
            {
                if (Instance.healthBars[i].BodyPartType == bodyPartType && Instance.healthBars[i].BodyPartSide == bodyPartSide)
                {
                    Instance.healthBars[i].UpdateValue();
                    break;
                }
            }
        }

        public static void UpdateArmorBar(EquipSlot equipSlot)
        {
            for (int i = 0; i < Instance.armorBars.Length; i++)
            {
                if (Instance.armorBars[i].EquipSlot == equipSlot)
                {
                    Instance.armorBars[i].UpdateValue();
                    break;
                }
            }
        }

        public static void UpdateEnergyBar() => Instance.energyBar.UpdateValue();
    }
}
