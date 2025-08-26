using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Hero Control", menuName = "Hemera Legions/Card Effects/ Hero Control")]
public class HeroControl : CardEffect
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
        throw new System.NotImplementedException();
    }

    public override void DeactivateEffect(Effect effect)
    {
        effect.durability--;
    }

    public override void UpdateEffect(Effect effect)
    {
        
    }
    public string DescriptionText()
    {
        return $"Brain control";
    }
}
