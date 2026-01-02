using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Attack Immunity", menuName = "Hemera Legions/Card Effects/ Attack Immunity")]
public class AttackImmunity : CardEffect
{
    [SerializeField] private int numberTurns;
    [SerializeField] private bool isRanged;
    public int NumberTurns => numberTurns * DuelManager.NumberOfTurns;
    public bool IsRanged => isRanged;
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
        if (!isPermanent) effect.durability--;
        effect.SetEffectDescription(DescriptionText(effect));
    }
    public string DescriptionText(Effect effect)
    {
        var s = $" for {effect.Durability} turn{(effect.Durability > 1 ? "s" : "")}";
        return $"Immune to {(effect.isRanged ? "ranged" : "melee")} attacks " + (!isPermanent ? s : "");
    }

    public override void ActivateEffect(SimCardState caster, SimCardState target)
    {
        target.AddEffect(new Effect(this, target.OriginalCard));
    }
}
