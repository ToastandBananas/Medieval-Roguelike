using UnityEngine;

namespace EffectsSystem
{
    [CreateAssetMenu(fileName = "NewParticleSystemData", menuName = "ParticleSystemData")]
    public class ParticleSystemData : ScriptableObject
    {
        public enum ParticleSystemType
        {
            BloodSpray = 0,
            Dust = 20,
            Fire_Torch = 10,
            None = 10000
        }

        public ParticleSystemType type;
        public ParticleSystem prefab;
        public int amountToPool = 10;
    }
}
