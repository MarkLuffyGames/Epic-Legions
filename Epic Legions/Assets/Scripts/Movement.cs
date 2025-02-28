using System;
using System.Collections.Generic;

[Serializable]
public class Movement
{
    private MoveSO moveSO;
    public MoveSO MoveSO => moveSO;

    public Movement(MoveSO moveSO)
    {
        this.moveSO = moveSO;
    }

    public void ActivateEffect(Card caster, Card target)
    {
        if (moveSO.EffectCondition.ActivateEffect(caster, target))
        {
            if (moveSO.MoveEffect != null)
            {
                moveSO.MoveEffect.ActivateEffect(caster, target);
            }
        }
    }

    public void ActivateEffect(Card caster, List<Card> target)
    {
        List<Card> targets = new List<Card>();
        foreach (Card card in target)
        {
            if (moveSO.EffectCondition.ActivateEffect(caster, card))
            {
                targets.Add(card);
            }
        }

        if (moveSO.MoveEffect != null)
        {
            moveSO.MoveEffect.ActivateEffect(caster, targets);
        }
    }
}
