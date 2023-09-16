using UnityEngine;

public class SlotCoordinate
{
    public string name { get; private set; }

    public Vector2Int coordinate { get; private set; }
    public SlotCoordinate parentSlotCoordinate { get; private set; }
    public Inventory myInventory { get; private set; }
    public ItemData itemData { get; private set; }
    public bool isFull { get; private set; }

    public void SetIsFull(bool isFull) => this.isFull = isFull;

    public void SetSlotCoordinate(int xCoord, int yCoord)
    {
        coordinate = new Vector2Int(xCoord, yCoord);
        name = $"({xCoord}, {yCoord})";
    }

    public void SetupNewItem(ItemData newItemData)
    {
        itemData = newItemData;
        itemData.SetInventorySlotCoordinate(this);

        int width = itemData.Item.width;
        int height = itemData.Item.height;
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                SlotCoordinate slotCoordinateToSetup = myInventory.GetSlotCoordinate(coordinate.x - x, coordinate.y - y);
                slotCoordinateToSetup.SetParentSlotCoordinate(this);
                slotCoordinateToSetup.isFull = true;
            }
        }
    }

    public void ClearItem()
    {
        Inventory inventory = myInventory;
        int width = parentSlotCoordinate.itemData.Item.width;
        int height = parentSlotCoordinate.itemData.Item.height;
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                SlotCoordinate slotCoordinateToSetup = inventory.GetSlotCoordinate(coordinate.x - x, coordinate.y - y);
                slotCoordinateToSetup.SetParentSlotCoordinate(null);
                slotCoordinateToSetup.itemData = null;
                slotCoordinateToSetup.isFull = false;
            }
        }
    }

    public void SetParentSlotCoordinate(SlotCoordinate parentSlotCoordinate) => this.parentSlotCoordinate = parentSlotCoordinate; 

    public void SetItemData(ItemData itemData) => this.itemData = itemData;
    
    public SlotCoordinate(int xCoord, int yCoord, Inventory inventory)
    {
        SetSlotCoordinate(xCoord, yCoord);
        myInventory = inventory;
    }

    void SetInventory(Inventory inventory) => myInventory = inventory;
}
