using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Absorb Damage", menuName = "Hemera Legions/Card Effects/ Absorb Damage")]
public class AbsorbDamage : CardEffect
{
    [SerializeField] private int amount;
    [SerializeField] private int numberTurns;

    public int Amount => amount;
    public int NumberTurns => numberTurns;
    public override void ActivateEffect(Card caster, Card target, Movement movement)
    {
        movement.effect = new Effect(this);
        target.AddEffect(movement.effect);
    }

    public override void ActivateEffect(Card caster, List<Card> target, Movement movement)
    {
        movement.effect = new Effect(this);
        foreach (Card card in target)
        {
            card.AddEffect(movement.effect);
        }
    }

    public override void DeactivateEffect(Movement movement)
    {
        throw new System.NotImplementedException();
    }

    public override void UpdateEffect(Movement movement)
    {
        movement.effect.durability--;

        if (movement.effect.durability <= 0)
        {
            movement.effect = null;
        }
    }
}
