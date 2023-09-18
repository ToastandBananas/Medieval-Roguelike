using System.Collections.Generic;
using UnityEngine;

public class InventorySlotPool : MonoBehaviour
{
    public static InventorySlotPool Instance;

    [SerializeField] InventorySlot inventorySlotPrefab;
    [SerializeField] int amountToPool = 80;

    List<InventorySlot> slots = new List<InventorySlot>();

    void Awake()
    {
        if (Instance != null)
        {
            Debug.LogError("There's more than one InventorySlotPool! " + transform + " - " + Instance);
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    void Start()
    {
        for (int i = 0; i < amountToPool; i++)
        {
            InventorySlot newSlot = CreateNewSlot();
            newSlot.gameObject.SetActive(false);
        }
    }

    public InventorySlot GetSlotFromPool()
    {
        for (int i = 0; i < slots.Count; i++)
        {
            if (slots[i].gameObject.activeSelf == false)
                return slots[i];
        }

        return CreateNewSlot();
    }

    InventorySlot CreateNewSlot()
    {
        InventorySlot newSlot = Instantiate(inventorySlotPrefab, transform).GetComponent<InventorySlot>();
        slots.Add(newSlot);
        return newSlot;
    }

    public void ReturnToPool(InventorySlot slot)
    {
        if (slot.ParentSlot() != null)
            slot.ParentSlot().ClearSlotVisuals();

        slot.SetMyInventory(null);
        slot.transform.SetParent(transform);
        slot.gameObject.SetActive(false);
    }
}
