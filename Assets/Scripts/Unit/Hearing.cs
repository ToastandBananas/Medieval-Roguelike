using UnityEngine;

public class Hearing : MonoBehaviour
{
    [SerializeField] float hearingRadius = 10f;

    SphereCollider hearingCollider;

    void Awake()
    {
        hearingCollider = GetComponent<SphereCollider>();
        hearingCollider.radius = hearingRadius;
    }

    public void SetHearingRadius(float radius)
    {
        hearingRadius = radius;
        hearingCollider.radius = radius;
    }

    public float HearingRadius() => hearingRadius;
}
