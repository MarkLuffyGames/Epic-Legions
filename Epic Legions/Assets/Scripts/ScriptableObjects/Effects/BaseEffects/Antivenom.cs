using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Antivenom", menuName = "Hemera Legions/Card Effects/ Antivenom")]
public class Antivenom : CardEffect
{
    private Card caster;
    public Card Caster => caster;
    public override void ActivateEffect(Card caster, Card target)
    {
        this.caster = caster;
        target.AddEffect(new Effect(this, target));
    }

    public override void ActivateEffect(Card caster, List<Card> target)
    {
        this.caster = caster;
        foreach (Card card in target)
        {
            card.AddEffect(new Effect(this, card));
        }
    }

    public override void DeactivateEffect(Effect effect)
    {
        throw new System.NotImplementedException();
    }

    public override void UpdateEffect(Effect effect)
    {
        if (effect.casterHero.FieldPosition == null)
        {
            effect.durability = 0;
        }
    }
    public string DescriptionText()
    {
        return $"Cannot be poisoned";
    }
}

