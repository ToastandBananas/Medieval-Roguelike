using UnityEngine;
using UnityEngine.UI;

public class QuiverInventoryItem : InventoryItem
{
    [Header("Quiver Ammo Images")]
    [SerializeField] RectTransform iconsParent_RectTransform;
    [SerializeField] Image[] quiverAmmoImages;

    public RectTransform IconsParent_RectTransform => iconsParent_RectTransform;
    public Image[] QuiverAmmoImages => quiverAmmoImages;

    public void UpdateQuiverSprites()
    {
        if (myCharacterEquipment.Unit.CharacterEquipment.QuiverEquipped() == false)
            return;

        for (int i = 0; i < quiverAmmoImages.Length; i++)
        {
            quiverAmmoImages[i].enabled = false;
        }
        
        int spriteCount = 0;
        for (int i = 0; i < myCharacterEquipment.Unit.QuiverInventoryManager.ParentInventory.ItemDatas.Count; i++)
        {
            spriteCount += myCharacterEquipment.Unit.QuiverInventoryManager.ParentInventory.ItemDatas[i].CurrentStackSize;
        }

        int totalAmmoCount = spriteCount;
        if (spriteCount > 10)
            spriteCount = 10;

        int iconIndex = 0;
        for (int i = 0; i < myCharacterEquipment.Unit.QuiverInventoryManager.ParentInventory.ItemDatas.Count; i++)
        {
            if (iconIndex >= spriteCount)
                break;
            else if (iconIndex >= myCharacterEquipment.Unit.QuiverInventoryManager.ParentInventory.ItemDatas[i].Item.Ammunition.QuiverSprites.Length)
            {
                Debug.LogWarning($"Not enough Quiver Sprites for {myCharacterEquipment.Unit.QuiverInventoryManager.ParentInventory.ItemDatas[i].Item.name}");
                break;
            }

            float ammoPercent = myCharacterEquipment.Unit.QuiverInventoryManager.ParentInventory.ItemDatas[i].CurrentStackSize / totalAmmoCount;
            int thisAmmosSpriteCount = Mathf.RoundToInt(spriteCount * ammoPercent);
            for (int j = 0; j < thisAmmosSpriteCount; j++)
            {
                if (iconIndex >= spriteCount)
                {
                    quiverAmmoImages[iconIndex - 1].sprite = myCharacterEquipment.Unit.QuiverInventoryManager.ParentInventory.ItemDatas[i].Item.Ammunition.QuiverSprites[iconIndex - 1];
                    break;
                }
                else if (iconIndex >= myCharacterEquipment.Unit.QuiverInventoryManager.ParentInventory.ItemDatas[i].Item.Ammunition.QuiverSprites.Length)
                {
                    Debug.LogWarning($"Not enough Quiver Sprites for {myCharacterEquipment.Unit.QuiverInventoryManager.ParentInventory.ItemDatas[i].Item.name}");
                    break;
                }

                quiverAmmoImages[iconIndex].sprite = myCharacterEquipment.Unit.QuiverInventoryManager.ParentInventory.ItemDatas[i].Item.Ammunition.QuiverSprites[iconIndex];
                quiverAmmoImages[iconIndex].enabled = true;
                iconIndex++;
            }
        }
    }
}
