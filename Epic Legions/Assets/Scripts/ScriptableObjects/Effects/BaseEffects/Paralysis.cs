using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Paralysis", menuName = "Hemera Legions/Card Effects/ Paralysis")]
public class Paralysis : CardEffect
{
    [SerializeField] private int numberTurns;
    public int NumberTurns => numberTurns * DuelManager.NumberOfTurns;
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
    }

    public override void UpdateEffect(Effect effect, SimCardState simCardState)
    {
        effect.durability--;
        effect.SetEffectDescription(DescriptionText(effect));
    }
    public string DescriptionText(Effect effect)
    {
        return $"Paralysis \nRemaining turn{(effect.Durability > 1 ? "s" : "")} {effect.Durability}";
    }

    public override void ActivateEffect(SimCardState caster, SimCardState target)
    {
        target.AddEffect(new Effect(this, target.OriginalCard));
    }
}
