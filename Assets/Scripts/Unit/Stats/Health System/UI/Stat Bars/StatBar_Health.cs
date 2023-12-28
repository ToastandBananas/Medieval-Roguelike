using UnityEngine;

namespace UnitSystem.UI
{
    public class StatBar_Health : StatBar
    {
        [SerializeField] BodyPartType bodyPartType;
        [SerializeField] BodyPartSide bodyPartSide;
        BodyPart bodyPart;

        public override void Initialize(Unit unit)
        {
            base.Initialize(unit);
            bodyPart = unit.HealthSystem.GetBodyPart(bodyPartType, bodyPartSide);
            UpdateValue(bodyPart.CurrentHealthNormalized);
        }

        public override void UpdateValue(float startNormalizedHealth)
        {
            base.UpdateValue(startNormalizedHealth);
            slider.value = bodyPart.CurrentHealthNormalized;
            if (textMesh != null)
                textMesh.text = $"{bodyPart.CurrentHealth}/{bodyPart.MaxHealth.GetValue()}";
        }

        public BodyPartType BodyPartType => bodyPartType;
        public BodyPartSide BodyPartSide => bodyPartSide;
        public BodyPart BodyPart => bodyPart;
    }
}
