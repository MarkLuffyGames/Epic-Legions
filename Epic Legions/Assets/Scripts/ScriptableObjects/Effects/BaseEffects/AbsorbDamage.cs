using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Absorb Damage", menuName = "Epic Legions/Card Effects/ Absorb Damage")]
public class AbsorbDamage : CardEffect
{
    [SerializeField] private int amount;
    [SerializeField] private int numberTurns;
    public override void ActivateEffect(Card caster, Card target, Movement movement)
    {
        movement.effect = new Effect(0, amount, numberTurns);
        target.AddModifier(movement.effect);
    }

    public override void ActivateEffect(Card caster, List<Card> target, Movement movement)
    {
        movement.effect = new Effect(0, amount, numberTurns);
        foreach (Card card in target)
        {
            card.AddModifier(movement.effect);
        }
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
