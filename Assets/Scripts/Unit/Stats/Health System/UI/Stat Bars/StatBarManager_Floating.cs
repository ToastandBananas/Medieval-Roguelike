using System.Collections;
using UnityEngine;
using CameraSystem;

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
            gameObject.SetActive(false);
            unit = null;
            bodyArmorBar.gameObject.SetActive(false);
            helmArmorBar.gameObject.SetActive(false);
        }

        IEnumerator StartTimer()
        {
            timer = 0f;
            while (gameObject.activeSelf && timer < showTime)
            {
                timer += Time.deltaTime;
                yield return null;
            }

            Hide();
        }

        IEnumerator SetPosition()
        {
            float height = headRectTransform.rect.height * GameManager.Canvas.scaleFactor;
            height += torsoRectTransform.rect.height * GameManager.Canvas.scaleFactor;
            if (helmArmorBar.gameObject.activeSelf)
                height += helmRectTransform.rect.height * GameManager.Canvas.scaleFactor;
            if (bodyArmorBar.gameObject.activeSelf)
                height += bodyArmorRectTransform.rect.height * GameManager.Canvas.scaleFactor;

            float heightOffset = 6f * unit.ShoulderHeight * GameManager.Canvas.scaleFactor;
            Vector3 targetScale = Vector3.one;
            float minScale = 0.25f;
            //float verticalSpacing = 2f;

            while (gameObject.activeSelf)
            {
                float inverseNormalizedZoom = Mathf.Max(1f - (CameraController.CurrentZoom / CameraController.MAX_FOLLOW_Y_OFFSET) + (CameraController.MIN_FOLLOW_Y_OFFSET / CameraController.MAX_FOLLOW_Y_OFFSET), minScale);
                targetScale.Set(inverseNormalizedZoom, inverseNormalizedZoom, 1f);
                rectTransform.localScale = targetScale;

                // Calculate the distance from the camera based on the current field of view
                // float distance = Mathf.Abs(heightOffset / Mathf.Tan(Camera.main.fieldOfView * 0.5f * Mathf.Deg2Rad));

                // Calculate the target position in screen space, considering the distance
                Vector2 targetScreenPosition = Camera.main.WorldToScreenPoint(unit.transform.position + (heightOffset * Vector3.up));

                // Adjust for canvas scale factor
                targetScreenPosition.y -= height / 2f * GameManager.Canvas.scaleFactor;
                targetScreenPosition.y *= GameManager.Canvas.scaleFactor;

                // Loop through WorldTooltips and adjust positions to prevent vertical overlap
                /*foreach (StatBarManager_Floating floatingStatBar in Pool_FloatingStatBar.floatingStatBars)
                {
                    if (floatingStatBar == this || !floatingStatBar.gameObject.activeSelf)
                        continue;

                    // Check if the tooltip will overlap with the previous tooltip vertically
                    if (Mathf.Abs(newPosition.y - floatingStatBar.rectTransform.position.y) < height)
                    {
                        // Adjust the tooltip's vertical position to stack them vertically
                        newPosition.y = floatingStatBar.rectTransform.position.y + height + verticalSpacing;
                    }
                }*/

                // Set the tooltip's position after adjusting
                rectTransform.position = targetScreenPosition;

                // Wait a couple frames before checking positions again
                yield return null;
                yield return null;
            }
        }

        public void UpdateHeadHealth() => headHealthBar.UpdateValue();
        public void UpdateTorsoHealth() => torsoHealthBar.UpdateValue();
        public void UpdateHelmArmor() => helmArmorBar.UpdateValue();
        public void UpdateBodyArmor() => bodyArmorBar.UpdateValue();
    }
}
