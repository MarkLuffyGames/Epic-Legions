using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Increase Defense", menuName = "Epic Legions/Card Effects/ Increase Defense")]
public class IncreaseDefense : CardEffect
{
    [SerializeField] private int amount;
    [SerializeField] private int numberTurns;

    public override void ActivateEffect(Card caster, Card target, Movement movement)
    {
        movement.statModifier = new StatModifier(amount, numberTurns);
        target.AddModifier(movement.statModifier);
    }

    public override void ActivateEffect(Card caster, List<Card> target, Movement movement)
    {
        movement.statModifier = new StatModifier(amount, numberTurns);
        foreach (Card card in target)
        {
            card.AddModifier(movement.statModifier);
        }
    }

    public override StatModifier UpdateEffect(Movement movement)
    {
        movement.statModifier.durability--;

        if(movement.statModifier.durability <= 0)
        {
            movement.statModifier = null;
            return movement.statModifier;
        }

        return null;
    }
}
