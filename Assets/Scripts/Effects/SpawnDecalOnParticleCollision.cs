using System.Collections.Generic;
using UnityEngine;

public class SpawnDecalOnParticleCollision : MonoBehaviour
{
    public GameObject bloodDecalPrefab;
    private ParticleSystem ps;

    void Start()
    {
        ps = GetComponent<ParticleSystem>();
    }

    void OnParticleCollision(GameObject other)
    {
        if (bloodDecalPrefab == null) return;

        List<ParticleCollisionEvent> collisionEvents = new List<ParticleCollisionEvent>();
        int numCollisionEvents = ParticlePhysicsExtensions.GetCollisionEvents(ps, other, collisionEvents);

        for (int i = 0; i < numCollisionEvents; i++)
        {
            Vector3 collisionPos = collisionEvents[i].intersection;
            Quaternion rotation = Quaternion.LookRotation(collisionEvents[i].normal);
            Instantiate(bloodDecalPrefab, collisionPos, rotation);
        }
    }
}
