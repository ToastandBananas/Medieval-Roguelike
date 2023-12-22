using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace UnitSystem.UI
{
    public abstract class StatBar : MonoBehaviour
    {
        [SerializeField] protected Slider slider;
        [SerializeField] protected TextMeshProUGUI textMesh;

        public abstract void UpdateValue();
    }
}
