using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace EffectsSystem
{
    public class ParticleEffectPool : MonoBehaviour
    {
        public static ParticleEffectPool Instance;

        [SerializeField] ParticleSystemData[] particleEffectData;

        Dictionary<ParticleSystem, ParticleSystemData.ParticleSystemType> particleEffects = new Dictionary<ParticleSystem, ParticleSystemData.ParticleSystemType>();

        readonly string pooledParticleSystemTag = "Particle Effect";

        void Awake()
        {
            if (Instance != null)
            {
                Debug.LogError($"There's more than one ParticleEffectPool! {transform} - {Instance}");
                Destroy(gameObject);
                return;
            }
            Instance = this;

            AddExistingParticleSystems();
        }

        void Start()
        {
            for (int i = 0; i < particleEffectData.Length; i++)
            {
                for (int j = 0; j < particleEffectData[i].amountToPool; j++)
                {
                    ParticleSystem newParticleEffect = CreateNewParticleEffect(particleEffectData[i].type);
                    newParticleEffect.gameObject.SetActive(false);
                }
            }
        }

        public ParticleSystem GetParticleEffectFromPool(ParticleSystemData.ParticleSystemType type)
        {
            foreach (KeyValuePair<ParticleSystem, ParticleSystemData.ParticleSystemType> particleEffect in particleEffects)
            {
                if (particleEffect.Key.gameObject.activeSelf == false && particleEffect.Value == type)
                {
                    particleEffect.Key.Stop();
                    particleEffect.Key.Clear();
                    return particleEffect.Key;
                }
            }

            return CreateNewParticleEffect(type);
        }

        ParticleSystem CreateNewParticleEffect(ParticleSystemData.ParticleSystemType type)
        {
            for (int i = 0; i < particleEffectData.Length; i++)
            {
                if (particleEffectData[i].type == type)
                {
                    ParticleSystem newParticleEffect = Instantiate(particleEffectData[i].prefab, transform).GetComponent<ParticleSystem>();
                    particleEffects.Add(newParticleEffect, type);
                    return newParticleEffect;
                }
            }

            Debug.LogWarning($"No ParticleEffectData exists for ParticleSystem type {type}. Create a new ScriptableObject and add it to the ParticleEffectsPool.");
            return null;
        }

        void AddExistingParticleSystems()
        {
            ParticleSystem[] existingParticleSystems = GameObject.FindGameObjectsWithTag(pooledParticleSystemTag).Select(go => go.GetComponent<ParticleSystem>()).ToArray();

            foreach (ParticleSystem particleSystem in existingParticleSystems)
            {
                ParticleSystemData.ParticleSystemType type = GetParticleSystemType(particleSystem);
                if (type != ParticleSystemData.ParticleSystemType.None)
                {
                    AddParticleSystem(particleSystem, type);
                }
            }
        }

        void AddParticleSystem(ParticleSystem particleSystem, ParticleSystemData.ParticleSystemType type)
        {
            if (particleEffects.ContainsKey(particleSystem) == false)
            {
                Debug.LogError($"No pool exists for ParticleSystem type {type}");
                return;
            }

            particleEffects.Add(particleSystem, type);
            particleSystem.transform.parent = transform;
        }

        ParticleSystemData.ParticleSystemType GetParticleSystemType(ParticleSystem particleSystem)
        {
            // Determine the type based on the ParticleSystem name
            string name = particleSystem.name.ToLower();
            if (name.Contains("blood-spray")) return ParticleSystemData.ParticleSystemType.BloodSpray;
            if (name.Contains("fire-torch")) return ParticleSystemData.ParticleSystemType.Fire_Torch;
            if (name.Contains("dust")) return ParticleSystemData.ParticleSystemType.Dust;

            Debug.LogWarning($"The ParticleSystem {particleSystem.name} does not have a proper name in the hierarchy. A ParticleSystemType cannot be determined.");
            return ParticleSystemData.ParticleSystemType.None;
        }
    }
}
