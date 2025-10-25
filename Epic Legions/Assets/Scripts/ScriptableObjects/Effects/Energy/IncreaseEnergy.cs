using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Increase Energy", menuName = "Hemera Legions/Card Effects/ Increase Energy")]
public class IncreaseEnergy : CardEffect
{
    [SerializeField] private int amount = 10;
    [SerializeField] private int numberTurns = 1;
    public int Amount => amount;
    public int NumberTurns => numberTurns * DuelManager.NumberOfTurns;
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

    public override void UpdateEffect(Effect effect)
    {
        if (!isPermanent) effect.durability--;
    }
}
