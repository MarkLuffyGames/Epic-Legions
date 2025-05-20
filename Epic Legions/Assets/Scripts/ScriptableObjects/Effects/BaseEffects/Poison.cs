using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Poison", menuName = "Hemera Legions/Card Effects/ Poison")]
public class Poison : CardEffect
{
    [SerializeField] private int amount = 10;
    [SerializeField] private int numberTurns = 1;
    public int Amount => amount;
    public int NumberTurns => numberTurns;
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
        effect.durability = 0;
    }

    public override void UpdateEffect(Effect effect)
    {
        effect.elapsedTurns++;

        if (effect.elapsedTurns / (float)DuelManager.NumberOfTurns == 1)
        {
            effect.ApplyPoisonDamage();
            effect.elapsedTurns = 0;
        }
    }
}
