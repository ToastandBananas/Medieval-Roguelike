using UnityEngine;

public class HeldShield : HeldItem
{
    public bool shieldRaised { get; private set; }

    public override void DoDefaultAttack(bool attackBlocked, HeldItem itemBlockedWith)
    {
        if (attackBlocked == false)
            Debug.Log("Shield bash!");
        else
            Debug.Log("Shield bash blocked...");
    }

    public void RaiseShield()
    {
        if (shieldRaised)
            return;

        shieldRaised = true;
        if (unit.leftHeldItem == this)
            anim.Play("RaiseShield_L");
        else if (unit.rightHeldItem == this)
            anim.Play("RaiseShield_R");
    }

    public void LowerShield()
    {
        if (shieldRaised == false)
            return;

        shieldRaised = false;
        if (unit.leftHeldItem == this)
            anim.Play("LowerShield_L");
        else if (unit.rightHeldItem == this)
            anim.Play("LowerShield_R");
    }
}
