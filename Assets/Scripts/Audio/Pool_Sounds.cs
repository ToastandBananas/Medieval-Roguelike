using System.Collections.Generic;
using UnityEngine;

namespace SoundSystem
{
    public class Pool_Sounds : MonoBehaviour
    {
        public static Pool_Sounds Instance;

        [SerializeField] AudioSource soundPrefab;
        [SerializeField] int amountToPool = 10;

        static readonly List<AudioSource> audioSources = new();

        void Awake()
        {
            if (Instance != null)
            {
                Debug.LogError($"There's more than one SoundPool! ({Instance.name})");
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        void Start()
        {
            for (int i = 0; i < amountToPool; i++)
            {
                AudioSource newAudioSource = CreateNewAudioSource(null);
                newAudioSource.gameObject.SetActive(false);
            }
        }

        public static AudioSource GetSoundFromPool(Sound sound)
        {
            for (int i = 0; i < audioSources.Count; i++)
            {
                if (!audioSources[i].gameObject.activeSelf)
                {
                    sound?.SetSource(audioSources[i]);
                    return audioSources[i];
                }
            }

            return CreateNewAudioSource(sound);
        }

        static AudioSource CreateNewAudioSource(Sound sound)
        {
            AudioSource newAudioSource = Instantiate(Instance.soundPrefab, Instance.transform).GetComponent<AudioSource>();
            newAudioSource.outputAudioMixerGroup = AudioManager.Instance.masterAudioMixerGroup;

            sound?.SetSource(newAudioSource);
            audioSources.Add(newAudioSource);
            return newAudioSource;
        }
    }
}