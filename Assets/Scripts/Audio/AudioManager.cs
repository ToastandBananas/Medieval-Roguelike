using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;
using GridSystem;
using UnitSystem.ActionSystem;
using InventorySystem;
using UnitSystem;

namespace SoundSystem
{
    [System.Serializable]
    public class Sound
    {
        public string soundName;
        public AudioClip clip;

        [SerializeField][Range(0f, 2f)] float volume = 1f;
        [SerializeField][Range(0f, 2f)] float pitch = 1f;

        [SerializeField][Range(0f, 0.25f)] float randomVolumeAddOn = 0.25f;
        [SerializeField][Range(0f, 0.25f)] float randomPitchAddOn = 0.25f;

        [SerializeField] float soundRadius = 1f;
        [SerializeField] bool loop = false;

        AudioSource source;
        readonly Collider[] unitsInSoundRadius = new Collider[20];
        readonly RaycastHit[] hits = new RaycastHit[10];
        Ray ray = new();

        readonly float muffleAmountPerObstacle = 2f;

        public void SetSource(AudioSource _source)
        {
            source = _source;
            source.clip = clip;
            source.loop = loop;
        }

        public void Play(Vector3 soundPosition, Unit unitMakingSound, bool npcsShouldReact, float adjustedSoundRadius = -1f)
        {
            float distToPlayer = Vector3.Distance(soundPosition, UnitManager.player.WorldPosition);
            float actualSoundRadius = soundRadius;
            if (adjustedSoundRadius >= 0f)
                actualSoundRadius = adjustedSoundRadius;

            if (unitMakingSound.IsPlayer || distToPlayer - actualSoundRadius - UnitManager.player.Hearing.HearingRadius <= 0f)
            {
                source.volume = volume * (1f + Random.Range(-randomVolumeAddOn, randomVolumeAddOn));
                source.pitch = pitch * (1f + Random.Range(-randomPitchAddOn, randomPitchAddOn));

                float volumePercent = 1f;
                if (actualSoundRadius > 0f && distToPlayer - actualSoundRadius >= 0f)
                    volumePercent = Mathf.Clamp01(1f - ((distToPlayer - actualSoundRadius) / UnitManager.player.Hearing.HearingRadius));

                source.volume *= volumePercent;
                // Debug.Log("Volume: " + volume + " | Volume percent: " + volumePercent);
                source.Play();
            }

            // Should NPCs react to this sound? (Such as inspecting the sound or fleeing from the sound's origin)
            if (!npcsShouldReact)
                return;
            
            Physics.OverlapSphereNonAlloc(soundPosition, actualSoundRadius, unitsInSoundRadius, AudioManager.Instance.HearingMask);
            for (int i = 0; i < unitsInSoundRadius.Length; i++)
            {
                if (unitsInSoundRadius[i] == null)
                    continue;

                // Unit unit = LevelGrid.GetUnitAtGridPosition(LevelGrid.GetGridPosition(unitsInSoundRadius[i].transform.parent.parent.parent.parent.position));
                Unit unit = LevelGrid.GetUnitAtPosition(unitsInSoundRadius[i].transform.parent.parent.parent.parent.position);
                if (unit == null || unit.IsPlayer)
                    continue;

                if (unitMakingSound != null)
                { 
                    if (unitMakingSound == unit)
                        continue;

                    // We don't need NPCs inspecting sounds made by friendly or neutral units
                    if (!unit.Alliance.IsEnemy(unitMakingSound))
                        continue;
                }

                if (unit.StateController.CurrentState == GoalState.Fight)
                    continue;

                NPCActionHandler npcActionHandler = unit.UnitActionHandler as NPCActionHandler;
                if (unit.StateController.CurrentState == GoalState.InspectSound && npcActionHandler.GoalPlanner.InspectSoundAction.SoundGridPosition == LevelGrid.GetGridPosition(soundPosition))
                    continue;

                bool soundHeard = true;
                float distToUnit = Vector3.Distance(unit.WorldPosition, soundPosition);
                ray.origin = soundPosition;
                ray.direction = (unit.WorldPosition + (unit.ShoulderHeight * Vector3.up) - (soundPosition + (unit.ShoulderHeight * Vector3.up))).normalized;
                int hitCount = Physics.RaycastNonAlloc(ray, hits, distToUnit, unit.UnitActionHandler.AttackObstacleMask);
                
                // Muffle the sound radius through obstacles
                if (hitCount > 0)
                {
                    float soundMuffle = hitCount * muffleAmountPerObstacle;
                    float soundRadiusAfterMuffle = actualSoundRadius - soundMuffle;
                    if (soundRadiusAfterMuffle <= 0f) // If the sound was muffled so much that the Unit's hearing radius after muffling is 0, the sound is not heard
                        soundHeard = false;
                    else if (distToUnit > soundRadiusAfterMuffle) // If the sound position is not within the Unit's hearing radius after muffling
                    {
                        if (distToUnit - soundRadiusAfterMuffle - unit.Hearing.HearingRadius > 0f) // If the hearing radius after muffling and the sound radius no longer overlap, the sound is not heard
                            soundHeard = false;
                    }
                }

                if (soundHeard)
                {
                    Debug.Log(unit.name + " heard: " + soundName);
                    if (unitMakingSound != null && npcActionHandler.GoalPlanner.FleeAction != null && npcActionHandler.GoalPlanner.FleeAction.ShouldAlwaysFleeCombat)
                    {
                        // Is this sound closer than what the unit is already fleeing from (or are they not currently fleeing)?
                        if (unit.StateController.CurrentState != GoalState.Flee || Vector3.Distance(unit.transform.position, soundPosition) < Vector3.Distance(unit.transform.position, npcActionHandler.GoalPlanner.FleeAction.FleeFromPosition))
                            npcActionHandler.GoalPlanner.FleeAction.StartFlee(unitMakingSound, npcActionHandler.GoalPlanner.FleeAction.DefaultFleeDistance);
                    }
                    else if (npcActionHandler.GoalPlanner.InspectSoundAction != null)
                    {
                        npcActionHandler.GoalPlanner.InspectSoundAction.SetSoundGridPosition(soundPosition);
                        unit.StateController.SetCurrentState(GoalState.InspectSound);
                    }
                }
            }
        }

        public void Stop() => source.Stop();
    }

    public class AudioManager : MonoBehaviour
    {
        public static AudioManager Instance;

        [Header("Hearing Mask")]
        [SerializeField] LayerMask hearingMask;

        [Header("Master Audio Mixer:")]
        public AudioMixerGroup masterAudioMixerGroup;

        [Header("All Sounds")]
        public List<Sound[]> allSounds = new List<Sound[]>();

        [Header("Ambient Sounds")]
        public Sound[] windSounds;

        [Header("Inventory Sounds")]
        public Sound[] defaultPickUpSounds;
        public Sound[] clothingPickUpSounds;
        public Sound[] armorPickUpSounds;
        public Sound[] sharpWeaponPickUpSounds;
        public Sound[] bluntWeaponPickUpSounds;
        public Sound[] ringPickUpSounds;
        public Sound[] goldSounds;
        public Sound[] eatFoodSounds;
        public Sound[] drinkSounds;

        [Header("Human Sounds")]
        public Sound[] humanMaleGruntSounds;
        public Sound[] humanMaleDeathSounds;

        [Header("Footsteps")]
        public Sound[] footstepsStandard;
        public Sound[] footstepsStone;

        [Header("Door Sounds")]
        public Sound[] openDoorSounds;
        public Sound[] closeDoorSounds;

        [Header("Container Sounds")]
        public Sound[] chestSounds;
        public Sound[] searchBodySounds;

        [Header("Bow and Arrow Sounds")]
        public Sound[] arrowHitFleshSounds;
        public Sound[] arrowHitArmorSounds;
        public Sound[] arrowHitWallSounds;
        public Sound[] bowDrawSounds;
        public Sound[] bowReleaseSounds;

        [Header("Sword Sounds")]
        public Sound[] swordSlashSounds;
        public Sound[] swordStabSounds;
        public Sound[] swordSlashFleshSounds;
        public Sound[] swordSlashArmorSounds;
        public Sound[] swordStabFleshSounds;
        public Sound[] swordStabArmorSounds;

        [Header("Blunt Weapon Sounds")]
        public Sound[] bluntHitFleshSounds;
        public Sound[] bluntHitArmorSounds;

        Unit player;

        void Awake()
        {
            #region Singleton
            if (Instance != null)
            {
                if (Instance != this)
                    Destroy(this.gameObject);
            }
            else
            {
                Instance = this;
                DontDestroyOnLoad(this);
            }
            #endregion

            allSounds.Add(windSounds);
            allSounds.Add(defaultPickUpSounds);
            allSounds.Add(clothingPickUpSounds);
            allSounds.Add(armorPickUpSounds);
            allSounds.Add(sharpWeaponPickUpSounds);
            allSounds.Add(bluntWeaponPickUpSounds);
            allSounds.Add(ringPickUpSounds);
            allSounds.Add(goldSounds);
            allSounds.Add(eatFoodSounds);
            allSounds.Add(drinkSounds);
            allSounds.Add(humanMaleGruntSounds);
            allSounds.Add(humanMaleDeathSounds);
            allSounds.Add(footstepsStandard);
            allSounds.Add(footstepsStone);
            allSounds.Add(openDoorSounds);
            allSounds.Add(closeDoorSounds);
            allSounds.Add(chestSounds);
            allSounds.Add(searchBodySounds);
            allSounds.Add(arrowHitFleshSounds);
            allSounds.Add(arrowHitArmorSounds);
            allSounds.Add(arrowHitWallSounds);
            allSounds.Add(bowDrawSounds);
            allSounds.Add(bowReleaseSounds);
            allSounds.Add(swordSlashSounds);
            allSounds.Add(swordStabSounds);
            allSounds.Add(swordSlashFleshSounds);
            allSounds.Add(swordSlashArmorSounds);
            allSounds.Add(swordStabFleshSounds);
            allSounds.Add(swordStabArmorSounds);
            allSounds.Add(bluntHitFleshSounds);
            allSounds.Add(bluntHitArmorSounds);
        }

        void Start()
        {
            player = UnitManager.player;

            PlayAmbienceSound();
        }

        public static void PlaySound(Sound[] soundArray, string soundName, Vector3 soundPosition, Unit unitMakingSound, bool npcsShouldReact, float adjustedSoundRadius = -1f)
        {
            for (int i = 0; i < soundArray.Length; i++)
            {
                if (soundArray[i].soundName == soundName)
                {
                    AudioSource audioSource = Pool_Sounds.GetSoundFromPool(soundArray[i]);
                    audioSource.gameObject.SetActive(true);
                    soundArray[i].SetSource(audioSource);
                    soundArray[i].Play(soundPosition, unitMakingSound, npcsShouldReact, adjustedSoundRadius);

                    Instance.StartCoroutine(DelayDeactivateSound(audioSource));
                    return;
                }
            }

            // No sound with _soundName
            Debug.LogWarning("AudioManager: Sound not found in list: " + soundName);
        }

        static IEnumerator DelayDeactivateSound(AudioSource audioSource)
        {
            if (audioSource.clip == null)
            {
                yield return new WaitForSeconds(1f);
                audioSource.gameObject.SetActive(false);
                yield break;
            }

            yield return new WaitForSeconds(audioSource.clip.length);
            audioSource.gameObject.SetActive(false);
        }

        public static void PlayRandomSound(Sound[] soundArray, Vector3 soundPosition, Unit unitMakingSound, bool npcsShouldReact, float adjustedSoundRadius = -1f)
        {
            int randomIndex = Random.Range(0, soundArray.Length);
            for (int i = 0; i < soundArray.Length; i++)
            {
                if (soundArray[randomIndex] == soundArray[i])
                {
                    PlaySound(soundArray, soundArray[i].soundName, soundPosition, unitMakingSound, npcsShouldReact, adjustedSoundRadius);
                    return;
                }
            }
        }

        public static void StopSound(Sound[] soundArray, string _soundName)
        {
            for (int i = 0; i < soundArray.Length; i++)
            {
                if (soundArray[i].soundName == _soundName)
                {
                    soundArray[i].Stop();
                    return;
                }
            }

            // No sound with _soundName
            Debug.LogWarning("AudioManager: Sound not found in list: " + _soundName);
        }

        void PlayAmbienceSound()
        {
            int randomIndex = Random.Range(0, windSounds.Length);
            for (int i = 0; i < windSounds.Length; i++)
            {
                if (windSounds[randomIndex] == windSounds[i])
                {
                    PlaySound(windSounds, windSounds[i].soundName, player.transform.position, null, false);

                    // Play another ambience sound after this clip finishes
                    Invoke("PlayAmbienceSound", windSounds[i].clip.length);
                    return;
                }
            }
        }

        public void PlayPickUpItemSound(Item item)
        {
            bool soundFound = false;

            /*if (item.itemType == ItemType.Weapon)
            {
                Equipment equipment = (Equipment)item;
                if (equipment.weaponType == WeaponType.Sword)
                {
                    PlayRandomSound(sharpWeaponPickUpSounds, player.transform.position);
                    soundFound = true;
                }
                else if (equipment.weaponType == WeaponType.Mace || equipment.weaponType == WeaponType.Staff || equipment.weaponType == WeaponType.Spear || equipment.weaponType == WeaponType.Axe)
                {
                    PlayRandomSound(bluntWeaponPickUpSounds, player.transform.position);
                    soundFound = true;
                }
            }

            if (item.itemType == ItemType.Armor)
            {
                Equipment equipment = (Equipment)item;
                if (equipment.armorType == ArmorType.Shirt || equipment.armorType == ArmorType.Pants || equipment.armorType == ArmorType.Belt)
                {
                    PlayRandomSound(clothingPickUpSounds, player.transform.position);
                    soundFound = true;
                }
                else
                {
                    PlayRandomSound(armorPickUpSounds, player.transform.position);
                    soundFound = true;
                }
            }

            if (item.itemType == ItemType.Consumable)
            {
                Consumable consumable = (Consumable)item;
                if (consumable.consumableType == ConsumableType.Drink)
                {
                    PlayRandomSound(drinkSounds, player.transform.position);
                    soundFound = true;
                }
            }*/

            if (!soundFound)
                PlayRandomSound(defaultPickUpSounds, player.transform.position, null, false);
        }

        public static void PlayFootstepSound(Unit unit, float soundRadius)
        {
            PlayRandomSound(Instance.footstepsStandard, unit.WorldPosition, unit, true, soundRadius);
        }

        public void PlayPickUpGoldSound(int goldAmount)
        {
            if (goldAmount <= 10)
                PlaySound(goldSounds, goldSounds[0].soundName, player.transform.position, null, false);
            else if (goldAmount > 10 && goldAmount <= 50)
            {
                int randomNum = Random.Range(1, 3);
                if (randomNum == 1)
                    PlaySound(goldSounds, goldSounds[1].soundName, player.transform.position, null, false);
                else
                    PlaySound(goldSounds, goldSounds[2].soundName, player.transform.position, null, false);
            }
            else if (goldAmount > 50 && goldAmount <= 100)
                PlaySound(goldSounds, goldSounds[3].soundName, player.transform.position, null, false);
            else
                PlaySound(goldSounds, goldSounds[4].soundName, player.transform.position, null, false);
        }

        public void PlayDamageSound()
        {
            /*
            if (arms.currentAttackType == AttackType.Slash)
                PlayRandomSound(swordSlashFleshSounds, arms.transform.position);
            else if (arms.currentAttackType == AttackType.Thrust)
                PlayRandomSound(swordStabFleshSounds, arms.transform.position);
            else if (arms.currentAttackType == AttackType.Blunt)
                PlayRandomSound(bluntHitFleshSounds, arms.transform.position);
            */
        }

        public LayerMask HearingMask => hearingMask;
    }
}