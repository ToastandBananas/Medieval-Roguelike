using UnityEngine;

public class LooseQuiverItem : LooseContainerItem
{
    [Header("Arrow Meshes")]
    [SerializeField] MeshFilter[] arrowMeshFilters;
    [SerializeField] MeshRenderer[] arrowMeshRenderers;

    public override void Awake()
    {
        base.Awake();

        UpdateArrowMeshes();
    }

    public void UpdateArrowMeshes()
    {
        HideArrowMeshes();
        
        int arrowCount = 0;
        for (int i = 0; i < ContainerInventoryManager.ParentInventory.ItemDatas.Count; i++)
        {
            arrowCount += ContainerInventoryManager.ParentInventory.ItemDatas[i].CurrentStackSize;
        }
        
        int totalAmmoCount = arrowCount;
        if (arrowCount > 5)
            arrowCount = 5;

        int meshIndex = 0;
        for (int i = 0; i < ContainerInventoryManager.ParentInventory.ItemDatas.Count; i++)
        {
            if (meshIndex >= arrowCount)
                break;

            if (ContainerInventoryManager.ParentInventory.ItemDatas[i].Item.Ammunition.LooseQuiverMesh == null)
            {
                Debug.LogWarning(ContainerInventoryManager.ParentInventory.ItemDatas[i].Item.name + " doesn't have an assigned Loose Quiver Mesh in its Scriptable Object");
                continue;
            }

            if (ContainerInventoryManager.ParentInventory.ItemDatas[i].Item.Ammunition.LooseQuiverMaterial == null)
            {
                Debug.LogWarning(ContainerInventoryManager.ParentInventory.ItemDatas[i].Item.name + " doesn't have an assigned Loose Quiver Material in its Scriptable Object");
                continue;
            }

            float ammoPercent = (float)ContainerInventoryManager.ParentInventory.ItemDatas[i].CurrentStackSize / totalAmmoCount;
            int thisAmmosSpriteCount = Mathf.RoundToInt(arrowCount * ammoPercent);
            for (int j = 0; j < thisAmmosSpriteCount; j++)
            {
                if (meshIndex >= arrowCount)
                {
                    arrowMeshFilters[meshIndex - 1].mesh = ContainerInventoryManager.ParentInventory.ItemDatas[i].Item.Ammunition.LooseQuiverMesh;
                    arrowMeshRenderers[meshIndex - 1].material = ContainerInventoryManager.ParentInventory.ItemDatas[i].Item.Ammunition.LooseQuiverMaterial;
                    arrowMeshFilters[meshIndex - 1].transform.parent.gameObject.SetActive(true);
                    break;
                }

                arrowMeshFilters[meshIndex].mesh = ContainerInventoryManager.ParentInventory.ItemDatas[i].Item.Ammunition.LooseQuiverMesh;
                arrowMeshRenderers[meshIndex].material = ContainerInventoryManager.ParentInventory.ItemDatas[i].Item.Ammunition.LooseQuiverMaterial;
                arrowMeshFilters[meshIndex].transform.parent.gameObject.SetActive(true);
                meshIndex++;
            }
        }
    }

    public void HideArrowMeshes()
    {
        if (arrowMeshRenderers[0].transform.parent.gameObject.activeSelf == false)
            return;

        for (int i = 0; i < arrowMeshRenderers.Length; i++)
        {
            arrowMeshRenderers[i].transform.parent.gameObject.SetActive(false);
        }
    }
}
