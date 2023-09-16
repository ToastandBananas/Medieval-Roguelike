using System.Collections.Generic;
using UnityEngine;

public class LooseItemPool : MonoBehaviour
{
    public static LooseItemPool Instance;

    [Header("Default Loose Item")]
    [SerializeField] LooseItem looseItemPrefab;
    [SerializeField] int amountLooseItemsToPool = 40;

    [Header("Loose Container Item")]
    [SerializeField] LooseItem looseContainerItemPrefab;
    [SerializeField] int amountLooseContainerItemsToPool = 3;

    List<LooseItem> looseItems = new List<LooseItem>();
    List<LooseItem> looseContainerItems = new List<LooseItem>();

    void Awake()
    {
        foreach(LooseItem looseItem in FindObjectsOfType<LooseItem>())
        {
            if (looseItem.ItemData.Item.IsBag() || looseItem.ItemData.Item.IsPortableContainer())
                looseContainerItems.Add(looseItem);
            else
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
        for (int i = 0; i < amountLooseItemsToPool; i++)
        {
            LooseItem newLooseItem = CreateNewLooseItem();
            newLooseItem.gameObject.SetActive(false);
        }

        for (int i = 0; i < amountLooseContainerItemsToPool; i++)
        {
            LooseItem newLooseItem = CreateNewLooseContainerItem();
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

    public LooseItem GetLooseContainerItemFromPool()
    {
        for (int i = 0; i < looseContainerItems.Count; i++)
        {
            if (looseContainerItems[i].gameObject.activeSelf == false)
                return looseContainerItems[i];
        }

        return CreateNewLooseContainerItem();
    }

    LooseItem CreateNewLooseContainerItem()
    {
        LooseItem newLooseContainerItem = Instantiate(looseContainerItemPrefab, transform).GetComponent<LooseItem>();
        looseContainerItems.Add(newLooseContainerItem);
        return newLooseContainerItem;
    }
}
