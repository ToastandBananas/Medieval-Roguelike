using System.Collections.Generic;
using UnityEngine;
using InventorySystem;
using GeneralUI;

namespace InteractableObjects
{
    public class LooseItemPool : MonoBehaviour
    {
        public static LooseItemPool Instance;

        [Header("Default Loose Item")]
        [SerializeField] Transform looseItemParent;
        [SerializeField] LooseItem looseItemPrefab;
        [SerializeField] int amountLooseItemsToPool = 10;

        [Header("Loose Container Item")]
        [SerializeField] Transform looseContainerItemParent;
        [SerializeField] LooseItem looseContainerItemPrefab;
        [SerializeField] int amountLooseContainerItemsToPool = 2;

        [Header("Loose Quivers")]
        [SerializeField] Transform looseQuiverItemParent;
        [SerializeField] LooseItem looseQuiverPrefab;
        [SerializeField] int amountLooseQuiversToPool = 1;

        List<LooseItem> looseItems = new List<LooseItem>();
        List<LooseItem> looseContainerItems = new List<LooseItem>();
        List<LooseItem> looseQuivers = new List<LooseItem>();

        void Awake()
        {
            if (Instance != null)
            {
                Debug.LogError("There's more than one LooseItemPool! " + transform + " - " + Instance);
                Destroy(gameObject);
                return;
            }
            Instance = this;

            foreach (LooseItem looseItem in FindObjectsOfType<LooseItem>())
            {
                if (looseItem is LooseQuiverItem)
                {
                    looseQuivers.Add(looseItem);
                    looseItem.transform.SetParent(looseQuiverItemParent);
                }
                else if (looseItem is LooseContainerItem)
                {
                    looseContainerItems.Add(looseItem);
                    looseItem.transform.SetParent(looseContainerItemParent);
                }
                else
                {
                    looseItems.Add(looseItem);
                    looseItem.transform.SetParent(looseItemParent);
                }
            }
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

            for (int i = 0; i < amountLooseQuiversToPool; i++)
            {
                LooseItem newLooseItem = CreateNewLooseQuiver();
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
            LooseItem newLooseItem = Instantiate(looseItemPrefab, looseItemParent).GetComponent<LooseItem>();
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
            LooseItem newLooseContainerItem = Instantiate(looseContainerItemPrefab, looseContainerItemParent).GetComponent<LooseItem>();
            looseContainerItems.Add(newLooseContainerItem);
            return newLooseContainerItem;
        }

        public LooseItem GetLooseQuiverItemFromPool()
        {
            for (int i = 0; i < looseQuivers.Count; i++)
            {
                if (looseQuivers[i].gameObject.activeSelf == false)
                    return looseQuivers[i];
            }

            return CreateNewLooseQuiver();
        }

        LooseItem CreateNewLooseQuiver()
        {
            LooseItem newLooseQuiver = Instantiate(looseQuiverPrefab, looseQuiverItemParent).GetComponent<LooseItem>();
            looseQuivers.Add(newLooseQuiver);
            return newLooseQuiver;
        }

        public static void ReturnToPool(LooseItem looseItem)
        {
            if (looseItem is LooseQuiverItem)
            {
                LooseQuiverItem looseQuiver = (LooseQuiverItem)looseItem;
                if (looseQuiver.ContainerInventoryManager.ParentInventory.slotVisualsCreated)
                    InventoryUI.GetContainerUI(looseQuiver.ContainerInventoryManager).CloseContainerInventory();

                looseQuiver.transform.SetParent(Instance.looseQuiverItemParent);
                looseQuiver.HideArrowMeshes();
            }
            else if (looseItem is LooseContainerItem)
            {
                LooseContainerItem looseContainerItem = (LooseContainerItem)looseItem;
                if (looseContainerItem.ContainerInventoryManager.ParentInventory.slotVisualsCreated)
                    InventoryUI.GetContainerUI(looseContainerItem.ContainerInventoryManager).CloseContainerInventory();

                looseContainerItem.transform.SetParent(Instance.looseContainerItemParent);
            }
            else
                looseItem.transform.SetParent(Instance.looseItemParent);

            looseItem.RigidBody.isKinematic = false;
            looseItem.RigidBody.useGravity = true;
            looseItem.MeshCollider.enabled = true;
            looseItem.MeshCollider.isTrigger = false;
            looseItem.SetItemData(null);
            looseItem.gameObject.SetActive(false);

            TooltipManager.UpdateLooseItemTooltips();
        }

        public Transform LooseItemParent => looseItemParent;
    }
}
