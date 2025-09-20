using UnityEngine;

[CreateAssetMenu(fileName = "Target Element", menuName = "Hemera Legions/Effect Condition/ Target Element")]
public class TargetElement : Condition
{
    public CardElement element;
    public override bool CheckCondition(Card caster, Card target)
    {
        return target.cardSO.CardElemnt == element;
    }
}
