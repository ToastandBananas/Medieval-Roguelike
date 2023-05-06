using System.Collections.Generic;
using UnityEngine;

public class LooseItemPool : MonoBehaviour
{
    public static LooseItemPool Instance;

    [SerializeField] LooseItem looseItemPrefab;
    [SerializeField] int amountToPool = 40;

    List<LooseItem> looseItems = new List<LooseItem>();

    void Awake()
    {
        foreach(LooseItem looseItem in FindObjectsOfType<LooseItem>())
        {
            looseItems.Add(looseItem);
            looseItem.transform.parent = transform;
        }

        if (Instance != null)
        {
            Debug.LogError("There's more than one LooseItemPool! " + transform + " - " + Instance);
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    void Start()
    {
        for (int i = 0; i < amountToPool; i++)
        {
            LooseItem newLooseItem = CreateNewLooseItem();
            newLooseItem.gameObject.SetActive(false);
        }
    }

    public LooseItem GetLooseItemFromPool()
    {
        for (int i = 0; i < looseItems.Count; i++)
        {
            if (looseItems[i].gameObject.activeSelf == false)
                return looseItems[i];
        }

        return CreateNewLooseItem();
    }

    LooseItem CreateNewLooseItem()
    {
        LooseItem newLooseItem = Instantiate(looseItemPrefab, transform).GetComponent<LooseItem>();
        looseItems.Add(newLooseItem);
        return newLooseItem;
    }
}
