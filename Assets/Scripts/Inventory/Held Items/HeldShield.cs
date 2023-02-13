using System.Collections;
using UnityEngine;

public class HeldShield : HeldItem
{
    public override void DoDefaultAttack(bool attackBlocked)
    {
        if (attackBlocked == false)
            Debug.Log("Shield bash!");
        else
            Debug.Log("Shield bash blocked...");
    }
}
