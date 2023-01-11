using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ExplosionPool : MonoBehaviour
{
    public static ExplosionPool Instance;

    [SerializeField] ParticleSystem explosionPrefab;
    [SerializeField] int amountToPool = 1;

    List<ParticleSystem> explosions = new List<ParticleSystem>();

    void Awake()
    {
        if (Instance != null)
        {
            Debug.LogError("There's more than one ExplosionPool! " + transform + " - " + Instance);
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    void Start()
    {
        for (int i = 0; i < amountToPool; i++)
        {
            ParticleSystem newExplosion = CreateNewExplosion();
            newExplosion.gameObject.SetActive(false);
        }
    }

    public ParticleSystem GetExplosionFromPool()
    {
        ParticleSystem explosion = null;
        for (int i = 0; i < explosions.Count; i++)
        {
            if (explosions[i].gameObject.activeSelf == false)
            {
                explosion = explosions[i];
                break;
            }
        }

        if (explosion == null)
            explosion = CreateNewExplosion();

        StartCoroutine(DelayDisableExplosion(explosion));
        return explosion;
    }

    ParticleSystem CreateNewExplosion()
    {
        ParticleSystem newExplosion = Instantiate(explosionPrefab, transform).GetComponent<ParticleSystem>();
        explosions.Add(newExplosion);
        return newExplosion;
    }

    IEnumerator DelayDisableExplosion(ParticleSystem explosion)
    {
        yield return new WaitForSeconds(explosion.main.startLifetime.constant);
        explosion.gameObject.SetActive(false);
        explosion.Clear();
    }
}
