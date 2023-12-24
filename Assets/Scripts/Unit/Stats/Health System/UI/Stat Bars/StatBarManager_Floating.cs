using System.Collections;
using UnityEngine;
using CameraSystem;
using InventorySystem;

namespace UnitSystem.UI
{
    public class StatBarManager_Floating : MonoBehaviour
    {
        [SerializeField] RectTransform rectTransform;

        [Header("Bars")]
        [SerializeField] StatBar_Health headHealthBar;
        [SerializeField] StatBar_Health torsoHealthBar;
        [SerializeField] StatBar_Armor helmArmorBar;
        [SerializeField] StatBar_Armor bodyArmorBar;

        [Header("Bar RectTransforms")]
        [SerializeField] RectTransform headRectTransform;
        [SerializeField] RectTransform torsoRectTransform;
        [SerializeField] RectTransform helmRectTransform;
        [SerializeField] RectTransform bodyArmorRectTransform;

        Unit unit;

        float timer;
        readonly float showTime = 12f;

        public void Initialize(Unit unit)
        {
            this.unit = unit;
            if (unit == null)
                return;

            unit.SetStatBarManager(this);

            headHealthBar.Initialize(unit);
            torsoHealthBar.Initialize(unit);

            if (unit.UnitEquipment != null)
            {
                helmArmorBar.Initialize(unit);
                helmArmorBar.gameObject.SetActive(true);
                bodyArmorBar.Initialize(unit);
                bodyArmorBar.gameObject.SetActive(true);
            }

            gameObject.SetActive(true);
            unit.StartCoroutine(SetPosition());
        }

        public void Show(Unit unit)
        {
            if (unit != null && this.unit == unit && gameObject.activeSelf)
            {
                timer = 0f;
                return;
            }

            Initialize(unit);
            unit.StartCoroutine(StartTimer());
        }

        public void Hide()
        {
            StopTempHide();
            if (unit != null)
                unit.SetStatBarManager(null);

            unit = null;
            bodyArmorBar.gameObject.SetActive(false);
            helmArmorBar.gameObject.SetActive(false);
            gameObject.SetActive(false);
        }

        void TempHide()
        {
            for (int i = 0; i < transform.childCount; i++)
                transform.GetChild(i).gameObject.SetActive(false);
        }

        void StopTempHide()
        {
            for (int i = 0; i < transform.childCount; i++)
                transform.GetChild(i).gameObject.SetActive(true);
        }

        IEnumerator StartTimer()
        {
            timer = 0f;
            while (timer < showTime)
            {
                if (!gameObject.activeSelf)
                    yield break;

                timer += Time.deltaTime;
                yield return null;
            }

            Hide();
        }

        IEnumerator SetPosition()
        {
            float heightOffset = 2.25f * unit.ShoulderHeight;
            Vector3 targetScale = Vector3.one;
            float minScale = 0.5f;
            int lastZoom = Mathf.RoundToInt(CameraController.CurrentZoom);

            while (gameObject.activeSelf)
            {
                if (Mathf.RoundToInt(CameraController.CurrentZoom) != lastZoom)
                {
                    if (CameraController.CurrentZoom > 10)
                    {
                        TempHide();
                        yield return null;
                        continue;
                    }
                    else
                        StopTempHide();
                }

                // Set the scale
                float inverseNormalizedZoom = Mathf.Max(1f - (CameraController.CurrentZoom / CameraController.MAX_FOLLOW_Y_OFFSET) + (CameraController.MIN_FOLLOW_Y_OFFSET / CameraController.MAX_FOLLOW_Y_OFFSET), minScale);
                targetScale.Set(inverseNormalizedZoom, inverseNormalizedZoom, 1f);
                rectTransform.localScale = targetScale;

                // Calculate the target position in screen space, considering the distance
                Vector2 targetScreenPosition = Camera.main.WorldToScreenPoint(unit.transform.position + (heightOffset * Vector3.up));

                // Set the tooltip's position after adjusting
                rectTransform.position = targetScreenPosition;
                yield return null;
            }
        }

        public void UpdateHealthBar(BodyPartType bodyPartType)
        {
            if (bodyPartType == BodyPartType.Torso)
                torsoHealthBar.UpdateValue();
            else if (bodyPartType == BodyPartType.Head)
                headHealthBar.UpdateValue();
        }

        public void UpdateArmorBar(EquipSlot equipSlot)
        {
            if (equipSlot == EquipSlot.BodyArmor)
                bodyArmorBar.UpdateValue();
            else if (equipSlot == EquipSlot.Helm)
                helmArmorBar.UpdateValue();
        }
    }
}
