using System.Collections.Generic;
using UnityEngine;

namespace InventorySystem
{
    public class HeldItemBasePool : MonoBehaviour
    {
        public static HeldItemBasePool Instance;

        [Header("Parent Transforms")]
        [SerializeField] Transform meleeWeaponsParent;
        [SerializeField] Transform rangedWeaponsParent;
        [SerializeField] Transform shieldsParent;

        [Header("Prefabs")]
        [SerializeField] int meleeWeaponBasesToPool = 5;
        [SerializeField] HeldMeleeWeapon meleeWeaponBasePrefab;
        [SerializeField] int rangedWeaponBasesToPool = 5;
        [SerializeField] HeldRangedWeapon rangedWeaponBasePrefab;
        [SerializeField] int shieldBasesToPool = 5;
        [SerializeField] HeldShield shieldBasePrefab;

        List<HeldMeleeWeapon> meleeWeaponBases = new List<HeldMeleeWeapon>();
        List<HeldRangedWeapon> rangedWeaponBases = new List<HeldRangedWeapon>();
        List<HeldShield> shieldBases = new List<HeldShield>();

        void Awake()
        {
            foreach (HeldMeleeWeapon meleeWeaponBase in FindObjectsOfType<HeldMeleeWeapon>())
            {
                meleeWeaponBases.Add(meleeWeaponBase);
                meleeWeaponBase.transform.SetParent(transform);
                meleeWeaponBase.gameObject.SetActive(false);
            }

            foreach (HeldRangedWeapon rangedWeaponBase in FindObjectsOfType<HeldRangedWeapon>())
            {
                rangedWeaponBases.Add(rangedWeaponBase);
                rangedWeaponBase.transform.SetParent(transform);
                rangedWeaponBase.gameObject.SetActive(false);
            }

            foreach (HeldShield shieldBase in FindObjectsOfType<HeldShield>())
            {
                shieldBases.Add(shieldBase);
                shieldBase.transform.SetParent(transform);
                shieldBase.gameObject.SetActive(false);
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

        public static void ReturnToPool(HeldItem heldItem)
        {
            if (heldItem is HeldMeleeWeapon)
                heldItem.transform.SetParent(Instance.meleeWeaponsParent);
            else if (heldItem is HeldRangedWeapon)
                heldItem.transform.SetParent(Instance.rangedWeaponsParent);
            else if (heldItem is HeldShield)
                heldItem.transform.SetParent(Instance.shieldsParent);

            heldItem.gameObject.SetActive(false);
        }
    }
}
