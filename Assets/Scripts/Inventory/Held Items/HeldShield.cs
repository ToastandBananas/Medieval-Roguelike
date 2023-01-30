using System.Collections;
using UnityEngine;

public class HeldShield : HeldItem
{
    public override void DoDefaultAttack()
    {
        Debug.Log("Shield bash!");
    }
}
