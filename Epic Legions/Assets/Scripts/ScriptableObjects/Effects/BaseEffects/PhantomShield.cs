using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Phantom Shield", menuName = "Hemera Legions/Card Effects/ Phantom Shield")]
public class PhantomShield : CardEffect
{
    public override void ActivateEffect(Card caster, Card target)
    {
        target.AddEffect(new Effect(this, target));
    }

    public override void ActivateEffect(Card caster, List<Card> target)
    {
        foreach (Card card in target)
        {
            card.AddEffect(new Effect(this, card));
        }
    }

    public override void DeactivateEffect(Effect effect)
    {
        effect.durability = 0;
    }

    public override void UpdateEffect(Effect effect, SimCardState simCardState)
    {

    }
     public string DescriptionText()
    {
        return $"Absorb all damage";
    }

    public override void ActivateEffect(SimCardState caster, SimCardState target)
    {
        target.AddEffect(new Effect(this, target.OriginalCard));
    }
}
