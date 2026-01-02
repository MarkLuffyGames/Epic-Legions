using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Full Damage Reflection", menuName = "Hemera Legions/Card Effects/ Full Damage Reflection")]
public class FullDamageReflection : CardEffect
{
    [SerializeField] private int numberTurns = 1;
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
        
    }

    public override void UpdateEffect(Effect effect, SimCardState simCardState)
    {
        effect.durability--;
        effect.SetEffectDescription(DescriptionText(effect));
    }
    public string DescriptionText(Effect effect)
    {
        return $"Reflect all damage received for {effect.Durability} turn{(effect.Durability > 1 ? "s" : "")}.";
    }

    public override void ActivateEffect(SimCardState caster, SimCardState target)
    {
        target.AddEffect(new Effect(this, target.OriginalCard));
    }
}
