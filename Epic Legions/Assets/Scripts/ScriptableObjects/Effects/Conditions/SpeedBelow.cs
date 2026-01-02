using UnityEngine;

[CreateAssetMenu(fileName = "Speed Below", menuName = "Hemera Legions/Effect Condition/ Speed Below")]
public class SpeedBelow : Condition
{
    [SerializeField] private int speedThreshold;
    public override bool CheckCondition(Card caster, Card target)
    {
        return target.CurrentSpeedPoints < speedThreshold;
    }

    public override bool CheckCondition(SimCardState caster, SimCardState target)
    {
        return target.CurrentSPD < speedThreshold;
    }
}
