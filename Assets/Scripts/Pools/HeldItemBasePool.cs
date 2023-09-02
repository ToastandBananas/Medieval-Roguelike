using System.Collections.Generic;
using UnityEngine;

public class HeldItemBasePool : MonoBehaviour
{
    public static HeldItemBasePool Instance;

    [Header("Parent Transforms")]
    [SerializeField] Transform meleeWeaponsParent;
    [SerializeField] Transform rangedWeaponsParent;
    [SerializeField] Transform shieldsParent;

    [Header("Prefabs")]
    [SerializeField] int meleeWeaponBasesToPool = 40;
    [SerializeField] HeldMeleeWeapon meleeWeaponBasePrefab;
    [SerializeField] int rangedWeaponBasesToPool = 40;
    [SerializeField] HeldRangedWeapon rangedWeaponBasePrefab;
    [SerializeField] int shieldBasesToPool = 40;
    [SerializeField] HeldShield shieldBasePrefab;

    List<HeldMeleeWeapon> meleeWeaponBases = new List<HeldMeleeWeapon>();
    List<HeldRangedWeapon> rangedWeaponBases = new List<HeldRangedWeapon>();
    List<HeldShield> shieldBases = new List<HeldShield>();

    void Awake()
    {
        foreach (HeldMeleeWeapon meleeWeaponBase in FindObjectsOfType<HeldMeleeWeapon>())
        {
            meleeWeaponBases.Add(meleeWeaponBase);
            meleeWeaponBase.transform.parent = transform;
        }

        foreach (HeldRangedWeapon rangedWeaponBase in FindObjectsOfType<HeldRangedWeapon>())
        {
            rangedWeaponBases.Add(rangedWeaponBase);
            rangedWeaponBase.transform.parent = transform;
        }

        foreach (HeldShield shieldBase in FindObjectsOfType<HeldShield>())
        {
            shieldBases.Add(shieldBase);
            shieldBase.transform.parent = transform;
        }

        if (Instance != null)
        {
            Debug.LogError("There's more than one HeldItemBasePool! " + transform + " - " + Instance);
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    void Start()
    {
        for (int i = 0; i < meleeWeaponBasesToPool; i++)
        {
            HeldMeleeWeapon newMeleeWeaponBase = CreateNewMeleeWeaponBase();
            newMeleeWeaponBase.gameObject.SetActive(false);
        }

        for (int i = 0; i < rangedWeaponBasesToPool; i++)
        {
            HeldRangedWeapon newRangedWeaponBase = CreateNewRangedWeaponBase();
            newRangedWeaponBase.gameObject.SetActive(false);
        }

        for (int i = 0; i < shieldBasesToPool; i++)
        {
            HeldShield newShieldBase = CreateNewShieldBase();
            newShieldBase.gameObject.SetActive(false);
        }
    }

    public HeldMeleeWeapon GetMeleeWeaponBaseFromPool()
    {
        for (int i = 0; i < meleeWeaponBases.Count; i++)
        {
            if (meleeWeaponBases[i].gameObject.activeSelf == false)
                return meleeWeaponBases[i];
        }

        return CreateNewMeleeWeaponBase();
    }

    public HeldRangedWeapon GetRangedWeaponBaseFromPool()
    {
        for (int i = 0; i < rangedWeaponBases.Count; i++)
        {
            if (rangedWeaponBases[i].gameObject.activeSelf == false)
                return rangedWeaponBases[i];
        }

        return CreateNewRangedWeaponBase();
    }

    public HeldShield GetShieldBaseFromPool()
    {
        for (int i = 0; i < shieldBases.Count; i++)
        {
            if (shieldBases[i].gameObject.activeSelf == false)
                return shieldBases[i];
        }

        return CreateNewShieldBase();
    }

    HeldMeleeWeapon CreateNewMeleeWeaponBase()
    {
        HeldMeleeWeapon newMeleeWeaponBase = Instantiate(meleeWeaponBasePrefab, meleeWeaponsParent).GetComponent<HeldMeleeWeapon>();
        meleeWeaponBases.Add(newMeleeWeaponBase);
        return newMeleeWeaponBase;
    }

    HeldRangedWeapon CreateNewRangedWeaponBase()
    {
        HeldRangedWeapon newRangedWeaponBase = Instantiate(rangedWeaponBasePrefab, rangedWeaponsParent).GetComponent<HeldRangedWeapon>();
        rangedWeaponBases.Add(newRangedWeaponBase);
        return newRangedWeaponBase;
    }

    HeldShield CreateNewShieldBase()
    {
        HeldShield newShieldBase = Instantiate(shieldBasePrefab, shieldsParent).GetComponent<HeldShield>();
        shieldBases.Add(newShieldBase);
        return newShieldBase;
    }
}