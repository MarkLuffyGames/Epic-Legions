using UnityEngine;

[CreateAssetMenu(fileName = "Lacks Healt", menuName = "Hemera Legions/Effect Condition/ Lacks Healt")]
public class LacksHealt : Condition
{
    public override bool CheckCondition(Card caster, Card target)
    {
        return target.CurrentHealtPoints < target.HealtPoint && target.CanReceiveHealing();
    }
}
