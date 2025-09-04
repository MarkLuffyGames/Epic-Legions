using UnityEngine;

public class None : Condition
{
    public override bool CheckCondition(Card caster, Card target)
    {
        return true;
    }
}
