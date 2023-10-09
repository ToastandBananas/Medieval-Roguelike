using System.Collections;
using UnityEngine;
using InventorySystem;

namespace Utilities
{
    public class AnimationTimes : MonoBehaviour
    {
        [SerializeField] RuntimeAnimatorController bowController;
        [SerializeField] RuntimeAnimatorController meleeWeaponController;
        [SerializeField] RuntimeAnimatorController unitController;

        // Bow Clips
        AnimationClip[] bowClips;
        float defaultShoot_Time;

        // Melee Weapon Clips
        AnimationClip[] meleeWeaponClips;
        float defaultAttack_1H_Time;
        float defaultAttack_2H_Time;
        float swipeAttack_2H_Time;

        // Unit Clips
        AnimationClip[] unitClips;
        float die_Time;
        float dualWieldAttack_Time;
        float unarmedAttack_Time;

        #region Singleton
        public static AnimationTimes Instance;

        void Awake()
        {
            if (Instance != null)
            {
                if (Instance != this)
                {
                    Debug.LogWarning("More than one Instance of AnimationTimes. Fix me!");
                    Destroy(gameObject);
                }
            }
            else
                Instance = this;
        }
        #endregion

        void Start()
        {
            StartCoroutine(UpdateAnimClipTimes());
        }

        public IEnumerator UpdateAnimClipTimes()
        {
            yield return new WaitForSeconds(0.1f);

            bowClips = bowController.animationClips;
            meleeWeaponClips = meleeWeaponController.animationClips;
            unitClips = unitController.animationClips;

            foreach (AnimationClip clip in bowClips)
            {
                switch (clip.name)
                {
                    case "Shoot":
                        defaultShoot_Time = clip.length;
                        break;
                    default:
                        break;
                }
            }

            foreach (AnimationClip clip in meleeWeaponClips)
            {
                switch (clip.name)
                {
                    case "DefaultAttack_1H_R":
                        defaultAttack_1H_Time = clip.length;
                        break;
                    case "DefaultAttack_1H_L":
                        defaultAttack_1H_Time = clip.length;
                        break;
                    case "DefaultAttack_2H":
                        defaultAttack_2H_Time = clip.length;
                        break;
                    case "SwipeAttack_2H":
                        swipeAttack_2H_Time = clip.length;
                        break;
                    default:
                        break;
                }
            }

            foreach (AnimationClip clip in unitClips)
            {
                switch (clip.name)
                {
                    case "Die_Forward":
                        die_Time = clip.length;
                        break;
                    case "DualMeleeAttack":
                        dualWieldAttack_Time = clip.length;
                        break;
                    case "MeleeAttack":
                        unarmedAttack_Time = clip.length;
                        break;
                    default:
                        break;
                }
            }
        }

        public float DefaultWeaponAttackTime(Weapon weapon)
        {
            if (weapon is MeleeWeapon)
            {
                if (weapon.IsTwoHanded)
                    return defaultAttack_2H_Time;
                else
                    return defaultAttack_1H_Time;
            }
            else if (weapon is RangedWeapon)
            {
                return defaultShoot_Time;
            }

            return 0.25f;
        }

        public float DualWieldAttackTime() => dualWieldAttack_Time;

        public float SwipeAttackTime() => swipeAttack_2H_Time;

        public float UnarmedAttackTime() => unarmedAttack_Time;

        public float DeathTime() => die_Time;
    }
}
