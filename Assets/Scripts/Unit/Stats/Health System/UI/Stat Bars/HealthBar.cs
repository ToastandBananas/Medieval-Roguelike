using UnityEngine;

namespace UnitSystem.UI
{
    public class HealthBar : StatBar
    {
        [SerializeField] BodyPartType bodyPartType;
        [SerializeField] BodyPartSide bodyPartSide;
        BodyPart bodyPart;

        public void Initialize()
        {
            bodyPart = UnitManager.player.HealthSystem.GetBodyPart(bodyPartType, bodyPartSide);
            UpdateValue();
        }

        public override void UpdateValue()
        {
            slider.value = bodyPart.CurrentHealthNormalized;
            textMesh.text = $"{bodyPart.CurrentHealth}/{bodyPart.MaxHealth.GetValue()}";
        }

        public BodyPartType BodyPartType => bodyPartType;
        public BodyPartSide BodyPartSide => bodyPartSide;
    }
}
