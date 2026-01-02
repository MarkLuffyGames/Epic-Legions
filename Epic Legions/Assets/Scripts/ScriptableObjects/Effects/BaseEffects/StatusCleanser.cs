using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Status Cleanser", menuName = "Hemera Legions/Card Effects/ Status Cleanser")]
public class StatusCleanser : CardEffect
{
    public override void ActivateEffect(Card caster, Card target)
    {
        target.ClearAllEffects();
    }

    public override void ActivateEffect(Card caster, List<Card> target)
    {
        foreach (Card card in target)
        {
            card.ClearAllEffects();
        }
    }

    public override void ActivateEffect(SimCardState caster, SimCardState target)
    {
        target.ClearAllEffects();
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
