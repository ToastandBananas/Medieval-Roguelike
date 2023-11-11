using UnityEngine;
using UnityEngine.UI;

namespace InventorySystem
{
    public class QuiverInventoryItem : InventoryItem
    {
        [Header("Quiver Ammo Images")]
        [SerializeField] RectTransform iconsParent_RectTransform;
        [SerializeField] Image[] quiverAmmoImages;

        public RectTransform IconsParent_RectTransform => iconsParent_RectTransform;
        public Image[] QuiverAmmoImages => quiverAmmoImages;

        void Start()
        {
            UpdateQuiverSprites();
        }

        public void UpdateQuiverSprites()
        {
            HideQuiverSprites();

            if (myUnitEquipment == null || myUnitEquipment.MyUnit.UnitEquipment.QuiverEquipped() == false)
                return;

            int spriteCount = 0;
            for (int i = 0; i < myUnitEquipment.MyUnit.QuiverInventoryManager.ParentInventory.ItemDatas.Count; i++)
            {
                spriteCount += myUnitEquipment.MyUnit.QuiverInventoryManager.ParentInventory.ItemDatas[i].CurrentStackSize;
            }

            int totalAmmoCount = spriteCount;
            if (spriteCount > 10)
                spriteCount = 10;

            int iconIndex = 0;
            for (int i = 0; i < myUnitEquipment.MyUnit.QuiverInventoryManager.ParentInventory.ItemDatas.Count; i++)
            {
                float ammoPercent = (float)myUnitEquipment.MyUnit.QuiverInventoryManager.ParentInventory.ItemDatas[i].CurrentStackSize / totalAmmoCount;
                int thisAmmosSpriteCount = Mathf.RoundToInt(spriteCount * ammoPercent);
                if (thisAmmosSpriteCount == 0 && ammoPercent > 0f)
                    thisAmmosSpriteCount = 1;

                for (int j = 0; j < thisAmmosSpriteCount; j++)
                {
                    if (iconIndex >= spriteCount)
                    {
                        quiverAmmoImages[iconIndex - 1].sprite = myUnitEquipment.MyUnit.QuiverInventoryManager.ParentInventory.ItemDatas[i].Item.Ammunition.QuiverSprites[iconIndex - 1];
                        quiverAmmoImages[iconIndex - 1].enabled = true;
                        break;
                    }
                    else if (iconIndex >= myUnitEquipment.MyUnit.QuiverInventoryManager.ParentInventory.ItemDatas[i].Item.Ammunition.QuiverSprites.Length)
                    {
                        Debug.LogWarning($"Not enough Quiver Sprites for {myUnitEquipment.MyUnit.QuiverInventoryManager.ParentInventory.ItemDatas[i].Item.name}");
                        break;
                    }

                    quiverAmmoImages[iconIndex].sprite = myUnitEquipment.MyUnit.QuiverInventoryManager.ParentInventory.ItemDatas[i].Item.Ammunition.QuiverSprites[iconIndex];
                    quiverAmmoImages[iconIndex].enabled = true;
                    iconIndex++;
                }
            }
        }

        public void HideQuiverSprites()
        {
            if (quiverAmmoImages[0].enabled == false) // Already hidden
                return;

            for (int i = 0; i < quiverAmmoImages.Length; i++)
            {
                quiverAmmoImages[i].enabled = false;
            }
        }
    }
}
