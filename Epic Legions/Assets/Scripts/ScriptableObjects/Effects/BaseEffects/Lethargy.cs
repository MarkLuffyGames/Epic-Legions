using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Lethargy", menuName = "Hemera Legions/Card Effects/ Lethargy")]
public class Lethargy : CardEffect
{
    [SerializeField] private int numberTurns;

    public int NumberTurns => numberTurns * DuelManager.NumberOfTurns;
    public override void ActivateEffect(Card caster, Card target)
    {
        target.AddEffect(new Effect(this, target));
    }

    public override void ActivateEffect(Card caster, List<Card> target)
    {
        throw new System.NotImplementedException();
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
        return $"Lethargy \nRemaining turn{(effect.Durability > 1 ? "s" : "")} {effect.Durability}";
    }

    public override void ActivateEffect(SimCardState caster, SimCardState target)
    {
        target.AddEffect(new Effect(this, target.OriginalCard));
    }
}
