using UnityEngine;

[CreateAssetMenu(fileName = "Lower Speed", menuName = "Hemera Legions/Effect Condition/ Lower Speed")]
public class LowerSpeed : Condition
{
    public override bool CheckCondition(Card caster, Card target)
    {
        if(target == null || caster == null)
        {
            return false;
        }
        return target.CurrentSpeedPoints < caster.CurrentSpeedPoints;
    }

    public override bool CheckCondition(SimCardState caster, SimCardState target)
    {
        if (target == null || caster == null)
        {
            return false;
        }
        return target.GetEffectiveSpeed() < caster.GetEffectiveSpeed();
    }
}
