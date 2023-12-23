using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace UnitSystem.UI
{
    public abstract class StatBar : MonoBehaviour
    {
        [SerializeField] protected Slider slider;
        [SerializeField] protected TextMeshProUGUI textMesh;

        protected Unit unit;

        public virtual void Initialize(Unit unit) => this.unit = unit;

        public abstract void UpdateValue();
    }
}
