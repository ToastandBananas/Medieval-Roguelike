using UnityEngine;

namespace UnitSystem.UI
{
    public class StatBar_Energy : StatBar
    {
        public override void Initialize(Unit unit)
        {
            base.Initialize(unit);
            UpdateValue();
        }

        public override void UpdateValue()
        {
            slider.value = unit.Stats.CurrentEnergyNormalized;
            if (textMesh != null)
                textMesh.text = $"Energy: {unit.Stats.CurrentEnergy}/{unit.Stats.MaxEnergy}";
        }
    }
}
