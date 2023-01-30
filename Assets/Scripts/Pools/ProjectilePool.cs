using System.Collections.Generic;
using UnityEngine;

public class ProjectilePool : MonoBehaviour
{
    public static ProjectilePool Instance;

    [SerializeField] Projectile projectilePrefab;
    [SerializeField] int amountToPool = 40;

    [Header("Scriptable Objects")]
    [SerializeField] Projectile_Item arrow;
    [SerializeField] Projectile_Item bomb;

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

    public Projectile_Item Arrow_SO() => arrow;

    public Projectile_Item Bomb_SO() => bomb;
}
