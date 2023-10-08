using System.Collections.Generic;
using UnityEngine;

namespace SoundSystem
{
    public class SoundPool : MonoBehaviour
    {
        public static SoundPool Instance;

        [SerializeField] AudioSource soundPrefab;
        [SerializeField] int amountToPool = 10;

        List<AudioSource> audioSources = new List<AudioSource>();

        void Awake()
        {
            if (Instance != null)
            {
                Debug.LogError("There's more than one SoundPool! " + transform + " - " + Instance);
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

        public AudioSource GetSoundFromPool(Sound sound)
        {
            for (int i = 0; i < audioSources.Count; i++)
            {
                if (audioSources[i].gameObject.activeSelf == false)
                {
                    if (sound != null)
                        sound.SetSource(audioSources[i]);
                    return audioSources[i];
                }
            }

            return CreateNewAudioSource(sound);
        }

        AudioSource CreateNewAudioSource(Sound sound)
        {
            AudioSource newAudioSource = Instantiate(soundPrefab, transform).GetComponent<AudioSource>();
            newAudioSource.outputAudioMixerGroup = AudioManager.Instance.masterAudioMixerGroup;

            if (sound != null)
                sound.SetSource(newAudioSource);

            audioSources.Add(newAudioSource);
            return newAudioSource;
        }
    }
}