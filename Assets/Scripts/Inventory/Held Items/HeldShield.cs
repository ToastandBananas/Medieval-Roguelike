using UnityEngine;

public class HeldShield : HeldItem
{
    public bool shieldRaised { get; private set; }

    public override void DoDefaultAttack()
    {
        Debug.LogWarning("Default attack for Shields is not created yet.");
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
