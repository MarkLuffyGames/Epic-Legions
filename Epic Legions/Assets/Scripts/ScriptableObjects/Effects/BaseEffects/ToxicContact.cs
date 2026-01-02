using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Toxic Contact", menuName = "Hemera Legions/Card Effects/ Toxic Contact")]
public class ToxicContact : CardEffect
{
    [SerializeField] private int numberTurns;
    [SerializeField] private Poison poisonEffect;
    public int NumberTurns => numberTurns * DuelManager.NumberOfTurns;
    public Poison PoisonEffect => poisonEffect;
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
        throw new System.NotImplementedException();
    }

    public override void UpdateEffect(Effect effect, SimCardState simCardState)
    {
        effect.durability--;
        effect.SetEffectDescription(DescriptionText(effect));
    }
    public string DescriptionText(Effect effect)
    {
        return $"Toxic contact \nRemaining turn{(effect.Durability > 1 ? "s" : "")} {effect.Durability}";
    }

    public override void ActivateEffect(SimCardState caster, SimCardState target)
    {
        target.AddEffect(new Effect(this, target.OriginalCard));
    }
}
