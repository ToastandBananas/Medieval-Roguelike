using UnityEngine;

namespace UnitSystem.UI
{
    public class EnergyBar : StatBar
    {
        void Start()
        {
            UpdateValue();
        }

        public override void UpdateValue()
        {
            slider.value = UnitManager.player.Stats.CurrentEnergyNormalized;
            textMesh.text = $"{UnitManager.player.Stats.CurrentEnergy}/{UnitManager.player.Stats.MaxEnergy}";
        }
    }
}
