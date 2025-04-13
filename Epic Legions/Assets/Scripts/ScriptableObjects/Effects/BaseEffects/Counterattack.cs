using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Counterattack", menuName = "Hemera Legions/Card Effects/ Counterattack")]
public class Counterattack : CardEffect
{
    private Card caster;
    [SerializeField] private int amount;

    public Card Caster => caster;
    public int Amount => amount;
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
        effect.durability = 0;
    }

    public override void UpdateEffect(Effect effect)
    {
        
    }
}
