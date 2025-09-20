using UnityEngine;

[CreateAssetMenu(fileName = "Not Controlled", menuName = "Hemera Legions/Effect Condition/ Not Controlled")]
public class NotControlled : Condition
{
    public override bool CheckCondition(Card caster, Card target)
    {
        return !target.IsControlled();
    }
}
