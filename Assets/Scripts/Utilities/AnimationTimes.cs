using System.Collections;
using UnityEngine;

public class AnimationTimes : MonoBehaviour
{
    [SerializeField] RuntimeAnimatorController bowController;
    [SerializeField] RuntimeAnimatorController meleeWeaponController;
    [SerializeField] RuntimeAnimatorController unitController;

    // Bow Clips
    AnimationClip[] bowClips;
    public float shoot_Time { get; private set; }

    // Melee Weapon Clips
    AnimationClip[] meleeWeaponClips;
    public float humanAttack_1H_Time { get; private set; }

    // Unit Clips
    AnimationClip[] unitClips;
    public float dualWieldAttack_Time { get; private set; }

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

    public float GetWeaponAttackAnimationTime(Weapon weapon)
    {
        if (weapon.IsMeleeWeapon())
        {
            if (weapon.isTwoHanded == false)
                return humanAttack_1H_Time;
        }
        else if (weapon.IsRangedWeapon())
        {
            return shoot_Time;
        }

        return 0.1f;
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
                    shoot_Time = clip.length;
                    break;
                default:
                    break;
            }
        }

        foreach (AnimationClip clip in meleeWeaponClips)
        {
            switch (clip.name)
            {
                case "Attack_1H_R":
                    humanAttack_1H_Time = clip.length;
                    break;
                default:
                    break;
            }
        }

        foreach (AnimationClip clip in unitClips)
        {
            switch (clip.name)
            {
                case "DualMeleeAttack":
                    dualWieldAttack_Time = clip.length;
                    break;
                default:
                    break;
            }
        }
    }
}
