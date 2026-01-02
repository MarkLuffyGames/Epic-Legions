using UnityEngine;

public class None : Condition
{
    public override bool CheckCondition(Card caster, Card target)
    {
        return true;
    }

    public override bool CheckCondition(SimCardState caster, SimCardState target)
    {
        return true;    
    }
}
