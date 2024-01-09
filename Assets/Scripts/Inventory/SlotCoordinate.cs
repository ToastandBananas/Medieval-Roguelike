using UnityEngine;

namespace InventorySystem
{
    public class SlotCoordinate
    {
        public string Name { get; private set; }

        public Vector2Int Coordinate { get; private set; }
        public SlotCoordinate ParentSlotCoordinate { get; private set; }
        public Inventory MyInventory { get; private set; }
        public ItemData ItemData { get; private set; }
        public bool IsFull { get; private set; }

        public void SetSlotCoordinate(int xCoord, int yCoord)
        {
            Coordinate = new Vector2Int(xCoord, yCoord);
            Name = $"({xCoord}, {yCoord})";
        }

        public void SetupNewItem(ItemData newItemData)
        {
            ItemData = newItemData;
            ItemData.SetInventorySlotCoordinate(this);
            
            if (MyInventory.InventoryLayout.HasStandardSlotSize)
            {
                int width = ItemData.Item.Width;
                int height = ItemData.Item.Height;
                for (int x = 0; x < width; x++)
                {
                    for (int y = 0; y < height; y++)
                    {
                        SlotCoordinate slotCoordinateToSetup = MyInventory.GetSlotCoordinate(Coordinate.x - x, Coordinate.y - y);
                        slotCoordinateToSetup.SetParentSlotCoordinate(this);
                        slotCoordinateToSetup.IsFull = true;
                    }
                }
            }
            else
            {
                SetParentSlotCoordinate(this);
                IsFull = true;
            }
        }

        public void ClearItem()
        {
            Inventory inventory = MyInventory;
            if (MyInventory.InventoryLayout.HasStandardSlotSize)
            {
                int width = ParentSlotCoordinate.ItemData.Item.Width;
                int height = ParentSlotCoordinate.ItemData.Item.Height;
                for (int x = 0; x < width; x++)
                {
                    for (int y = 0; y < height; y++)
                    {
                        SlotCoordinate slotCoordinateToSetup = inventory.GetSlotCoordinate(Coordinate.x - x, Coordinate.y - y);
                        slotCoordinateToSetup.SetParentSlotCoordinate(slotCoordinateToSetup);
                        slotCoordinateToSetup.ItemData = null;
                        slotCoordinateToSetup.IsFull = false;
                    }
                }
            }
            else
            {
                SetParentSlotCoordinate(this);
                ItemData = null;
                IsFull = false;
            }
        }

        public void SetParentSlotCoordinate(SlotCoordinate parentSlotCoordinate) => ParentSlotCoordinate = parentSlotCoordinate;

        public void SetItemData(ItemData itemData) => ItemData = itemData;

        public SlotCoordinate(int xCoord, int yCoord, Inventory inventory)
        {
            SetSlotCoordinate(xCoord, yCoord);
            MyInventory = inventory;
            ParentSlotCoordinate = this;
        }
    }
}
