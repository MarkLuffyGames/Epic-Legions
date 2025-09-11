using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Ranged Immunity", menuName = "Hemera Legions/Card Effects/ Ranged Immunity")]
public class RangedImmunity : CardEffect
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

    public override void UpdateEffect(Effect effect)
    {
        if (!isPermanent) effect.durability--;
        effect.SetEffectDescription(DescriptionText(effect));
    }
    public string DescriptionText(Effect effect)
    {
        var s = $" for {effect.Durability} turn{(effect.Durability > 1 ? "s" : "")}";
        return "Immune to ranged attacks " + (!isPermanent ? s : "");
    }
}
