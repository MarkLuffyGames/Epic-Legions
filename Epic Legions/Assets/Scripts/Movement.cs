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
        if (moveSO.MoveEffect != null)
        {
            moveSO.MoveEffect.ActivateEffect(caster, target, this);
        }
    }

    public void ActivateEffect(Card caster, List<Card> target)
    {
        if (moveSO.MoveEffect != null)
        {
            moveSO.MoveEffect.ActivateEffect(caster, target, this);
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
}
