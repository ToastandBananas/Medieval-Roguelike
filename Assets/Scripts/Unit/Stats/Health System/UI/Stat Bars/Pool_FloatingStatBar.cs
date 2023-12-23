using System.Collections.Generic;
using UnityEngine;

namespace UnitSystem.UI
{
    public class Pool_FloatingStatBar : MonoBehaviour
    {
        public static Pool_FloatingStatBar Instance;

        [SerializeField] StatBarManager_Floating floatingStatBarsPrefab;
        [SerializeField] int amountToPool = 2;

        public readonly static List<StatBarManager_Floating> floatingStatBars = new();

        void Awake()
        {
            if (Instance != null)
            {
                Debug.LogError("There's more than one Pool_FloatingStatBar! " + transform + " - " + Instance);
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        void Start()
        {
            for (int i = 0; i < amountToPool; i++)
            {
                StatBarManager_Floating newFloatingStatBars = CreateNewFloatingStatBars();
                newFloatingStatBars.gameObject.SetActive(false);
            }
        }

        public static StatBarManager_Floating GetFloatingStatBarsFromPool()
        {
            for (int i = 0; i < floatingStatBars.Count; i++)
            {
                if (!floatingStatBars[i].gameObject.activeSelf)
                    return floatingStatBars[i];
            }

            return CreateNewFloatingStatBars();
        }

        static StatBarManager_Floating CreateNewFloatingStatBars()
        {
            StatBarManager_Floating newFloatingStatBars = Instantiate(Instance.floatingStatBarsPrefab, Instance.transform).GetComponent<StatBarManager_Floating>();
            floatingStatBars.Add(newFloatingStatBars);
            return newFloatingStatBars;
        }

        public static void ReturnToPool(StatBarManager_Floating floatingStatBars)
        {
            floatingStatBars.transform.SetParent(Instance.transform);
            floatingStatBars.gameObject.SetActive(false);
        }
    }
}
