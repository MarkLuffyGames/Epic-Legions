using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Cleanse Negative", menuName = "Hemera Legions/Card Effects/ Cleanse Negative")]
public class CleanseNegative : CardEffect
{
    public override void ActivateEffect(Card caster, Card target)
    {
        target.CleanAllNegativeEffects();
    }

    public override void ActivateEffect(Card caster, List<Card> target)
    {
        foreach (Card card in target)
        {
            card.CleanAllNegativeEffects();
        }
    }

    public override void ActivateEffect(SimCardState caster, SimCardState target)
    {
        target.CleanAllNegativeEffects();
    }

    public override void DeactivateEffect(Effect effect)
    {
        throw new System.NotImplementedException();
    }

    public override void UpdateEffect(Effect effect, SimCardState simCardState)
    {
        throw new System.NotImplementedException();
    }
}
