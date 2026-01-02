using UnityEngine;

[CreateAssetMenu(fileName = "Lacks Healt", menuName = "Hemera Legions/Effect Condition/ Lacks Healt")]
public class LacksHealt : Condition
{
    public override bool CheckCondition(Card caster, Card target)
    {
        return target.CurrentHealthPoints < target.HealthPoint && target.CanReceiveHealing();
    }

    public override bool CheckCondition(SimCardState caster, SimCardState target)
    {
        return target.CurrentHP < target.HP && target.CanReceiveHealing();
    }
}
