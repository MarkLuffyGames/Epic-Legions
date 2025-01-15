using System;
using System.Collections.Generic;

[Serializable]
public class Movement
{
    private MoveSO moveSO;
    public StatModifier statModifier;
    public MoveSO MoveSO => moveSO;

    public Movement(MoveSO moveSO)
    {
        this.moveSO = moveSO;
    }

    public void ActivateEffect(Card caster, Card target)
    {
        moveSO.MoveEffect.ActivateEffect(caster, target, this);
    }

    public void ActivateEffect(Card caster, List<Card> target)
    {
        moveSO.MoveEffect.ActivateEffect(caster, target, this);
    }

    public bool EffectIsActive()
    {
        if (statModifier == null || statModifier.durability == 0)
        {
            return false;
        }

        return true;
    }

    public StatModifier UpdateEffect()
    {
        return moveSO.MoveEffect.UpdateEffect(this);
    }
}
