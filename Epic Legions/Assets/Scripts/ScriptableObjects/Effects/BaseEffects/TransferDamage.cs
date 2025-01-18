using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Transfer Damage", menuName = "Epic Legions/Card Effects/ Transfer Damage")]
public class TransferDamage : CardEffect
{

    [SerializeField] private int numberTurns;
    private Card caster;

    public int NumberTurns => numberTurns;
    public Card Caster => caster;
    public override void ActivateEffect(Card caster, Card target, Movement movement)
    {
        this.caster = caster;
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

    public override void UpdateEffect(Movement movement)
    {
        movement.effect.durability--;

        if (movement.effect.durability <= 0)
        {
            movement.effect = null;
        }
    }

    public override void DeactivateEffect(Movement movement)
    {
        movement.effect.durability = 0;
        movement.effect = null;
    }
}
