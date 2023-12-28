using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace UnitSystem.UI
{
    public abstract class StatBar : MonoBehaviour
    {
        [SerializeField] protected Slider changeSlider;
        [SerializeField] protected Slider slider;
        [SerializeField] protected TextMeshProUGUI textMesh;

        bool isAnimating;
        readonly float animateSpeed = 1f;
        readonly float animateDelay = 0.5f;
        float animateDelayTimer;

        protected Unit unit;

        public virtual void Initialize(Unit unit) => this.unit = unit;

        public virtual void UpdateValue(float startNormalizedValue)
        {
            unit.StartCoroutine(AnimateChangeBar(startNormalizedValue));
        }

        protected IEnumerator AnimateChangeBar(float startNormalizedValue)
        {
            changeSlider.gameObject.SetActive(true);
            changeSlider.value = startNormalizedValue;
            if (isAnimating) // If already animating, all we need to do is update the value
                yield break;

            isAnimating = true;
            animateDelayTimer = 0f;
            while (animateDelayTimer < animateDelay || changeSlider.value > slider.value)
            {
                if (animateDelayTimer < animateDelay)
                    animateDelayTimer += Time.deltaTime;
                else
                    changeSlider.value -= animateSpeed * Time.deltaTime;

                yield return null;
            }

            changeSlider.gameObject.SetActive(false);
            isAnimating = false;
        }
    }
}
