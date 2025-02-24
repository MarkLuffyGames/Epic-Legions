using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Modify Defense", menuName = "Hemera Legions/Card Effects/ Modify Defense")]
public class ModifyDefense : CardEffect
{
    [SerializeField] private int amount;
    [SerializeField] private int numberTurns;
    [SerializeField] private bool isIncrease;

    public int Amount => amount;
    public int NumberTurns => numberTurns;
    public bool IsIncrease => isIncrease;

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

        if(movement.effect.durability <= 0)
        {
            movement.effect = null;
        }
    }
}
