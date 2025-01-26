using UnityEngine;

public class None : Condition
{
    public override bool ActivateEffect(Card caster, Card target)
    {
        return true;
    }
}
