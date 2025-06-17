using UnityEngine;

[CreateAssetMenu(fileName = "Lower Speed", menuName = "Hemera Legions/Effect Condition/ Lower Speed")]
public class LowerSpeed : Condition
{
    public override bool ActivateEffect(Card caster, Card target)
    {
        return target.CurrentSpeedPoints < caster.CurrentSpeedPoints;
    }
}
