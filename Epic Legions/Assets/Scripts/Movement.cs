using System;
using System.Collections.Generic;

[Serializable]
public class Movement
{
    private MoveSO moveSO;
    public Effect effect;
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
                moveSO.MoveEffect.ActivateEffect(caster, target, this);
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
            moveSO.MoveEffect.ActivateEffect(caster, targets, this);
        }
    }

    public bool EffectIsActive()
    {
        if (effect == null || effect.durability == 0)
        {
            return false;
        }

        return true;
    }

    public void UpdateEffect()
    {
        if (moveSO.MoveEffect != null)
        {
            moveSO.MoveEffect.UpdateEffect(this);
        }
    }

    public void CancelEffect()
    {
        if (moveSO.MoveEffect != null)
        {
            moveSO.MoveEffect.UpdateEffect(this);
        }
    }
}
