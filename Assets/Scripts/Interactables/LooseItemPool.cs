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
        [SerializeField] Interactable_LooseItem looseItemPrefab;
        [SerializeField] int amountLooseItemsToPool = 10;

        [Header("Loose Container Item")]
        [SerializeField] Transform looseContainerItemParent;
        [SerializeField] Interactable_LooseItem looseContainerItemPrefab;
        [SerializeField] int amountLooseContainerItemsToPool = 2;

        [Header("Loose Quivers")]
        [SerializeField] Transform looseQuiverItemParent;
        [SerializeField] Interactable_LooseItem looseQuiverPrefab;
        [SerializeField] int amountLooseQuiversToPool = 1;

        List<Interactable_LooseItem> looseItems = new List<Interactable_LooseItem>();
        List<Interactable_LooseItem> looseContainerItems = new List<Interactable_LooseItem>();
        List<Interactable_LooseItem> looseQuivers = new List<Interactable_LooseItem>();

        void Awake()
        {
            if (Instance != null)
            {
                Debug.LogError("There's more than one LooseItemPool! " + transform + " - " + Instance);
                Destroy(gameObject);
                return;
            }
            Instance = this;

            foreach (Interactable_LooseItem looseItem in FindObjectsOfType<Interactable_LooseItem>())
            {
                if (looseItem is LooseQuiverItem)
                {
                    looseQuivers.Add(looseItem);
                    looseItem.transform.SetParent(looseQuiverItemParent);
                }
                else if (looseItem is Interactable_LooseContainerItem)
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
                Interactable_LooseItem newLooseItem = CreateNewLooseItem();
                newLooseItem.gameObject.SetActive(false);
            }

            for (int i = 0; i < amountLooseContainerItemsToPool; i++)
            {
                Interactable_LooseItem newLooseItem = CreateNewLooseContainerItem();
                newLooseItem.gameObject.SetActive(false);
            }

            for (int i = 0; i < amountLooseQuiversToPool; i++)
            {
                Interactable_LooseItem newLooseItem = CreateNewLooseQuiver();
                newLooseItem.gameObject.SetActive(false);
            }
        }

        public Interactable_LooseItem GetLooseItemFromPool()
        {
            for (int i = 0; i < looseItems.Count; i++)
            {
                if (looseItems[i].gameObject.activeSelf == false)
                    return looseItems[i];
            }

            return CreateNewLooseItem();
        }

        Interactable_LooseItem CreateNewLooseItem()
        {
            Interactable_LooseItem newLooseItem = Instantiate(looseItemPrefab, looseItemParent).GetComponent<Interactable_LooseItem>();
            looseItems.Add(newLooseItem);
            return newLooseItem;
        }

        public Interactable_LooseItem GetLooseContainerItemFromPool()
        {
            for (int i = 0; i < looseContainerItems.Count; i++)
            {
                if (looseContainerItems[i].gameObject.activeSelf == false)
                    return looseContainerItems[i];
            }

            return CreateNewLooseContainerItem();
        }

        Interactable_LooseItem CreateNewLooseContainerItem()
        {
            Interactable_LooseItem newLooseContainerItem = Instantiate(looseContainerItemPrefab, looseContainerItemParent).GetComponent<Interactable_LooseItem>();
            looseContainerItems.Add(newLooseContainerItem);
            return newLooseContainerItem;
        }

        public Interactable_LooseItem GetLooseQuiverItemFromPool()
        {
            for (int i = 0; i < looseQuivers.Count; i++)
            {
                if (looseQuivers[i].gameObject.activeSelf == false)
                    return looseQuivers[i];
            }

            return CreateNewLooseQuiver();
        }

        Interactable_LooseItem CreateNewLooseQuiver()
        {
            Interactable_LooseItem newLooseQuiver = Instantiate(looseQuiverPrefab, looseQuiverItemParent).GetComponent<Interactable_LooseItem>();
            looseQuivers.Add(newLooseQuiver);
            return newLooseQuiver;
        }

        public static void ReturnToPool(Interactable_LooseItem looseItem)
        {
            if (looseItem is LooseQuiverItem)
            {
                LooseQuiverItem looseQuiver = (LooseQuiverItem)looseItem;
                if (looseQuiver.ContainerInventoryManager.ParentInventory.SlotVisualsCreated)
                    InventoryUI.GetContainerUI(looseQuiver.ContainerInventoryManager).CloseContainerInventory();

                looseQuiver.transform.SetParent(Instance.looseQuiverItemParent);
                looseQuiver.HideArrowMeshes();
            }
            else if (looseItem is Interactable_LooseContainerItem)
            {
                Interactable_LooseContainerItem looseContainerItem = (Interactable_LooseContainerItem)looseItem;
                if (looseContainerItem.ContainerInventoryManager.ParentInventory.SlotVisualsCreated)
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
