using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Cleanse And Regen", menuName = "Hemera Legions/Card Effects/ Cleanse And Regen")]

public class CleanseAndRegen : CardEffect
{
    [SerializeField] private int amount;
    public override void ActivateEffect(Card caster, Card target)
    {
        target.CleanAllNegativeEffects();
        target.ToHeal(amount);
    }

    public override void ActivateEffect(Card caster, List<Card> target)
    {
        foreach (Card card in target)
        {
            card.CleanAllNegativeEffects();
            card.ToHeal(amount);
        }
    }

    public override void ActivateEffect(SimCardState caster, SimCardState target)
    {
        target.CleanAllNegativeEffects();
        target.ToHeal(amount);
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
