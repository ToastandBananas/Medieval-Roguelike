using System;
using UnityEngine;

public class ScreenShakeActions : MonoBehaviour
{
    void Start()
    {
        Projectile.OnExplosion += Projectile_OnExplosion;
    }

    void Projectile_OnExplosion(object sender, EventArgs e)
    {
        ScreenShake.Instance.Shake(3f);
    }
}
