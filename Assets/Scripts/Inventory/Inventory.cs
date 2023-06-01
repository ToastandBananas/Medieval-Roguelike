using System.Collections.Generic;
using UnityEngine;

public class Inventory : MonoBehaviour
{
    [SerializeField] Transform slotsParent;

    [Header("Slot Counts")]
    [SerializeField] int amountOfSlots = 24;
    [SerializeField] int maxSlots = 24;
    [SerializeField] int maxSlotsPerRow = 12;
    int maxSlotsPerColumn;

    [Header("Items in Inventory")]
    [SerializeField] List<ItemData> itemDatas = new List<ItemData>();

    public List<InventorySlot> slots { get; private set; }

    void Awake()
    {
        maxSlotsPerColumn = Mathf.CeilToInt(maxSlots / maxSlotsPerRow);

        slots = new List<InventorySlot>();

        for (int i = 0; i < amountOfSlots; i++)
        {
            InventorySlot newSlot = Instantiate(InventoryUI.Instance.InventorySlotPrefab(), slotsParent);
            newSlot.SetSlotCoordinate(new Vector2((i % maxSlotsPerRow) + 1, Mathf.FloorToInt(i / maxSlotsPerRow) + 1));
            newSlot.name = $"Slot - {newSlot.slotCoordinate}";
            newSlot.SetMyInventory(this);
            slots.Add(newSlot);

            // Debug.Log(newSlot.name + ": " + newSlot.slotCoordinate);

            if (i == maxSlots - 1)
                break;
        }

        for (int i = 0; i < itemDatas.Count; i++)
        {
            if (itemDatas[i].Item() == null)
                continue;

            if (itemDatas[i].HasBeenInitialized() == false)
                itemDatas[i].InitializeData();

            if (AddItem(itemDatas[i]) == false)
                Debug.LogError($"{itemDatas[i].Item().name} can't fit in {name} inventory...");
        }
    }

    public bool AddItem(ItemData newItemData)
    {
        if (newItemData.Item() != null)
        {
            InventorySlot slot = GetNextAvailableInventorySlot(newItemData);
            if (slot != null)
            {
                slot.InventoryItem().SetItemData(newItemData);
                slot.ShowSlotImage();
                slot.SetupParentSlot();
                return true;
            }
        }
        return false;
    }

    InventorySlot GetNextAvailableInventorySlot(ItemData itemData)
    {
        int width = itemData.Item().width;
        int height = itemData.Item().height;

        for (int i = 0; i < slots.Count; i++)
        {
            if (slots[i].IsFull())
                continue;

            bool isAvailable = true;
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    InventorySlot slotToCheck = GetSlotFromCoordinate(new Vector2(slots[i].slotCoordinate.x - x, slots[i].slotCoordinate.y - y));
                    if (slotToCheck == null || slotToCheck.IsFull())
                    {
                        isAvailable = false;
                        break;
                    }

                    if (isAvailable == false)
                        break;
                }
            }

            if (isAvailable)
            {
                // Debug.Log(slots[i].name + " is available to place " + itemData.Item().name + " in " + name);
                return slots[i];
            }
        }
        return null;
    }

    public InventorySlot GetSlotFromCoordinate(Vector2 slotCoordinate)
    {
        if (slotCoordinate.x <= 0 || slotCoordinate.y <= 0)
            return null;

        for (int i = 0; i < slots.Count; i++)
        {
            if (slots[i].slotCoordinate == slotCoordinate)
                return slots[i];
        }
        Debug.LogWarning("Invalid slot coordinate");
        return null;
    }

    public bool ContainsItem(Item item)
    {
        for (int i = 0; i < itemDatas.Count; i++)
        {
            if (itemDatas[i].Item() == item)
                return true;
        }
        return false;
    }

    public List<ItemData> ItemDatas() => itemDatas;
}
