using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Increase Defense", menuName = "Epic Legions/Card Effects/ Increase Defense")]
public class IncreaseDefense : CardEffect
{
    [SerializeField] private int amount;
    [SerializeField] private int numberTurns;

    public override void ActivateEffect(Card caster, Card target)
    {
        var modifier = new StatModifier(amount, numberTurns);
        target.AddModifier(modifier);
    }

    public override void ActivateEffect(Card caster, List<Card> target)
    {
        var modifier = new StatModifier(amount, numberTurns);
        foreach (Card card in target)
        {
            card.AddModifier(modifier);
        }
    }
}
