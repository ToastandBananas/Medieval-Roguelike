using System.Collections.Generic;
using UnityEngine;

namespace InventorySystem
{
    public class Pool_Projectiles : MonoBehaviour
    {
        public static Pool_Projectiles Instance;

        [SerializeField] Projectile projectilePrefab;
        [SerializeField] int amountToPool = 40;

        [Header("Scriptable Objects")]
        [SerializeField] Item_Ammunition arrow;
        [SerializeField] Item_Ammunition bomb;

        List<Projectile> projectiles = new List<Projectile>();

        void Awake()
        {
            if (Instance != null)
            {
                Debug.LogError("There's more than one ProjectilePool! " + transform + " - " + Instance);
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        void Start()
        {
            for (int i = 0; i < amountToPool; i++)
            {
                Projectile newProjectile = CreateNewProjectile();
                newProjectile.gameObject.SetActive(false);
            }
        }

        public Projectile GetProjectileFromPool()
        {
            for (int i = 0; i < projectiles.Count; i++)
            {
                if (projectiles[i].gameObject.activeSelf == false)
                    return projectiles[i];
            }

            return CreateNewProjectile();
        }

        Projectile CreateNewProjectile()
        {
            Projectile newProjectile = Instantiate(projectilePrefab, transform).GetComponent<Projectile>();
            projectiles.Add(newProjectile);
            return newProjectile;
        }

        public static void ReturnToPool(Projectile projectile)
        {
            projectile.transform.SetParent(Instance.transform);
            projectile.gameObject.SetActive(false);
        }

        public Item_Ammunition Arrow_SO() => arrow;

        public Item_Ammunition Bomb_SO() => bomb;
    }
}
